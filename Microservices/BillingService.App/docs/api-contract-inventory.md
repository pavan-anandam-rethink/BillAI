# API Contract Inventory (Legacy Baseline)

## Routing conventions

- Primary convention: `[Route("[controller]/[action]")]`
- Typical endpoint form: `/{Controller}/{Action}`
- Special cases:
  - `ClientChargeHistoryController` uses `[Route("[controller]")]` + explicit action route fragments.
  - `PusherAuthController` route: `/pusher/auth`.

## Auth contract

- `XApiKey` header path remains supported.
- JWT Bearer header path remains supported.
- Legacy middleware expects billing claims:
  - `BillingSessionKey`
  - `AccountInfoId`

## Controller inventory (BillingService.Web)

- `AppointmentController`
- `AppointmentReportsController`
- `BulkPaymentPostingController`
- `ChargeEntryController`
- `ChargePaymentController`
- `ClaimAttachmentController`
- `ClaimController`
- `ClaimNoteController`
- `ClaimPostingController`
- `ClaimUpdateController`
- `ClearingHouseController`
- `ClientChargeHistoryController`
- `EdiFileController`
- `NotifyClaimStatusController`
- `PatientInvoiceController`
- `PaymentAttachmentController`
- `PaymentNoteController`
- `PaymentPostingController`
- `ServiceLineAdjustmentController`
- `WriteOffController`
- `BillingSettingsController`
- `FunderSettingController`
- `PusherAuthController`

## Compatibility requirement for migrated API

For every migrated endpoint:

1. Keep identical HTTP method and route.
2. Keep required headers and auth behavior.
3. Keep response JSON shape and key names.
4. Keep status code semantics for success/failure.
5. Keep integration side-effects and ordering where required.

