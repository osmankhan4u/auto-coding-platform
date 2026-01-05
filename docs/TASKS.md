# Tasks — Codex Execution List (Do in Order)

## Task 01 — Create Terminology.Api project (Minimal API)
**Goal:** Create src/Services/Terminology.Api with minimal API scaffold.
**DoD:**
- dotnet build succeeds
- /health endpoint returns OK
- appsettings.json includes TerminologyDb connection string

## Task 02 — EF Core entities + DbContext for Terminology
**Goal:** Implement entities + mappings to Postgres schema per SPEC.
**Files:**
- Data/TerminologyDbContext.cs
- Data/Entities/*.cs
- Data/TerminologySearchRow.cs (keyless)
**DoD:**
- Migrations create tables and columns
- Context connects and can run a simple query

## Task 03 — Migrations for extensions + indexes
**Goal:** EF migration includes enabling pg_trgm, unaccent, vector extensions.
**DoD:**
- migration applies cleanly
- indexes exist for search_tsv, trigram, vector

## Task 04 — Implement POST /terminology/search (Hybrid SQL)
**Goal:** Implement TerminologySearchService with FromSqlRaw hybrid query.
**DoD:**
- Parameterized SQL only
- Supports filters: isBillableOnly/excludeHeaders
- Returns matchModes + matchedTerms
- Score is 0..1

## Task 05 — Add Embedding Provider abstraction
**Goal:** Implement IEmbeddingProvider + FakeEmbeddingProvider (local dev).
**DoD:**
- API runs without external dependencies
- Query returns results (requires loaded sample data)

## Task 06 — Coding.Worker skeleton
**Goal:** Create src/Services/Coding.Worker with worker host.
**DoD:**
- dotnet run starts worker
- config includes Terminology base URL

## Task 07 — Implement Coding pipeline (ICD rules)
**Goal:** SafetyGate + RadiologyCodingService + contracts.
**DoD:**
- Primary candidates only from INDICATION
- Secondary suggestions only
- Safety gate blocks auto primary when required
- Output JSON matches SPEC

## Task 08 — Local runner for Coding.Worker
**Goal:** Read extracted JSONs from samples/_in and output results to samples/_out.
**DoD:**
- For each input JSON, output icd json
- Logs errors but continues processing

## Task 09 — Add unit tests (minimum)
**Goal:** Test Terminology search service SQL parameterization + Coding policy.
**DoD:**
- tests run in CI locally
- no flaky tests
