---
title: Import workflow
---

This guide explains the five-step import journey from selection to final report.

## Step 1: Select group

Choose the Microsoft 365 group where the destination plan lives.

## Step 2: Enter plan name

Enter the plan name you want to use.

If a plan with that name already exists in the selected group, the app can reuse it.

## Step 3: Upload CSV

Upload your CSV file with the supported headings.

## Step 4: Validate and preview

The app validates your file and shows a preview before any write action is confirmed.

Use this step to correct issues before execution.

## Step 5: Confirm and execute

When you confirm, the app starts the import and then shows an execution report.

You can download that execution report as a CSV to keep a record, share outcomes with colleagues, or fix your source file before you upload again.

## Understanding report outcomes

- Created: a new task was created in Planner.
- Reused or skipped: this combined outcome covers rows where an existing match was reused, and rows that were not created.

## Plan reuse behaviour

You can import into an existing plan by using the same plan name in the same group.

## Manual follow-up

Some goal-related links or post-import housekeeping may still need manual follow-up after execution.

## Workflow illustrations

The screenshots below were captured with **demonstration mode** enabled on a local
non-production host. Names, plans, and CSV rows are synthetic and are not from a live
Microsoft 365 tenant.

### Step 1: Select Planner location

![Select Planner location in demonstration mode](/import-workflow/step-1-select-location.png)

### Step 2: Select plan

![Select plan in demonstration mode](/import-workflow/step-2-select-plan.png)

### Step 3: Upload CSV

![Upload CSV in demonstration mode](/import-workflow/step-3-upload-csv.png)

### Step 4: Preview and confirm

![Preview and confirm in demonstration mode](/import-workflow/step-4-preview-and-confirm.png)

### Step 5: Execution report

![Execution report in demonstration mode](/import-workflow/step-5-execution-report.png)

Maintainers can refresh these images with `./scripts/capture-demo-workflow-screenshots.sh`
(documented in the repository engineering guide `docs/demo-mode.md`).

Need help with errors? Continue to [Troubleshooting](./troubleshooting).
