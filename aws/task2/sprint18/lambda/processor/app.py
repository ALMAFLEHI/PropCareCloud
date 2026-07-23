import json
import os

from notification_contract import ContractValidationError, parse_and_validate


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


def _log(
    event_id,
    event_type,
    correlation_id,
    processing_status,
    message_id,
    context,
):
    print(
        json.dumps(
            {
                "eventId": event_id,
                "eventType": event_type,
                "correlationId": correlation_id,
                "processingStatus": processing_status,
                "sqsMessageId": message_id,
                **_trace_fields(context),
            },
            separators=(",", ":"),
        )
    )


def lambda_handler(event, context):
    failures = []

    for record in event.get("Records", []):
        message_id = record.get("messageId")
        try:
            notification_event = parse_and_validate(record.get("body"))
            _log(
                notification_event["eventId"],
                notification_event["eventType"],
                notification_event["correlationId"],
                "Processed",
                message_id,
                context,
            )
        except (ContractValidationError, TypeError):
            _log(None, None, None, "Rejected", message_id, context)
            failures.append({"itemIdentifier": message_id})

    return {"batchItemFailures": failures}
