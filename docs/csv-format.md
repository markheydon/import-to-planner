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
- Description (optional)
- Priority (optional)
- Bucket (optional)
- Goal (optional)

Use these exact headings in the first row.

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
- Wrong delimiter: use commas unless your export tool is configured differently and your app instance supports it.
- Extra columns: unexpected columns are ignored for import, but keeping only supported columns reduces confusion.
- Empty task names: each row must include a task name.

Next step: see [Import workflow](./import-workflow) for the full in-app process.
