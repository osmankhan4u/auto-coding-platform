# Terminology.Loader

Loads ICD-10-CM tabular + index XML into the Terminology Postgres database used by Terminology.Api.

## Prerequisites
- Docker compose up (Postgres running with pgvector + unaccent + pg_trgm).
- Terminology.Api migrations applied to the same database.
- ICD-10-CM "table and index" zip available locally.

## Commands
Example with the FY2026 files:

```bash
dotnet run --project src/Tools/Terminology.Loader -- \
  --codeSystem ICD10CM \
  --codeVersionId ICD10CM_2026 \
  --effectiveFrom 2025-10-01 \
  --inputZip C:\path\to\icd10cm-table-and-index-2026.zip \
  --modelId fake-embed-1536 \
  --embed false \
  --aliases true
```

To enable embeddings:

```bash
dotnet run --project src/Tools/Terminology.Loader -- \
  --codeSystem ICD10CM \
  --codeVersionId ICD10CM_2026 \
  --inputZip C:\path\to\icd10cm-table-and-index-2026.zip \
  --embed true
```

## Configuration
- Connection string key: `ConnectionStrings:TerminologyDb`
- Supports `appsettings.json` and environment variable override (`ConnectionStrings__TerminologyDb`)

## Troubleshooting
- Missing zip: verify `--inputZip` path and file name match the downloaded CDC zip.
- Missing XML inside zip: ensure the zip contains the tabular and index XML files.
- Permissions: ensure the Postgres user has privileges to insert/update and create indexes.
