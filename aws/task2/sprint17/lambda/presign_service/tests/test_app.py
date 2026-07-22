import json
import os
import sys
import types
import unittest
from pathlib import Path
from unittest.mock import MagicMock

SERVICE_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SERVICE_ROOT))
os.environ.setdefault("AWS_DEFAULT_REGION", "us-east-1")
os.environ.setdefault("AWS_ACCESS_KEY_ID", "test")
os.environ.setdefault("AWS_SECRET_ACCESS_KEY", "test")
os.environ.setdefault("ATTACHMENT_BUCKET", "private-test-bucket")


class FakeClientError(Exception):
    def __init__(self, response, operation_name):
        super().__init__(operation_name)
        self.response = response


fake_boto3 = types.ModuleType("boto3")
fake_boto3.client = lambda _service_name: MagicMock()
fake_botocore = types.ModuleType("botocore")
fake_botocore_exceptions = types.ModuleType("botocore.exceptions")
fake_botocore_exceptions.ClientError = FakeClientError
sys.modules["boto3"] = fake_boto3
sys.modules["botocore"] = fake_botocore
sys.modules["botocore.exceptions"] = fake_botocore_exceptions

import app  # noqa: E402


class PresignServiceTests(unittest.TestCase):
    def setUp(self):
        self.request_id = "4ba37d70-0ca4-4f75-9c07-1ed1f3c0c52b"
        app.s3_client = MagicMock()
        app.s3_client.generate_presigned_post.return_value = {
            "url": "https://private-test-bucket.s3.amazonaws.com",
            "fields": {"key": "generated"},
        }

    def invoke(self, route, body):
        return app.lambda_handler(
            {
                "httpMethod": "POST",
                "resource": route,
                "body": json.dumps(body),
            },
            None,
        )

    def test_valid_upload_request_returns_presigned_post(self):
        result = self.invoke(
            "/attachments/upload-url",
            {
                "requestId": self.request_id,
                "fileName": "leak.png",
                "contentType": "image/png",
                "sizeBytes": 1024,
            },
        )

        payload = json.loads(result["body"])
        self.assertEqual(200, result["statusCode"])
        self.assertTrue(
            payload["objectKey"].startswith(
                f"maintenance-requests/{self.request_id}/"
            )
        )
        app.s3_client.generate_presigned_post.assert_called_once()

    def test_invalid_content_type_is_rejected(self):
        result = self.invoke(
            "/attachments/upload-url",
            {
                "requestId": self.request_id,
                "fileName": "script.exe",
                "contentType": "application/octet-stream",
                "sizeBytes": 10,
            },
        )
        self.assertEqual(400, result["statusCode"])

    def test_oversized_attachment_is_rejected(self):
        result = self.invoke(
            "/attachments/upload-url",
            {
                "requestId": self.request_id,
                "fileName": "large.pdf",
                "contentType": "application/pdf",
                "sizeBytes": 10 * 1024 * 1024 + 1,
            },
        )
        self.assertEqual(400, result["statusCode"])

    def test_unsafe_file_name_is_reduced_to_safe_leaf_name(self):
        result = self.invoke(
            "/attachments/upload-url",
            {
                "requestId": self.request_id,
                "fileName": "../../private report.pdf",
                "contentType": "application/pdf",
                "sizeBytes": 200,
            },
        )
        payload = json.loads(result["body"])
        self.assertEqual(200, result["statusCode"])
        self.assertNotIn("..", payload["objectKey"])
        self.assertTrue(payload["objectKey"].endswith("-private report.pdf"))

    def test_invalid_request_id_is_rejected(self):
        result = self.invoke(
            "/attachments/upload-url",
            {
                "requestId": "not-a-guid",
                "fileName": "leak.png",
                "contentType": "image/png",
                "sizeBytes": 1024,
            },
        )
        self.assertEqual(400, result["statusCode"])

    def test_verify_success_returns_matching_metadata(self):
        object_key = (
            f"maintenance-requests/{self.request_id}/"
            "09543af4-6d15-49b2-ab55-5a5354806b7e-leak.png"
        )
        app.s3_client.head_object.return_value = {
            "ContentType": "image/png",
            "ContentLength": 1024,
            "ServerSideEncryption": "AES256",
        }
        result = self.invoke(
            "/attachments/verify",
            {
                "requestId": self.request_id,
                "objectKey": object_key,
                "contentType": "image/png",
                "sizeBytes": 1024,
            },
        )
        self.assertEqual(200, result["statusCode"])
        self.assertTrue(json.loads(result["body"])["verified"])

    def test_verify_missing_object_returns_not_found(self):
        app.s3_client.head_object.side_effect = FakeClientError(
            {"Error": {"Code": "404", "Message": "Not Found"}},
            "HeadObject",
        )
        result = self.invoke(
            "/attachments/verify",
            {
                "requestId": self.request_id,
                "objectKey": (
                    f"maintenance-requests/{self.request_id}/"
                    "09543af4-6d15-49b2-ab55-5a5354806b7e-leak.png"
                ),
                "contentType": "image/png",
                "sizeBytes": 1024,
            },
        )
        self.assertEqual(404, result["statusCode"])

    def test_download_request_returns_short_lived_url(self):
        app.s3_client.head_object.return_value = {"ContentLength": 1024}
        app.s3_client.generate_presigned_url.return_value = (
            "https://private-test-bucket.s3.amazonaws.com/signed"
        )
        result = self.invoke(
            "/attachments/download-url",
            {
                "requestId": self.request_id,
                "objectKey": (
                    f"maintenance-requests/{self.request_id}/"
                    "09543af4-6d15-49b2-ab55-5a5354806b7e-leak.png"
                ),
                "fileName": "leak.png",
            },
        )
        payload = json.loads(result["body"])
        self.assertEqual(200, result["statusCode"])
        self.assertEqual(300, payload["expiresInSeconds"])

    def test_malformed_body_is_rejected(self):
        result = app.lambda_handler(
            {
                "httpMethod": "POST",
                "resource": "/attachments/upload-url",
                "body": "{not-json",
            },
            None,
        )
        self.assertEqual(400, result["statusCode"])


if __name__ == "__main__":
    unittest.main()
