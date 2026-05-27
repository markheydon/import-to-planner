---
layout: page
title: Troubleshooting
permalink: /troubleshooting
---

Use this page when something does not work as expected.

## Sign-in or permission issues

Symptoms:

- You cannot sign in.
- You are returned to sign-in repeatedly.
- You see a permissions or consent message.

What to check:

- You are using a Microsoft 365 work or school account.
- Your account has access to Planner.
- Your tenant allows user consent, or an administrator has granted consent for the app.

If consent is blocked by policy, contact your administrator and share the exact message shown.

## No groups found

If the app shows No groups found, your account may not belong to any eligible Microsoft 365 group.

Try the following:

1. Confirm you are a member of at least one group or team.
2. Sign out and sign in again.
3. Ask your administrator to verify your group membership and Planner access.

## CSV validation failures

Common causes:

- Missing Task Name header.
- Unsupported or misspelled column headings.
- Empty task names.
- Invalid priority values.

Fix the file using the [CSV format](./csv-format) guide, then upload again.

## Duplicate handling questions

The app can detect existing matching tasks and report them as reused rather than creating a duplicate.

Check the execution report for created, reused, and skipped outcomes.

## Temporary API or throttling issues

If Microsoft Graph or Planner is temporarily busy, imports may fail or partially complete.

Try again after a short wait. If the issue continues, capture the time and error summary and contact support.

## Still stuck?

See [FAQ](./faq) for common questions, or contact your support team with:

- The step where the issue happened.
- The exact error text.
- Approximate time of the attempt.
