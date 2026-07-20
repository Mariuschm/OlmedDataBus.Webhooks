# Target Architecture for Prosepo.Webhooks

## Layering

- Controllers: HTTP boundary only; no business logic.
- Services/Application: use-case orchestration, validation, response mapping.
- Services/Webhook: decryption, parsing, strategy selection, queue item creation.
- Application/Common: shared response contracts and result types.
- Prospeo.DbContext: persistence and database-facing services.

## Existing application contracts

- ApiErrorResponse
- ApiSuccessResponse<T>
- ApplicationResult<T>
- ApiResultFactory

## Webhook pipeline

- WebhookController receives and verifies the payload.
- WebhookDataParser classifies payloads.
- WebhookProcessingOrchestrator selects a strategy.
- Product, Order, MarketingInvoice, and Unknown strategies perform the concrete work.

## Target conventions

- Controllers should return IActionResult only through use-case services.
- Use-case services should not contain HTTP parsing details unless unavoidable.
- Shared error and success responses should be consistent across all endpoints.
- New webhook types should be added by introducing a strategy, not by expanding controller logic.
