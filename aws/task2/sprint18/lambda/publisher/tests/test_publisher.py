import io
import json
import os
from pathlib import Path
import sys
import types
import unittest
from unittest.mock import MagicMock, patch
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
COMMON = ROOT.parent / "common"
sys.path.insert(0, str(COMMON))
sys.path.insert(0, str(ROOT))
os.environ["NOTIFICATION_TOPIC_ARN"] = "safe-test-topic"

fake_boto3 = types.ModuleType("boto3")
fake_boto3.client = lambda _service_name: MagicMock()
sys.modules["boto3"] = fake_boto3

import app


class FakeSns:
    def __init__(self, should_fail=False):
        self.should_fail = should_fail
        self.messages = []

    def publish(self, **kwargs):
        if self.should_fail:
            raise RuntimeError("safe simulated failure")
        self.messages.append(kwargs)
        return {"MessageId": "safe-message-id"}


def valid_event():
    return {
        "schemaVersion": "1.0",
        "eventId": str(uuid4()),
        "eventType": "MaintenanceRequestCreated",
        "occurredAtUtc": "2026-07-23T10:00:00Z",
        "maintenanceRequestId": str(uuid4()),
        "actorUserId": str(uuid4()),
        "targetRole": "Multiple",
        "targetProfileIds": [str(uuid4())],
        "title": "Maintenance request created",
        "message": "A maintenance request was submitted.",
        "correlationId": str(uuid4()),
        "source": "PropCareCloud.Api",
    }


def api_event(payload):
    return {"httpMethod": "POST", "body": json.dumps(payload)}


class PublisherTests(unittest.TestCase):
    def setUp(self):
        self.sns = FakeSns()
        app.sns_client = self.sns

    def test_valid_event_publishes_to_sns(self):
        response = app.lambda_handler(api_event(valid_event()), None)
        self.assertEqual(202, response["statusCode"])
        self.assertEqual(1, len(self.sns.messages))
        self.assertTrue(json.loads(response["body"])["queued"])

    def test_malformed_json_is_rejected(self):
        response = app.lambda_handler(
            {"httpMethod": "POST", "body": "{invalid"}, None
        )
        self.assertEqual(400, response["statusCode"])

    def test_missing_required_field_is_rejected(self):
        payload = valid_event()
        del payload["eventId"]
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_unknown_event_type_is_rejected(self):
        payload = valid_event()
        payload["eventType"] = "Unknown"
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_invalid_guid_is_rejected(self):
        payload = valid_event()
        payload["maintenanceRequestId"] = "not-a-guid"
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_oversized_title_is_rejected(self):
        payload = valid_event()
        payload["title"] = "T" * 121
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_oversized_message_is_rejected(self):
        payload = valid_event()
        payload["message"] = "M" * 501
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_target_profile_limit_is_enforced(self):
        payload = valid_event()
        payload["targetProfileIds"] = [str(uuid4()) for _ in range(21)]
        self.assertEqual(400, app.lambda_handler(api_event(payload), None)["statusCode"])

    def test_sns_failure_is_handled_without_sensitive_output(self):
        app.sns_client = FakeSns(should_fail=True)
        payload = valid_event()
        payload["message"] = "safe business message"
        captured = io.StringIO()
        with patch("sys.stdout", captured):
            response = app.lambda_handler(api_event(payload), None)
        self.assertEqual(500, response["statusCode"])
        self.assertNotIn("safe business message", captured.getvalue())


if __name__ == "__main__":
    unittest.main()
