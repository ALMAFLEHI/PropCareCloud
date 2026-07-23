import base64
import json
import logging
import os
import re
import uuid
from pathlib import PurePath
from urllib.parse import quote

import boto3
from botocore.exceptions import ClientError


LOGGER = logging.getLogger()
LOGGER.setLevel(logging.INFO)

BUCKET_NAME = os.environ.get("ATTACHMENT_BUCKET", "")
URL_EXPIRY_SECONDS = int(os.environ.get("URL_EXPIRY_SECONDS", "300"))
MAX_FILE_SIZE_BYTES = int(os.environ.get("MAX_FILE_SIZE_BYTES", "10485760"))
ALLOWED_CONTENT_TYPES = {
    value.strip().lower()
    for value in os.environ.get(
        "ALLOWED_CONTENT_TYPES",
        "image/jpeg,image/png,image/webp,application/pdf",
    ).split(",")
    if value.strip()
}
ALLOWED_EXTENSIONS = {
    "image/jpeg": {".jpg", ".jpeg"},
    "image/png": {".png"},
    "image/webp": {".webp"},
    "application/pdf": {".pdf"},
}

s3_client = boto3.client("s3")


def lambda_handler(event, context):
    method = str(event.get("httpMethod", "")).upper()
    path = str(event.get("resource") or event.get("path") or "").rstrip("/")
    try:
        if method != "POST":
            result = response(405, {"message": "Method not allowed."})
            _log_invocation(context, method, path, result["statusCode"])
            return result

        body = parse_body(event)
        if path.endswith("/attachments/upload-url"):
            result = create_upload_authorization(body)
        elif path.endswith("/attachments/verify"):
            result = verify_uploaded_object(body)
        elif path.endswith("/attachments/download-url"):
            result = create_download_authorization(body)
        else:
            result = response(404, {"message": "Attachment service route not found."})
        _log_invocation(context, method, path, result["statusCode"])
        return result
    except ValueError as error:
        result = response(400, {"message": str(error)})
        _log_invocation(context, method, path, result["statusCode"])
        return result
    except Exception as error:  # Lambda boundary: never return stack traces.
        LOGGER.error("Unhandled attachment service error: %s", type(error).__name__)
        result = response(500, {"message": "Attachment service request failed."})
        _log_invocation(context, method, path, result["statusCode"])
        return result


def _log_invocation(context, method, path, status_code):
    trace_header = os.environ.get("_X_AMZN_TRACE_ID", "")
    trace_root = next(
        (
            value.removeprefix("Root=")
            for value in trace_header.split(";")
            if value.startswith("Root=")
        ),
        "",
    )
    LOGGER.info(
        json.dumps(
            {
                "operation": path.rsplit("/", 1)[-1] or "unknown",
                "httpMethod": method,
                "statusCode": status_code,
                "lambdaRequestId": getattr(context, "aws_request_id", None),
                "xrayTraceId": trace_root or None,
            },
            separators=(",", ":"),
        )
    )


def create_upload_authorization(body):
    request_id = require_request_id(body)
    file_name = sanitize_file_name(body.get("fileName"))
    content_type = require_content_type(body.get("contentType"))
    size_bytes = require_size(body.get("sizeBytes"))
    require_matching_extension(file_name, content_type)

    object_key = (
        f"maintenance-requests/{request_id}/"
        f"{uuid.uuid4()}-{file_name}"
    )
    upload = s3_client.generate_presigned_post(
        Bucket=BUCKET_NAME,
        Key=object_key,
        Fields={
            "Content-Type": content_type,
            "x-amz-server-side-encryption": "AES256",
            "success_action_status": "204",
        },
        Conditions=[
            {"Content-Type": content_type},
            {"x-amz-server-side-encryption": "AES256"},
            {"success_action_status": "204"},
            ["content-length-range", 1, MAX_FILE_SIZE_BYTES],
        ],
        ExpiresIn=URL_EXPIRY_SECONDS,
    )
    LOGGER.info("Created attachment upload authorization for request %s", request_id)
    return response(
        200,
        {
            "uploadUrl": upload["url"],
            "fields": upload["fields"],
            "objectKey": object_key,
            "expiresInSeconds": URL_EXPIRY_SECONDS,
        },
    )


