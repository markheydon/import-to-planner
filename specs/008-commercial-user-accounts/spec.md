# Feature Specification: Commercial User Accounts

**Feature Branch**: `008-commercial-user-accounts`

**Created**: 2026-05-28

**Status**: Draft

**Input**: User description: "As a first step to make this app commercially viable, the app needs a login screen for the commercial version, while self-hosted deployments must continue to sign the current Microsoft 365 user in automatically. In the commercial version, if the user is not already signed in, show a login screen explaining that signing in with Microsoft 365 will create an account in the app. Show the current tenant name and email address on the main screen, add a basic profile page with a Delete Account button, and provide a profile link without changing the current navigation structure."

## Clarifications

### Session 2026-05-28

- Q: How should commercial user accounts be uniquely identified? -> A: Tenant Id -> User Id.
- Q: Which account details should be stored for the first release? -> A: Tenant Id, User Id, and created date only.
- Q: How should account deletion behave? -> A: Mark the account deleted first, then remove it after a 6-month retention window.
- Q: What should happen if a deleted user signs in again during the retention window? -> A: Block access by default, but offer automatic account restoration.
- Q: What audit trail is required for account lifecycle events? -> A: Record account create, delete, restore, and sign-in outcome events with Tenant Id, User Id, timestamp, and outcome; retain audit records for 12 months.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - First Commercial Sign-In Creates an Account (Priority: P1)

A user opening the commercial version of the app for the first time needs to understand why they must sign in, what will happen when they do, and how to proceed without confusion.

**Why this priority**: The commercial service is not viable without a clear sign-in entry point and an account being created for a user the first time they authenticate.

**Independent Test**: Can be fully tested by opening the commercial version while signed out, confirming the login screen explains the sign-in outcome, signing in with a Microsoft 365 account, and verifying that the user reaches the main app experience as a recognised account holder.

**Acceptance Scenarios**:

1. **Given** a person opens the commercial version while not signed in, **When** the app loads, **Then** it shows a login screen instead of the main app and explains that signing in with Microsoft 365 will create an account in the app.
2. **Given** a person signs in successfully to the commercial version for the first time, **When** authentication completes, **Then** the app creates their account using the minimum identity details needed and grants access to the main app.
3. **Given** a returning commercial user is already signed in, **When** they open the app, **Then** they are taken directly to the main experience without seeing the first-login explanation again.

---

### User Story 2 - Signed-In User Can See Their Identity Context (Priority: P2)

A signed-in commercial user needs to confirm which work account and organisation they are currently using so they can trust they are importing into the correct Microsoft 365 context.

**Why this priority**: Commercial users need immediate confidence that they are operating under the intended email address and tenant, especially when they work across more than one organisation.

**Independent Test**: Can be tested independently by signing in to the commercial version and verifying that the main screen shows the current user's email address and tenant name in a persistent, easy-to-find location.

**Acceptance Scenarios**:

1. **Given** a commercial user has signed in, **When** the main screen loads, **Then** the top-right area shows the current user's email address.
2. **Given** a commercial user has signed in and the tenant name is available for the current session, **When** the main screen loads, **Then** the top-right area shows the tenant name alongside the email address.
3. **Given** a commercial user wants to view their account details, **When** they look for account-related actions from the main screen, **Then** they can find a profile link without the existing navigation structure being otherwise reorganised.

---

### User Story 3 - User Manages and Deletes Their Account (Priority: P3)

A commercial user needs a simple profile page where they can review their basic account details and permanently remove their app account if they no longer want to use the service.

**Why this priority**: A commercial service needs a user-controlled account management path from the outset, even if profile information is minimal in the first release.

**Independent Test**: Can be fully tested by signing in to the commercial version, opening the profile page, reviewing the visible account details, deleting the account, and confirming that the user must go through first sign-in account creation again on their next visit.

**Acceptance Scenarios**:

1. **Given** a signed-in commercial user opens the profile page, **When** the page loads, **Then** it shows the stored account details currently held for that user, limited to Tenant Id, User Id, and account created date, and a Delete Account action.
2. **Given** a signed-in commercial user chooses Delete Account, **When** they confirm the action, **Then** the app marks their account as deleted immediately, ends their signed-in access, and starts a 6-month retention window before permanent removal.
3. **Given** a user has marked their commercial account for deletion, **When** they next visit the commercial version during the retention window, **Then** they are blocked from normal access, informed that the account is in the retention period, and offered a way to restore the deleted account automatically.
4. **Given** a user with a deleted account chooses the restore option during the retention window, **When** restoration completes, **Then** the existing account is reactivated and the user regains access without creating a new account.

---

### User Story 4 - Self-Hosted Access Keeps Today’s Behaviour (Priority: P4)

A self-hosting organisation needs the current deployment model to continue working without the commercial login gate being introduced into their existing flow.

**Why this priority**: Preserving the existing self-hosted behaviour avoids breaking current users while the commercial experience is introduced.

**Independent Test**: Can be tested independently by opening a self-hosted deployment and confirming that the current Microsoft 365 sign-in behaviour still controls access without the commercial first-login screen appearing.

**Acceptance Scenarios**:

1. **Given** a user opens a self-hosted deployment, **When** the app loads, **Then** access continues to be governed by the existing automatic Microsoft 365 sign-in behaviour rather than the new commercial login screen.
2. **Given** a self-hosted deployment signs the current user in successfully, **When** the main screen appears, **Then** the user can continue using the app without any requirement to create or manage a separate commercial account.

### Edge Cases

