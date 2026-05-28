# Quickstart: Commercial User Accounts

This quickstart defines the intended implementation and verification path for
feature 008.

## 1. Configuration and AppHost changes

1. Add a non-secret AppHost parameter for commercial mode.
2. Forward that parameter to the web project as configuration, for example under
   `Features__CommercialMode__Enabled`.
3. Update the staging workflow to pass the new parameter via environment
   variables.
4. Keep the feature disabled for self-hosted and other non-commercial
   environments unless explicitly enabled.

## 2. Application and infrastructure changes

1. Add application contracts for:
   - commercial access resolution
   - account creation on first sign-in
   - profile retrieval
   - delete and restore actions
   - retention purge and audit persistence
2. Add repository-owned models for commercial account state, audit events, and
   access decisions.
3. Implement Azure Table adapters for account records and audit records using
   the existing storage account.
4. Keep Blob storage limited to data protection and any future opaque blob use
   cases.

## 3. Web changes

1. Introduce a commercial-mode-aware access gate in the web layer.
2. Preserve the current self-hosted authentication path when commercial mode is
   disabled.
3. Add the commercial login screen explaining that Microsoft 365 sign-in creates
   an account.
4. Show the current email address and tenant name in the signed-in experience
   when available.
5. Add a basic profile page with the stored account details and Delete Account
   action.
6. Show deleted-account retention guidance and a restore path for eligible users.

## 4. Scheduled work strategy

1. Implement retention policy in application use cases regardless of execution
   mechanism.
2. If automated purge is required in this feature slice, start with a
   commercial-only scheduled hosted service inside the web app.
3. Add an Azure Functions project later only when retention plus planned credits
   work justify separate scheduled compute.

## 5. Test and evidence checklist

Automated tests:

1. xUnit application tests for first sign-in creation, delete, restore, blocked
   deleted access, and audit emission.
2. Infrastructure adapter tests for Azure Table account and audit persistence.
3. bUnit or web-layer tests for commercial login gating, profile access, deleted
   account guidance, and self-hosted parity.
4. Architecture compliance updates proving that new commercial account contracts
   stay provider-neutral in Application and Domain.

Manual verification:

1. Run the app with commercial mode disabled and confirm the current self-hosted
   sign-in flow is unchanged.
2. Run the app with commercial mode enabled and confirm an unsigned user sees the
   explanatory commercial login screen.
3. Complete first sign-in and confirm account creation, access to the main app,
   and display of email plus tenant name when available.
4. Open the profile page and confirm only `TenantId`, `UserId`, and created date
   are shown from persisted account data.
5. Delete the account and confirm the current session loses access.
6. Sign in again during retention and confirm the deleted-account message and
   restore option appear instead of creating a new account.
7. Restore the account and confirm access resumes without creating a second
   account record.

Operational checks:

1. Confirm staging deployment receives the commercial-mode parameter.
2. Confirm no new database resources are introduced beyond the existing storage
   account.
3. Confirm diagnostics remain user-safe and do not expose secrets or raw
   exception detail.
