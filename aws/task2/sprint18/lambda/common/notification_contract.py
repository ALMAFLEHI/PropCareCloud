"""Shared validation for the PropCare Cloud Sprint 18 notification event."""

from datetime import datetime
import json
import uuid

SCHEMA_VERSION = "1.0"
SOURCE = "PropCareCloud.Api"
MAX_PAYLOAD_BYTES = 8192
MAX_TITLE_LENGTH = 120
MAX_MESSAGE_LENGTH = 500
MAX_TARGET_PROFILE_IDS = 20
ALLOWED_EVENT_TYPES = {
    "MaintenanceRequestCreated",
    "MaintenanceRequestAssigned",
    "MaintenanceRequestStatusChanged",
    "AttachmentConfirmed",
}
ALLOWED_TARGET_ROLES = {
    "Admin",
    "PropertyManager",
    "Tenant",
    "MaintenanceStaff",
    "Multiple",
}
REQUIRED_FIELDS = {
    "schemaVersion",
    "eventId",
    "eventType",
    "occurredAtUtc",
    "maintenanceRequestId",
    "actorUserId",
    "targetRole",
    "targetProfileIds",
    "title",
    "message",
    "correlationId",
    "source",
}


class ContractValidationError(ValueError):
    """Raised when a notification event does not match the safe contract."""


def parse_and_validate(raw_body):
    if not isinstance(raw_body, str):
        raise ContractValidationError("body must be a JSON string")
    if len(raw_body.encode("utf-8")) > MAX_PAYLOAD_BYTES:
        raise ContractValidationError("payload exceeds the maximum size")

    try:
        event = json.loads(raw_body)
    except (TypeError, json.JSONDecodeError) as error:
        raise ContractValidationError("body must contain valid JSON") from error

    return validate_event(event)


def validate_event(event):
    if not isinstance(event, dict):
        raise ContractValidationError("event must be a JSON object")

    missing_fields = REQUIRED_FIELDS.difference(event)
    if missing_fields:
        raise ContractValidationError("event is missing required fields")
    if event["schemaVersion"] != SCHEMA_VERSION:
        raise ContractValidationError("schema version is not supported")
    if event["eventType"] not in ALLOWED_EVENT_TYPES:
        raise ContractValidationError("event type is not allowed")
    if event["targetRole"] not in ALLOWED_TARGET_ROLES:
        raise ContractValidationError("target role is not allowed")
    if event["source"] != SOURCE:
        raise ContractValidationError("event source is not allowed")

    _require_uuid(event["eventId"], "eventId")
    _require_uuid(event["maintenanceRequestId"], "maintenanceRequestId")
    _require_uuid(event["correlationId"], "correlationId")
    if event["actorUserId"] is not None:
        _require_uuid(event["actorUserId"], "actorUserId")

    target_ids = event["targetProfileIds"]
    if not isinstance(target_ids, list):
        raise ContractValidationError("targetProfileIds must be an array")
    if len(target_ids) > MAX_TARGET_PROFILE_IDS:
        raise ContractValidationError("targetProfileIds exceeds the maximum")
    for target_id in target_ids:
        _require_uuid(target_id, "targetProfileIds")

    _require_text(event["title"], "title", MAX_TITLE_LENGTH)
    _require_text(event["message"], "message", MAX_MESSAGE_LENGTH)
    _require_timestamp(event["occurredAtUtc"])
    return event


def _require_uuid(value, field_name):
    if not isinstance(value, str):
        raise ContractValidationError(f"{field_name} must be a GUID")
    try:
        parsed = uuid.UUID(value)
    except (ValueError, AttributeError, TypeError) as error:
        raise ContractValidationError(f"{field_name} must be a GUID") from error
    if parsed.int == 0:
        raise ContractValidationError(f"{field_name} must not be empty")


def _require_text(value, field_name, maximum_length):
    if not isinstance(value, str) or not value.strip():
        raise ContractValidationError(f"{field_name} is required")
    if len(value) > maximum_length:
        raise ContractValidationError(f"{field_name} exceeds the maximum length")


def _require_timestamp(value):
    if not isinstance(value, str):
        raise ContractValidationError("occurredAtUtc must be an ISO-8601 timestamp")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as error:
        raise ContractValidationError(
            "occurredAtUtc must be an ISO-8601 timestamp"
        ) from error
    if parsed.tzinfo is None:
        raise ContractValidationError("occurredAtUtc must include a timezone")
