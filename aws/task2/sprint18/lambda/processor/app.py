import json

from notification_contract import ContractValidationError, parse_and_validate


def _log(event_id, event_type, correlation_id, processing_status, message_id):
    print(
        json.dumps(
            {
                "eventId": event_id,
                "eventType": event_type,
                "correlationId": correlation_id,
                "processingStatus": processing_status,
                "sqsMessageId": message_id,
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
            )
        except (ContractValidationError, TypeError):
            _log(None, None, None, "Rejected", message_id)
            failures.append({"itemIdentifier": message_id})

    return {"batchItemFailures": failures}
