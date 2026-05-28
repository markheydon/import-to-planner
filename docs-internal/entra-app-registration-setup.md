# Microsoft Entra App-Registration Setup

This guide explains how to configure Microsoft Entra app registrations for the two supported operating modes in this repository:

- self-hosted single-tenant deployments
- hosted shared multi-tenant deployments

Recommended approach: keep these as separate app registrations. That keeps self-hosted ownership simple and prevents a shared hosted deployment from inheriting tenant-specific authority settings.

## Why this matters

The app uses `AzureAd:HomeTenantId` to choose its sign-in authority:

- `multiple` means shared hosted mode for work or school tenants
- a concrete tenant ID or verified domain means self-hosted single-tenant mode

If a hosted deployment uses a concrete `AzureAd:HomeTenantId` value instead of `multiple`, users from other tenants are redirected through a tenant-specific sign-in flow and may see an error like:

> Selected user account does not exist in tenant ... and cannot access the application ... in that tenant.

That error usually means the hosted runtime authority is still tenant-specific, even if the app registration permissions have already been widened.

## Mode matrix

| Use case | Recommended registration | Supported account types | Authority settings | Redirect-URI rule |
| --- | --- | --- | --- | --- |
| Self-hosted for one organisation | Separate tenant-owned registration | Accounts in this organisational directory only | `AzureAd:TenantId=<tenant>` and `AzureAd:HomeTenantId=<tenant>` | Add every real app origin used by that deployment, ending with `/signin-oidc` |
| Shared hosted deployment | Separate shared hosted registration | Accounts in any organisational directory | `AzureAd:TenantId=<app-registration-tenant>` and `AzureAd:HomeTenantId=multiple` | Add every hosted origin, ending with `/signin-oidc` |

Common values for both modes:

- `AzureAd:Instance=https://login.microsoftonline.com/`
- `AzureAd:CallbackPath=/signin-oidc`
- Delegated Graph permissions: `User.Read`, `Group.Read.All`, `GroupMember.Read.All`, `Tasks.ReadWrite`

## Shared hosted multi-tenant setup

Use this for the shared service deployment that admits supported work or school tenants.

### 1. Create or update the hosted app registration

- App type: Web
- Supported account types: `Accounts in any organisational directory`
- Client ID: use the hosted registration's application ID in `AzureAd:ClientId`

### 2. Add redirect URIs

Add one redirect URI for each real hosted origin. The callback path is fixed by the app and must end with `/signin-oidc`.

Examples:

- `https://app.importplanner.app/signin-oidc`
- `https://app-staging.importplanner.app/signin-oidc`

If you run a local hosted-style verification against a real app registration, register the exact local HTTPS origin used for that run as well.

### 3. Add delegated Microsoft Graph permissions

The hosted registration should request these delegated scopes:

- `User.Read`
- `Group.Read.All`
- `GroupMember.Read.All`
- `Tasks.ReadWrite`

These are the scopes currently configured in `src/ImportToPlanner.Web/appsettings.json`.

### 4. Decide how consent is granted

- If a customer tenant allows user consent for the required scopes, the user can complete delegated consent during sign-in.
- If a customer tenant blocks user consent, the app must send the user to an administrator-consent path instead.

The app can build an administrator-consent URI automatically from `AzureAd:Instance`, effective authority tenant, and `AzureAd:ClientId`. In hosted shared mode that means the URI is built against `organizations` when `AzureAd:HomeTenantId=multiple`, unless you supply an explicit `AzureAd:AdminConsentUri`.

### 5. Set hosted runtime configuration

For the hosted web app:

- `AzureAd:TenantId=<hosted-app-registration-tenant>`
- `AzureAd:HomeTenantId=multiple`
- `AzureAd:ClientId=<hosted-app-client-id>`
- `AzureAd:Instance=https://login.microsoftonline.com/`
- `AzureAd:CallbackPath=/signin-oidc`

Important: keep `AzureAd:TenantId` aligned with the hosted app registration's home tenant. Runtime shared authority is controlled by `AzureAd:HomeTenantId=multiple`.

## Self-hosted single-tenant setup

Use this when one organisation deploys and operates its own copy of the app.

### 1. Create or use a tenant-owned registration

- App type: Web
- Supported account types: `Accounts in this organisational directory only`
- Client ID: use this registration's application ID in `AzureAd:ClientId`

### 2. Add redirect URIs

Add the exact redirect URI for each self-hosted web origin. The callback path remains `/signin-oidc`.

Examples:

- `https://planner.contoso.example/signin-oidc`
- `https://localhost:5001/signin-oidc` if that is the exact local HTTPS origin you run

### 3. Add delegated Microsoft Graph permissions

Use the same delegated Graph scopes as the hosted path:

- `User.Read`
- `Group.Read.All`
- `GroupMember.Read.All`
- `Tasks.ReadWrite`

Grant consent in the owning tenant before rolling the app out to users.

### 4. Set self-hosted runtime configuration

For the self-hosted web app:

- `AzureAd:TenantId=<tenant-id-or-verified-domain>`
- `AzureAd:HomeTenantId=<tenant-id-or-verified-domain>`
- `AzureAd:ClientId=<tenant-owned-app-client-id>`
- `AzureAd:Instance=https://login.microsoftonline.com/`
- `AzureAd:CallbackPath=/signin-oidc`

This keeps sign-in restricted to the owning tenant and preserves the self-hosted baseline described in the specs and tests.

## Converting an existing single-tenant registration

If you already have a single-tenant registration that you used for testing, you can convert it for shared hosted use by changing the supported account types to `Accounts in any organisational directory`, updating the redirect URIs, and making sure the hosted deployment itself uses `AzureAd:HomeTenantId=multiple`.

Even so, the recommended steady-state approach is still to split registrations:

- one shared multitenant registration for the hosted service
- one tenant-owned single-tenant registration for each self-hosted deployment

That separation keeps consent ownership, redirect URIs, and operational risk easier to reason about.

## Troubleshooting

| Symptom | Likely cause | What to check |
| --- | --- | --- |
| `Selected user account does not exist in tenant ...` during hosted sign-in | Hosted runtime authority is using a concrete home tenant instead of `multiple`, or the app registration is still single-tenant | Check deployed `AzureAd:HomeTenantId`, the app registration supported account type, and the redirect URI used by the sign-in flow |
| Hosted sign-in works for the home tenant only | Deployment is tenant-specific even though the app registration was changed | Check AppHost and workflow parameter mapping for `AzureAd__HomeTenantId` |
| Hosted users get blocked after sign-in with a permissions error | Required Graph permissions are missing or consent has not been granted in that tenant | Check delegated Graph permissions and whether user consent or admin consent is required |
| Self-hosted users can sign in from the wrong tenant | Self-hosted deployment is using `AzureAd:HomeTenantId=multiple` or the wrong registration | Check `AzureAd:TenantId` and `AzureAd:HomeTenantId`, and confirm the self-hosted registration is single-tenant |

## Related repository guidance

- [ACA staging runbook](aca-staging-runbook.md)
- [Aspire production readiness](aspire-production-readiness.md)
- [Microsoft Graph guidelines](microsoft-graph-guidelines.md)
- [Repository README](../README.md)
