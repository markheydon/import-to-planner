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
- Invalid due date values (row-level CSV validation, not a file separator error).
- ISO due dates without zero-padded month or day (use `2026-05-31`, not `2026-5-31`).
- Excel serial numbers in the Due Date column — use a recognised date shape instead.
- Locale Excel CSV that uses semicolons — usually fine when the header uses semicolons consistently.
- UTF-8 BOM at the start of the file — the app ignores a leading BOM; you do not need to remove it.
- Mixed commas and semicolons in the header row — the app cannot determine the separator. Save as comma-separated UTF-8.
- Tab- or pipe-separated files — not supported. Save as comma-separated UTF-8.

**File-level separator errors** (for example, “separator could not be determined” or “not supported”) mean the app could not read the file layout. They are different from a **missing Task Name heading**, which only appears after a separator has been chosen.

Fix the file using the [CSV format](./csv-format) guide, then upload again.

## Preview blocked when Assigned To cannot be resolved

If your CSV includes Assigned To addresses and preview fails with a message about destination members or permissions, the app could not load the member list for the selected group or plan.

What to check:

- Your account has permission to read group membership (`GroupMember.Read.All` when admin consent is required).
- The selected destination is available and you still have access.
- Sign out and sign in again if consent or permissions recently changed.

Individual unknown people in Assigned To cells are not a preview block — they appear as manual follow-up after import. Only a failed member lookup blocks preview.

## Unmatched people in Assigned To

When an address does not match a destination member, the row still imports and the execution report lists **Assign person to task** follow-up items. Add the person to the destination in Microsoft 365 first, or assign them manually in Planner after import.

## Due date looks wrong in Planner after import

The app stores due dates at 10:00 UTC on the calendar day from your CSV. Preview shows the date in UK `dd/MM/yyyy` form. Microsoft Planner then displays that timestamp in your signed-in timezone.

For most UK and European accounts, the Planner card shows the same calendar date as the CSV and preview. If you work in an extreme positive UTC offset (for example UTC+14), Planner may show the next calendar day even though preview matched your file. Adjust the CSV date or set the due date manually in Planner if you hit that edge case.

## Duplicate handling questions

The app can detect existing matching tasks and report them as reused rather than creating a duplicate.

Check the execution report for Created and Reused or skipped outcomes.

## Temporary API or throttling issues

If Microsoft Graph or Planner is temporarily busy, imports may fail or partially complete.

Try again after a short wait. If the issue continues, capture the time and error summary and contact support.

## Still stuck?

See [FAQ](./faq) for common questions, or contact your support team with:

- The step where the issue happened.
- The exact error text.
- Approximate time of the attempt.
