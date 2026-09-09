# Contract: CSV Delimiter and UTF-8 BOM

## Scope

Parser adapter behaviour for CSV text uploaded into the existing import workflow, plus public docs obligations. Does not change Graph, credits, or authentication. Does not add `.xlsx` support.

Traceability: GitHub issue [#133](https://github.com/markheydon/import-to-planner/issues/133); spec FR-001–FR-012.

## Parser boundary (`ICsvImportParser`)

Signature remains:

`ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)`

Returns `CsvParseResult` as today. Callers (including `ImportWorkflowCoordinator`) do not pass a delimiter.

### Preconditions

- `csvContent` is the full file text. Encoding is UTF-8. A leading BOM may or may not still be present as U+FEFF.
- `ignoreExtraColumns` retains existing semantics after a delimiter has been chosen.

### Detection algorithm (normative)

1. If cancellation is requested, throw as today.
2. Remove a single leading U+FEFF if present.
3. If the remaining text is null or whitespace, return the existing empty-file error (`RowNumber = 0`, `Field = "File"`).
4. Read the first header line with quote-aware scanning (standard CSV quotes; doubled quotes inside quoted text).
5. Count unquoted `,` and unquoted `;`.
6. Decision:
   - Unquoted `,` > 0 and unquoted `;` = 0 → delimiter `","`
   - Unquoted `;` > 0 and unquoted `,` = 0 → delimiter `";"`
   - Both > 0 → **ambiguous**: one error, no rows, do not validate headings
   - Both = 0 and the header contains a tab or pipe → **unsupported**: one error, no rows
   - Both = 0 otherwise → delimiter `","` (single-column header)
7. Parse the full content with that delimiter, existing trim/blank-line behaviour, and existing heading/row validation.

Quoted commas or semicolons in the header do not count as unquoted. Quoted commas or semicolons in data rows must remain inside the field.

### File-level error messages (Web displays `Message` as-is)

UK English. Wording may be tightened at implementation provided tests assert the intent below.

| Outcome | Field | RowNumber | Message intent |
| ------- | ----- | --------- | -------------- |
| Ambiguous | `File` | 0 | Separator could not be determined. Save the file as comma-separated UTF-8 and upload again. |
| Unsupported | `File` | 0 | This separator is not supported. Save the file as comma-separated UTF-8 and upload again. |

These errors MUST NOT use `Field = "Task Name"` and MUST NOT say the Task Name column is missing unless heading validation later proves that after a successful delimiter choice.

### Equivalence

A valid semicolon-delimited file MUST produce the same `CsvTaskRow` values (names, descriptions, priorities, buckets, goals, logical row numbers) as the comma-delimited equivalent, aside from the physical separator.

### Non-goals for this contract

- User-selected delimiter parameter
- Additional encodings (Windows-1252, UTF-16)
- Excel workbook (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)
- Heading aliases / mapping (#127) — mapping MUST consume already-split, BOM-free headers from this parser

## Upload size (Web, unchanged)

`OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)` remains the size gate. Oversize files never reach `ParseAsync`.

## UI contract

- No delimiter control on Home.
- Existing validation grid continues to list `ImportValidationError` items; file-level delimiter errors appear there and preview stays blocked (`HasErrors`).
- Sample CSV downloads, if any exist in-app later, MUST be comma-separated UTF-8 without a BOM. Current product samples live in public docs examples only.

## Public docs contract

Align with `specs/007-end-user-docs-site/contracts/docs-site-contract.md`.

### `/csv-format`

MUST additionally:

- State that comma and semicolon separators are accepted when the header makes the choice clear.
- State that Excel in many UK/EU locales saves CSV with semicolons.
- State that Excel “CSV UTF-8” may start with a UTF-8 BOM and that the app ignores it.
- Keep embedded examples comma-separated and BOM-free.
- Replace the “use commas unless your export tool is configured differently” mistake with guidance that matches detection and the ambiguous/unsupported failure.

### `/troubleshooting`

CSV validation failures MUST mention:

- Locale Excel semicolon CSV
- UTF-8 BOM
- File-level separator errors versus a genuinely missing Task Name heading

### `/faq`

MAY add one short question if it stays within the FAQ page’s concise style; not mandatory if csv-format and troubleshooting already cover the topic.

## Security and trust boundary

- CSV text is untrusted user input already parsed in-process. Detection must not execute formulas or load workbooks.
- Error messages MUST NOT echo the full file contents.
- No new secrets, auth, or Graph scopes.

## Self-hosted path

The same `CsvImportParser` registration is used in hosted and self-hosted modes. No commercial or SaaS dependency.
