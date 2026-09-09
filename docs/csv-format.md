---
layout: page
title: CSV format
permalink: /csv-format
---

Use this guide to build a valid CSV file before upload.

## Required and accepted columns

The required column is:

- Task Name

Accepted columns are:

- Task Name (required)
- Description (optional) — written to the task notes in Planner when provided (maximum 32,768 characters)
- Priority (optional)
- Bucket (optional)
- Goal (optional)

Use these exact headings in the first row.

## Separators and encoding

The app accepts comma-separated or semicolon-separated CSV when the first row makes the choice clear.

- Excel in many UK and EU locales saves CSV with semicolons instead of commas.
- Excel “CSV UTF-8” may start with a UTF-8 byte order mark (BOM). The app ignores a leading BOM.
- Tab-separated and pipe-separated files are not supported. Save as comma-separated UTF-8 if you see a separator error.

If both commas and semicolons appear unquoted in the header row, the app cannot tell which separator you intended. Save the file as comma-separated UTF-8 and upload again.

The examples on this page use comma-separated UTF-8 without a BOM.

## Priority values

Priority accepts either:

- Numbers from 0 to 10.
- Text values: Urgent, Important, Medium, Low.

Text values are case-insensitive.

If the value is not recognised, validation will fail and the row will not be imported until corrected.

## Minimal valid CSV example

```csv
Task Name
Kick-off workshop
Review requirements
```

## Full-featured CSV example

```csv
Task Name,Description,Priority,Bucket,Goal
Prepare release notes,Draft version 1 for review,Urgent,Planning,Launch readiness
Confirm sign-off,Collect final approval from stakeholders,Important,Approvals,Launch readiness
Publish update,Post the final update to users,3,Delivery,Launch readiness
```

## Common mistakes and how to avoid them

- Missing header row: always include headings in the first row.
- Wrong heading names: use the accepted headings exactly.
- Invalid priority value: use 0-10 or one of Urgent/Important/Medium/Low.
- Description too long: keep descriptions within 32,768 characters to match Planner limits.
- Mixed separators in the header: if both commas and semicolons appear unquoted in the first row, save as comma-separated UTF-8.
- Unsupported separator: tab- or pipe-separated files are not supported; save as comma-separated UTF-8.
- Extra columns: unexpected columns can be ignored for import, but keeping only supported columns reduces confusion.
- Empty task names: each row must include a task name.

Next step: see [Import workflow](./import-workflow) for the full in-app process.
