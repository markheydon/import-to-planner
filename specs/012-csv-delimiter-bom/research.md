# Research: Detect CSV Delimiter and UTF-8 BOM

## Header-based comma versus semicolon detection

- **Decision**: Before CsvHelper reads records, inspect the first header line (after BOM removal and quote-aware scanning) and count **unquoted** commas and semicolons. Choose comma when only commas appear, semicolon when only semicolons appear, and fail the file when both appear unquoted. When neither appears, treat a single-column header as comma-delimited; treat a header that contains tab or pipe as an unsupported separator.
- **Rationale**: Issue #133 and the spec require an unambiguous choice from the header, not a guess from later rows. Quoted descriptions may contain the other character; those must not flip the separator. Failing closed avoids the current “Task Name column is required” false negative when `Task Name;Description` is parsed as one column.
- **Alternatives considered**:
  - CsvHelper `DetectDelimiter` / `DetectDelimiterValues`: convenient, but it can guess when both characters appear and does not emit the required file-level “could not determine separator” outcome.
  - Locale/culture-based default (for example current UI culture list separator): hosted users are not in a known Excel locale; header evidence is more reliable.
  - In-app delimiter picker (this increment): rejected by FR-012; explicit file-level error is enough for v1.0.

## UTF-8 BOM handling

- **Decision**: Strip a leading U+FEFF from CSV text inside `CsvImportParser` before empty checks, header sniffing, and CsvHelper parsing. Keep the existing 10 MB `StreamReader` upload path; do not rely on it as the only BOM defence.
- **Rationale**: Excel “CSV UTF-8” prepends EF BB BF. `StreamReader` on upload often skips the BOM, but `ParseAsync` also receives strings from tests and any future callers. If U+FEFF remains on the first heading, `PrepareHeaderForMatch` will not match `Task Name`. Stripping in the adapter makes behaviour deterministic for all callers.
- **Alternatives considered**:
  - Upload-only encoding (`new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true)`): helps the UI path but leaves parser tests and non-UI callers unprotected.
  - Requiring users to re-save without BOM: already a docs-only approach and still fails first-run Excel users.

## Quoted fields and CsvHelper configuration

- **Decision**: After the separator is chosen, parse with the existing CsvHelper configuration (invariant culture, trim, ignore blank lines, no header/missing-field exceptions) plus an explicit `Delimiter` of `","` or `";"`. Do not change quote rules; CsvHelper already preserves quoted commas and semicolons.
- **Rationale**: The product already depends on CsvHelper in `ImportToPlanner.Infrastructure.Graph`. The gap is delimiter selection and BOM, not a new parser. Quoted-field behaviour stays a CsvHelper responsibility once `Delimiter` is set.
- **Alternatives considered**: Hand-rolled full CSV parser: larger, riskier, and unnecessary when sniffing only the header and delegating rows to CsvHelper.

## Failure contract versus missing-column errors

- **Decision**: Ambiguous or unsupported separators return a single `ImportValidationError` with `RowNumber = 0` and `Field = "File"` and **do not** run heading validation. Messages tell the user the separator could not be determined or is not supported, and that they should save as comma-separated UTF-8. Missing `Task Name` remains a heading error only after a separator has been chosen.
- **Rationale**: Constitution VII and FR-003/FR-004 require an explicit, actionable file-level failure before preview. The coordinator already blocks preview when `HasErrors` is true and the Home grid already lists `ImportValidationError` rows.
- **Alternatives considered**: Continue parse and let “Task Name column is required” surface: this is the current bug. Throw exceptions: coordinators would need new catch paths and would violate the existing structured-error pattern.

## Layering and Application contract

- **Decision**: Keep `ICsvImportParser.ParseAsync` unchanged. Implement sniffing and BOM stripping entirely in `CsvImportParser`. Do not add Domain types or CsvHelper references to Application.
- **Rationale**: CSV text format is an infrastructure adapter concern (constitution I–III). Application already owns `CsvParseResult` / `ImportValidationError`. Heading alias/mapping (#127) must see already-split, BOM-free headings; detection therefore stays in the parser adapter, before any future mapping use case.
- **Alternatives considered**: New Application service for delimiter policy: extra abstraction with no second implementation. Detect in Web on upload: would duplicate logic and skip tests that call the parser directly.

## Public documentation

- **Decision**: Update `docs/csv-format.md` and `docs/troubleshooting.md` (and FAQ only if a short extra question stays concise) so Excel locale semicolon exports and UTF-8 BOM are described in UK English. Keep embedded sample tables comma-separated without a BOM. Follow the end-user docs skill and the docs-site contract’s CSV/troubleshooting content obligations.
- **Rationale**: FR-010 and SC-005. Troubleshooting already lists CSV validation causes but not delimiter/BOM. The csv-format “wrong delimiter” bullet currently implies comma-only.
- **Alternatives considered**: Docs-only without parser change: rejected by the issue. New docs route: unnecessary; existing `/csv-format` and `/troubleshooting` already own this topic.

## Testing approach

- **Decision**: Extend `CsvImportParserTests` (xUnit v3, built-in Assert) with comma, semicolon, BOM, BOM+semicolon, quoted commas/semicolons, ambiguous header, tab/pipe unsupported, single-column header, and ignore-extra-columns on a semicolon file. Prefer in-string fixtures (including `"\uFEFF"` prefixes) so BOM is not stripped by `File.ReadAllText`. No Playwright; no AppHost tests. Architecture tests stay unchanged unless Application accidentally gains CsvHelper.
- **Rationale**: Engineering policies require the smallest automated check that can fail. Parser behaviour is fully observable through `ICsvImportParser`. Web uses a parser stub, so bUnit would not exercise detection unless the stub were replaced.
- **Alternatives considered**: End-to-end Excel export tests: not repeatable in CI. Encoding-only StreamReader tests: do not cover `ParseAsync`.
