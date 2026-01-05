# Radiology Coding AI — Design Specification (MVP v1)

## 0. Purpose
Build a production-grade, audit-defensible Radiology Coding Automation solution for US RCM. MVP focuses on:
- Radiology-only scope
- Modality-specific extraction (US/CT/MRI first)
- ICD-10-CM coding (Primary + Secondary suggestions only)
- Terminology service with lexical + semantic retrieval (Postgres FTS + trigram + pgvector)

## 1. MVP Scope
### In scope
1) Extraction Worker (Radiology)
- Parse radiology report into sections
- Extract modality + body region + technique elements
- Extract clinical concepts with safety tagging:
  - Negation scope
  - Uncertainty scope
  - History scope
  - Conjunction/list scope expansion
  - Target-aware negation patterns
- Modality/body-region concept packs for controlled vocabulary
- Pack coverage warnings (GLOBAL-only fallback)

2) Coding Worker (Radiology ICD)
- Uses extraction output to generate ICD-10-CM code candidates
- Strict "indication-first" rules:
  - Primary ICD candidates from INDICATION concepts only
  - Secondary ICDs are suggestions only (never auto-finalized) in MVP
- Safety gate:
  - Missing indication => block auto primary
  - Global-only pack fallback => block auto primary
  - Low documentation completeness => block auto primary
- Evidence spans and decision trace output

3) Terminology API
- POST /terminology/search
- Hybrid retrieval:
  - Lexical: Postgres full-text search + trigram + aliases
  - Semantic: pgvector embeddings ANN
- Merge + rerank in SQL with deterministic scoring
- Filters: billable only, exclude headers
- Versioning: codeVersionId required (e.g., ICD10CM_2026)

### Out of scope (MVP)
- CPT/HCPCS automation
- Payer policy engines (LCD/NCCI) beyond placeholders
- Full EHR integrations (HL7/FHIR)
- Human coder UI (only contracts produced)
- Automated claim submission

## 2. Architecture (Services)
Repo structure (A):
- src/Services/Extraction.Worker
- src/Services/Coding.Worker
- src/Services/Terminology.Api

### Data flow
Radiology Report -> Extraction Worker -> ExtractedRadiologyEncounter JSON ->
Coding Worker -> RadiologyIcdCodingResult JSON -> downstream UI/RCM

Coding Worker calls Terminology API: POST /terminology/search.

## 3. Contracts (Key)
### 3.1 Clinical Concept assertion fields (Extraction output)
- Certainty: CONFIRMED | SUSPECTED | RULED_OUT
- Polarity: POSITIVE | NEGATIVE
- Temporality: CURRENT | HISTORY
- SourcePriority: INDICATION | IMPRESSION | FINDINGS
- Relevance: INDICATION_RELATED | INCIDENTAL | UNCLEAR
- EvidenceSpans: span ids referencing original text

### 3.2 Pack coverage warnings (Extraction)
- CONCEPT_PACK_FALLBACK_GLOBAL_ONLY
- CONCEPT_PACK_NO_MATCH

### 3.3 Coding output (Coding Worker)
RadiologyIcdCodingResult:
- PrimaryCandidates: ranked ICD candidates (from INDICATION only)
- SecondaryCandidates: ranked ICD candidates (suggestions only)
- FinalSelection:
  - PrimaryIcd: selected if safe
  - SecondaryIcds: ALWAYS empty in MVP
  - RequiresHumanReview: if no primary selected
- SafetyFlags: e.g., MISSING_INDICATION, PACK_FALLBACK_GLOBAL_ONLY, LOW_DOCUMENTATION_COMPLETENESS
- Trace: policy decisions + terminology queries (optional)

### 3.4 Terminology API request/response
POST /terminology/search
Request:
- codeSystem: "ICD10CM"
- codeVersionId: "ICD10CM_2026"
- dateOfService: YYYY-MM-DD
- queryText: free text
- topN: 1..50
- filters: isBillableOnly/excludeHeaders (strings)

Response: list of TerminologyHitDto:
- code, shortDescription, longDescription
- score (0..1)
- matchModes: ["FTS","VECTOR","ALIAS"]
- matchedTerms: ["ruq pain", ...]

## 4. Radiology Extraction Design (Deterministic Safety)
### 4.1 Sentence-scoped extraction
- Split section text into sentences
- Compute neg/uncert/history within sentence only
- Avoid context bleed across sentences/clauses

### 4.2 Negation/Uncertainty/History Scopes
Implement cue + scope with:
- scope terminators: but/however/except/although/yet, punctuation . ; :
- list handling: comma, and/or
- stop scope at strong positive clause cues (e.g., "is present", "are seen") after comma

### 4.3 Target-aware negation patterns
Avoid false negation:
- "Fracture without displacement" => fracture is positive; displacement negated (if modeled)
- "Pneumothorax not seen" => pneumothorax negated
- "Negative for X" => X negated

### 4.4 Modality/body-region concept packs
- GLOBAL pack always applied
- Modality and region packs merged, de-duped
- If only GLOBAL pack applies => warning CONCEPT_PACK_FALLBACK_GLOBAL_ONLY

## 5. Coding Worker ICD Rules (MVP)
### 5.1 Primary ICD selection
- Only from INDICATION concepts
- Exclude negated/rule-out
- SUSPECTED in indication is allowed as candidate but penalized
- Prefer symptom ICD over suspected diagnosis when both exist
- Safety gate may block auto primary; then RequiresHumanReview=true

### 5.2 Secondary ICDs
- Suggestions only (never auto-finalize)
- May be derived from confirmed/current findings/impression but returned only in SecondaryCandidates list

## 6. Terminology DB & Search (Hybrid)
### 6.1 Schema (Postgres)
Tables:
- terminology_code_version
- terminology_concept (ICD concepts) with search_text and search_tsv
- terminology_alias (synonyms)
- terminology_embedding (pgvector vectors by code + model)

Extensions:
- pg_trgm, unaccent, vector (pgvector)

Indexes:
- GIN on search_tsv
- GIN trigram on search_text and alias_norm
- HNSW or IVFFLAT on vector

### 6.2 Hybrid ranking
Compute:
- fts_rank (ts_rank_cd)
- trgm_sim (similarity)
- vec_sim (1 - cosine_distance)

Final score:
score = 0.55*fts + 0.15*trgm + 0.30*vec + aliasBoost
aliasBoost = +0.05 if trgm_sim > 0.50

Must be deterministic, parameterized, safe.

## 7. Non-negotiable Best Practices
- No hallucinated codes. Only from terminology.
- Never code negated/rule-out concepts.
- Never auto primary if missing indication or pack fallback global only.
- All results include evidence spans and/or match reasoning.
- All SQL is parameterized. No string interpolation.
- Add unit tests for scope logic and coding policy.
