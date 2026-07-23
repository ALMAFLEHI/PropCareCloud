import io
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
COMMON = ROOT.parent / "common"
sys.path.insert(0, str(COMMON))
sys.path.insert(0, str(ROOT))

import app


def valid_event(event_type="MaintenanceRequestStatusChanged"):
    return {
        "schemaVersion": "1.0",
        "eventId": str(uuid4()),
        "eventType": event_type,
        "occurredAtUtc": "2026-07-23T10:00:00Z",
        "maintenanceRequestId": str(uuid4()),
        "actorUserId": str(uuid4()),
        "targetRole": "Multiple",
        "targetProfileIds": [str(uuid4())],
        "title": "Maintenance request status changed",
        "message": "The status of a maintenance request was updated.",
        "correlationId": str(uuid4()),
        "source": "PropCareCloud.Api",
    }


def sqs_record(payload, message_id=None):
    return {
        "messageId": message_id or str(uuid4()),
        "body": payload if isinstance(payload, str) else json.dumps(payload),
    }


class ProcessorTests(unittest.TestCase):
    def test_valid_message_is_processed(self):
        result = app.lambda_handler({"Records": [sqs_record(valid_event())]}, None)
        self.assertEqual([], result["batchItemFailures"])

    def test_multiple_message_batch_is_processed(self):
        result = app.lambda_handler(
            {
                "Records": [
                    sqs_record(valid_event("MaintenanceRequestCreated")),
                    sqs_record(valid_event("AttachmentConfirmed")),
                ]
            },
            None,
        )
        self.assertEqual([], result["batchItemFailures"])

    def test_malformed_message_is_returned_as_batch_failure(self):
        record = sqs_record("{invalid", "malformed-message")
        result = app.lambda_handler({"Records": [record]}, None)
        self.assertEqual(
            [{"itemIdentifier": "malformed-message"}], result["batchItemFailures"]
        )

    def test_unknown_schema_version_fails(self):
        payload = valid_event()
        payload["schemaVersion"] = "2.0"
        result = app.lambda_handler({"Records": [sqs_record(payload, "schema")]}, None)
        self.assertEqual([{"itemIdentifier": "schema"}], result["batchItemFailures"])

    def test_missing_field_fails(self):
        payload = valid_event()
        del payload["correlationId"]
        result = app.lambda_handler({"Records": [sqs_record(payload, "missing")]}, None)
        self.assertEqual([{"itemIdentifier": "missing"}], result["batchItemFailures"])

    def test_structured_output_is_safe(self):
        payload = valid_event()
        payload["message"] = "private content must not be logged"
        captured = io.StringIO()
        with patch("sys.stdout", captured):
            result = app.lambda_handler({"Records": [sqs_record(payload)]}, None)
        parsed_log = json.loads(captured.getvalue())
        self.assertEqual("Processed", parsed_log["processingStatus"])
        self.assertNotIn("private content", captured.getvalue())
        self.assertEqual([], result["batchItemFailures"])


if __name__ == "__main__":
    unittest.main()
