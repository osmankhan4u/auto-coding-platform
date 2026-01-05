# Build Plan — Radiology Coding AI (MVP)

## Phase 0 — Local dev baseline
- .NET 8 SDK installed
- Docker Desktop installed
- Postgres container with pgvector enabled
- Solution layout:
  - src/Services/Extraction.Worker
  - src/Services/Coding.Worker
  - src/Services/Terminology.Api
  - docs/

## Phase 1 — Terminology API (must complete first)
1) Create Terminology.Api project (Minimal API)
2) EF Core entities + DbContext mappings
3) Database migrations (extensions + tables)
4) Implement POST /terminology/search using EF Core keyless entity + FromSqlRaw hybrid SQL
5) Add local FakeEmbeddingProvider to make endpoint runnable without OpenAI
6) Add /health endpoint (DB connectivity + extensions + codeVersionId exists)

## Phase 2 — Coding Worker (ICD only)
1) Create Coding.Worker project
2) Implement SafetyGate, policy, scoring
3) Implement RadiologyCodingService calling Terminology API
4) Local runner: reads extracted JSON from samples/_in and writes icd.json to samples/_out
5) Ensure Secondary ICDs are suggestions only (never auto-finalized)

## Phase 3 — Extraction Worker hardening (already designed)
- Ensure extraction output matches spec (assertions, packs, warnings)
- Ensure evidence spans are created and stable
- Add sample reports for CT chest, CT abdomen, MRI brain, US abdomen

## Phase 4 — End-to-end local compose
- docker-compose for Postgres + Terminology.Api
- run Extraction.Worker locally to generate extracted JSON
- run Coding.Worker locally to generate ICD output