def verify_uploaded_object(body):
    request_id = require_request_id(body)
    object_key = require_object_key(body.get("objectKey"), request_id)
    content_type = require_content_type(body.get("contentType"))
    size_bytes = require_size(body.get("sizeBytes"))

    try:
        metadata = s3_client.head_object(Bucket=BUCKET_NAME, Key=object_key)
    except ClientError as error:
        error_code = str(error.response.get("Error", {}).get("Code", ""))
        status = 404 if error_code in {"404", "NoSuchKey", "NotFound"} else 502
        return response(status, {"message": "Uploaded attachment could not be verified."})

    actual_type = str(metadata.get("ContentType", "")).lower()
    actual_size = int(metadata.get("ContentLength", -1))
    encryption = str(metadata.get("ServerSideEncryption", ""))
    if actual_type != content_type or actual_size != size_bytes or encryption != "AES256":
        return response(400, {"message": "Uploaded attachment metadata did not match."})

    LOGGER.info("Verified attachment upload for request %s", request_id)
    return response(
        200,
        {
            "verified": True,
            "objectKey": object_key,
            "contentType": actual_type,
            "sizeBytes": actual_size,
        },
    )


def create_download_authorization(body):
    request_id = require_request_id(body)
    object_key = require_object_key(body.get("objectKey"), request_id)
    file_name = sanitize_file_name(body.get("fileName"))

    try:
        s3_client.head_object(Bucket=BUCKET_NAME, Key=object_key)
    except ClientError:
        return response(404, {"message": "Attachment was not found."})

    content_disposition = f"inline; filename*=UTF-8''{quote(file_name)}"
    download_url = s3_client.generate_presigned_url(
        "get_object",
        Params={
            "Bucket": BUCKET_NAME,
            "Key": object_key,
            "ResponseContentDisposition": content_disposition,
        },
        ExpiresIn=URL_EXPIRY_SECONDS,
    )
    LOGGER.info("Created attachment download authorization for request %s", request_id)
    return response(
        200,
        {
            "downloadUrl": download_url,
            "expiresInSeconds": URL_EXPIRY_SECONDS,
        },
    )


def parse_body(event):
    raw_body = event.get("body")
    if not isinstance(raw_body, str) or not raw_body.strip():
        raise ValueError("A JSON request body is required.")
    if event.get("isBase64Encoded"):
        raw_body = base64.b64decode(raw_body).decode("utf-8")
    try:
        body = json.loads(raw_body)
    except (json.JSONDecodeError, UnicodeDecodeError, ValueError) as error:
        raise ValueError("The request body must contain valid JSON.") from error
    if not isinstance(body, dict):
        raise ValueError("The request body must be a JSON object.")
    return body


def require_request_id(body):
    try:
        return str(uuid.UUID(str(body.get("requestId", ""))))
    except (ValueError, AttributeError) as error:
        raise ValueError("A valid maintenance request ID is required.") from error


def sanitize_file_name(value):
    if not isinstance(value, str) or not value.strip():
        raise ValueError("A file name is required.")
    leaf_name = PurePath(value.replace("\\", "/")).name.strip().strip(".")
    safe_name = re.sub(r"[^A-Za-z0-9._ -]+", "_", leaf_name).strip(" .")
    if not safe_name or safe_name in {".", ".."}:
        raise ValueError("The file name is invalid.")
    if len(safe_name) > 255:
        suffix = PurePath(safe_name).suffix
        safe_name = f"{PurePath(safe_name).stem[: max(1, 255 - len(suffix))]}{suffix}"
    return safe_name


def require_content_type(value):
    content_type = str(value or "").strip().lower()
    if content_type not in ALLOWED_CONTENT_TYPES:
        raise ValueError("Only JPEG, PNG, WebP, and PDF attachments are allowed.")
    return content_type


def require_matching_extension(file_name, content_type):
    extension = PurePath(file_name).suffix.lower()
    if extension not in ALLOWED_EXTENSIONS.get(content_type, set()):
        raise ValueError("The file extension does not match the attachment type.")


def require_size(value):
    if isinstance(value, bool):
        raise ValueError("Attachment size must be a whole number.")
    try:
        size_bytes = int(value)
    except (TypeError, ValueError) as error:
        raise ValueError("Attachment size must be a whole number.") from error
    if size_bytes < 1:
        raise ValueError("The attachment cannot be empty.")
    if size_bytes > MAX_FILE_SIZE_BYTES:
        raise ValueError("The attachment must be 10 MB or smaller.")
    return size_bytes


def require_object_key(value, request_id):
    object_key = str(value or "")
    expected_prefix = f"maintenance-requests/{request_id}/"
    if not object_key.startswith(expected_prefix) or ".." in object_key:
        raise ValueError("The attachment object key is invalid.")
    try:
        generated_name = object_key.removeprefix(expected_prefix)
        if len(generated_name) < 38 or generated_name[36] != "-":
            raise ValueError
        uuid.UUID(generated_name[:36])
    except (ValueError, IndexError) as error:
        raise ValueError("The attachment object key is invalid.") from error
    return object_key


def response(status_code, body):
    return {
        "statusCode": status_code,
        "headers": {"Content-Type": "application/json"},
        "body": json.dumps(body),
    }
