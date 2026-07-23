import json
import os

import boto3

from notification_contract import ContractValidationError, parse_and_validate

sns_client = boto3.client("sns")
TOPIC_ARN = os.environ.get("NOTIFICATION_TOPIC_ARN", "")


def _response(status_code, body):
    return {
        "statusCode": status_code,
        "headers": {"Content-Type": "application/json"},
        "body": json.dumps(body),
    }


def _trace_fields(context):
    trace_header = os.environ.get("_X_AMZN_TRACE_ID", "")
    trace_root = next(
        (
            value.removeprefix("Root=")
            for value in trace_header.split(";")
            if value.startswith("Root=")
        ),
        "",
    )
    return {
        "lambdaRequestId": getattr(context, "aws_request_id", None),
        "xrayTraceId": trace_root or None,
    }


def _log(event_id, event_type, correlation_id, status, context):
    print(
        json.dumps(
            {
                "eventId": event_id,
                "eventType": event_type,
                "correlationId": correlation_id,
                "publishStatus": status,
                **_trace_fields(context),
            },
            separators=(",", ":"),
        )
    )


def lambda_handler(event, context):
    if event.get("httpMethod") != "POST":
        return _response(405, {"message": "Method not allowed."})

    try:
        notification_event = parse_and_validate(event.get("body"))
    except ContractValidationError:
        _log(None, None, None, "Rejected", context)
        return _response(400, {"message": "The notification event is invalid."})

    event_id = notification_event["eventId"]
    event_type = notification_event["eventType"]
    correlation_id = notification_event["correlationId"]

    try:
        publish_result = sns_client.publish(
            TopicArn=TOPIC_ARN,
            Message=json.dumps(notification_event, separators=(",", ":")),
        )
    except Exception:
        _log(event_id, event_type, correlation_id, "Failed", context)
        return _response(500, {"message": "Notification publishing failed."})

    _log(event_id, event_type, correlation_id, "Queued", context)
    return _response(
        202,
        {
            "queued": True,
            "eventId": event_id,
            "messageId": publish_result.get("MessageId"),
        },
    )