- What happens when a commercial user cancels sign-in or the sign-in attempt fails before an account is created?
- What happens when the commercial version can determine the email address for the current session but cannot retrieve a tenant name?
- What happens when a user deletes their account while they still have an active signed-in session in another browser tab?
- How does the app ensure the self-hosted deployment never shows the commercial first-login screen by mistake?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The commercial version of the app MUST show a login screen whenever a user is not already signed in.
- **FR-002**: The commercial login screen MUST explain in plain language that signing in with Microsoft 365 will create an account in the app.
- **FR-003**: The commercial version MUST create an app account automatically the first time a user completes sign-in successfully.
- **FR-004**: The initial app account MUST store only the minimum identity details needed to recognise the user and support future expansion of profile details: Tenant Id, User Id, and the account created date.
- **FR-005**: Each commercial app account MUST be uniquely identified by the combination of Tenant Id and User Id rather than by email address alone.
- **FR-006**: Returning commercial users who are already signed in MUST be taken directly to the main app experience without repeating the first-login explanation.
- **FR-007**: The main screen in the commercial version MUST display the signed-in user's email address in the top-right area.
- **FR-008**: The main screen in the commercial version MUST display the current tenant name alongside the email address when that tenant name is available for the session.
- **FR-009**: The app MUST provide a profile link from the signed-in experience without otherwise changing the current navigation structure for this release.
- **FR-010**: The profile page MUST show the stored account details for the user, limited in the first release to Tenant Id, User Id, and account created date.
- **FR-011**: The profile page MUST provide a Delete Account action that marks the user's app account as deleted immediately.
- **FR-012**: A deleted commercial account MUST be retained for 6 months before permanent removal.
- **FR-013**: After an account is marked deleted, the user MUST lose signed-in access to the commercial service until they either restore the deleted account during the retention window or the account is permanently removed.
- **FR-014**: If a deleted user signs in again during the 6-month retention window, the app MUST block normal access and show that the account is deleted but eligible for restoration.
- **FR-015**: If the user chooses to restore the deleted account during the retention window, the app MUST reactivate the same account automatically rather than creating a new account.
- **FR-016**: Self-hosted deployments MUST retain the current automatic Microsoft 365 sign-in behaviour and MUST NOT require the commercial first-login account creation flow.
- **FR-017**: The first release of this feature MUST NOT require storing the tenant name, email address, or display name as part of the user's saved account details.
- **FR-018**: The feature MUST distinguish clearly between the commercial user-account flow and the self-hosted access flow so that each deployment presents the correct sign-in behaviour.
- **FR-019**: The commercial service MUST record audit events for account creation, account deletion, account restoration, and sign-in outcomes using Tenant Id, User Id, timestamp, and outcome.
- **FR-020**: Audit records for account lifecycle and sign-in outcome events MUST be retained for 12 months.

### Key Entities *(include if feature involves data)*

- **App Account**: A record representing a commercial user of the service, uniquely identified by Tenant Id and User Id, and initially storing only those identifiers plus the account created date so the model can be expanded later.
- **Deleted Account Retention**: The retained state for a deleted commercial account, lasting 6 months before permanent removal and allowing account restoration during that period.
- **Account Audit Event**: A record of an account lifecycle or sign-in outcome event containing Tenant Id, User Id, timestamp, and outcome, retained for operational and audit purposes.
- **Profile View**: The user-facing summary of the current account, showing the limited stored account details available in the first release and exposing account removal.
- **Session Identity Context**: The current signed-in email address and tenant name shown to the user during a session so they can confirm which Microsoft 365 context is active.
- **Deployment Access Mode**: The active operating mode of the app that determines whether the user sees the commercial account flow or the existing self-hosted sign-in behaviour.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time commercial user can reach the main app screen, including understanding the sign-in message and completing account creation, in under 2 minutes without external help.
- **SC-002**: 100% of tested self-hosted deployments continue to grant access through the existing Microsoft 365 sign-in path without showing the commercial login screen.
- **SC-003**: 100% of tested signed-in commercial users can identify the active email address from the main screen without opening another page.
- **SC-004**: At least 95% of tested signed-in commercial users can identify both their email address and tenant name from the main screen when tenant information is available for the session.
- **SC-005**: A signed-in commercial user can navigate to the profile page and mark their account for deletion in under 1 minute.
- **SC-006**: 100% of tested deleted accounts remain inaccessible to normal use immediately after deletion is confirmed unless the user explicitly chooses restoration.
- **SC-007**: 100% of tested deleted-account sign-in attempts during the 6-month retention window show the deleted-account message and restore option instead of creating a new account automatically.
- **SC-008**: 100% of tested deleted accounts are permanently removed once the 6-month retention window expires.
- **SC-009**: 100% of tested account create, delete, restore, and sign-in outcome events generate an audit record containing Tenant Id, User Id, timestamp, and outcome.
- **SC-010**: Audit records for account lifecycle and sign-in outcome events remain available for at least 12 months in all tested environments.

## Assumptions

- The app can determine whether it is running in commercial mode or self-hosted mode before choosing which sign-in experience to show.
- The existing Microsoft 365 sign-in path remains the identity source for both deployment types.
- Future tenant-level changes may apply across all users linked to the same tenant, so tenant identity is part of the long-term account model.
- Deleting an app account removes the user's access to the commercial service immediately, but permanent record removal happens only after the 6-month retention window; during that retention period the user may explicitly restore the same account, and none of this removes their Microsoft 365 identity or data held outside this app.
- The initial release only needs a minimal profile view and does not require preferences, billing details, or other extended profile fields.
- The tenant name, email address, and other user-facing identity details may be available for display during a signed-in session without being saved as part of the long-term account record.
- Audit retention applies to account lifecycle and sign-in outcome records even though the primary app account record remains minimal.
