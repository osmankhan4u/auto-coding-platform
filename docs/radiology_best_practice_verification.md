# Radiology Best Practice Verification (MVP)

## PR Description
- Summary: Added radiology attribute extraction (laterality/contrast/views/guidance/intervention), deterministic CPT mapping with audit fields, bundling validation rules, a payer rules engine scaffold, and a 10-case best-practice test suite with sample reports.
- Risks: CPT mapping is intentionally narrow; contrast/laterality regexes may miss uncommon phrasing; ICD specificity still depends on Terminology API scoring.
- Testing: Added `RadiologyBestPracticeVerificationTests` (not run in this environment).

## Verification Matrix

### A) Extraction Fields
| Checklist Item | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| A1. modality (XR/CT/MRI/US/NM/IR) | Implemented | `src/Services/Extraction.Worker/Services/ModalityBodyRegionExtractor.cs`, `ModalityBodyRegionExtractor.Extract` | Regex-based coverage for XR/CT/MRI/US/NM/IR; expand modality patterns if additional modalities appear. |
| A2. body_part(s) (supports multi-region) | Partial | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Multi-region list supports abdomen/pelvis and a small body-region set; add more regions and anatomic synonyms in `BodyRegionPatterns`. |
| A3. laterality (RT/LT/Bilateral/None) | Implemented | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Laterality cues map to RT/LT/BILATERAL; extend patterns for spelled-out variants if needed. |
| A4. contrast_state (without/with/with_and_without/unknown) | Implemented | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Technique section regex maps to WITH/WITHOUT/WITH_AND_WITHOUT; add vendor-specific phrases as needed. |
| A5. views_or_completeness (x-ray views; US complete vs limited) | Partial | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Handles X-ray 2/3/4+ views and US complete/limited; expand for other view counts and modalities. |
| A6. guidance_flag (image guidance used) | Implemented | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Detects guidance terms with evidence spans; add more guidance synonyms if needed. |
| A7. intervention_flag (biopsy/drainage/injection/therapeutic vs diagnostic) | Partial | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `RadiologyAttributesExtractor.Extract` | Flags intervention keywords but does not classify diagnostic vs therapeutic; add a typed intervention model. |
| A8. clinical_indication (reason for exam) | Implemented | `src/Services/Extraction.Worker/Services/RadiologyExtractionService.cs`, `RadiologyExtractionService.Extract` | Uses Indication section + `IndicationEvidenceSpans`; consider parsing discrete indication concepts if needed. |
| A9. impression_concepts (primary diagnoses candidates) | Implemented | `src/Services/Extraction.Worker/Services/RadiologyExtractionService.cs`, `RadiologyExtractionService.Extract` | `ImpressionConcepts` populated from extracted concept list. |
| A10. negation_and_uncertainty (rule out / no evidence of / cannot exclude) | Implemented | `src/Services/Extraction.Worker/Services/NegationScopeResolver.cs`, `UncertaintyScopeResolver.cs` | Added rule-out and no-evidence cues; extend as needed for local style guides. |
| A11. evidence_spans (support for each extracted field) | Partial | `src/Services/Extraction.Worker/Models/ExtractedRadiologyEncounter.cs`, `RadiologyAttributesExtractor.Extract` | Evidence spans now provided for modality/body region/laterality/contrast/views/guidance/intervention and concepts; add spans for any new fields introduced. |

### B) CPT Selection
| Checklist Item | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| B1. CPT chosen based on structured fields | Implemented | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `RadiologyCptCodingService.Generate` | Deterministic mapping uses modality/body region/contrast/views. |
| B2. CT/MRI contrast differentiation | Implemented | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `MapCtChest`, `MapCtAbdPelvis`, `MapMriShoulder` | CT/MRI CPTs reflect WITH/WITHOUT/WITH_AND_WITHOUT. |
| B3. Multi-region logic | Partial | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `MapCtAbdPelvis` | Handles abdomen+pelvis; expand for other multi-region combinations. |
| B4. US complete vs limited logic | Implemented | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `MapUsAbdomen` | Maps US complete/limited to CPT 76700/76705. |
| B5. X-ray view counting logic | Implemented | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `MapXrKnee` | Maps view counts to knee CPTs; extend for other XR regions. |
| B6. IR diagnostic vs therapeutic + guidance coding | Partial | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `TryMapIrProcedure`, `MapGuidanceAddOn` | Intervention and guidance detected; lacks full diagnostic vs therapeutic separation. |
| B7. Add-on/3D reconstruction handling (if in scope) | Missing | N/A | Add 3D reconstruction add-ons and document inclusion/exclusion rules. |
| B8. Fallback behavior documented | Partial | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `CptCodingResult.ExclusionReasons` | Exclusion reasons captured; expand to include explicit fallback codes if policy allows. |

### C) ICD-10 Selection
| Checklist Item | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| C1. Prefer Impression over Indication | Implemented | `src/Services/Coding.Worker/Services/RadiologyCodingService.cs`, `GenerateAsync` | Primary candidates prefer impression concepts; fallback to indication. |
| C2. Do NOT code ruled-out diagnoses | Implemented | `src/Services/Coding.Worker/Services/RadiologyIcdPolicy.cs`, `IsEligibleForPrimary` | Ruled-out and negative concepts excluded. |
| C3. Handle negation and uncertainty correctly | Implemented | `src/Services/Extraction.Worker/Services/ClinicalConceptExtractor.cs`, `NegationScopeResolver` | Negation/uncertainty propagation drives concept certainty. |
| C4. Choose highest specificity where supported | Partial | `src/Services/Coding.Worker/Services/RadiologyCodingService.cs`, `BuildCandidatesAsync` | Depends on terminology ranking; add specificity tie-breakers if required. |
| C5. Avoid incidental findings unless in scope | Implemented | `src/Services/Coding.Worker/Services/RadiologyIcdPolicy.cs`, `IsEligibleForPrimary` | Incidental concepts excluded from primary/secondary. |
| C6. Provide evidence span per ICD selection | Implemented | `src/Services/Coding.Worker/Contracts/IcdCandidate.cs` | Evidence spans propagated from extracted concepts. |

### D) Modifiers & Billing Context
| Checklist Item | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| D1. Support 26/TC/global context | Partial | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `BuildModifiers` | Supports 26/TC/global via `BillingContext`; extraction currently defaults to GLOBAL. |
| D2. Laterality modifiers where required | Implemented | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs`, `BuildModifiers` | RT/LT/Bilateral modifiers applied for CPT selection. |
| D3. Repeat procedure modifiers if relevant | Missing | N/A | Add repeat-procedure detection + modifier rules per policy. |
| D4. Modifier rationale/evidence output | Partial | `src/Services/Coding.Worker/Contracts/CptCodingResult.cs`, `CptCodeSelection` | Rationale/evidence spans included per CPT; add explicit modifier-level rationale if required. |

### E) Compliance & Auditability
| Checklist Item | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| E1. NCCI/bundling validation step exists | Implemented | `src/Services/Coding.Worker/Services/IBundlingValidator.cs`, `src/Services/Coding.Worker/Services/BundlingValidator.cs` | Minimal NCCI/bundling rules applied (duplicate CPT and guidance bundling); expand ruleset for broader coverage. |
| E2. Code outputs include rule_id, rule_version, confidence, evidence_span, exclusion reasons | Partial | `src/Services/Coding.Worker/Contracts/IcdCandidate.cs`, `src/Services/Coding.Worker/Contracts/CptCodingResult.cs` | Fields added; expand `ExclusionReasons` for ICD selection logic as needed. |
| E3. End-to-end tests for >=10 tricky reports | Implemented | `tests/RadiologyBestPracticeVerificationTests.cs`, `src/Services/Extraction.Worker/samples/_in` | 10 de-identified sample reports with CPT/ICD assertions. |

## Article Crosswalk: "Mastering Radiology Coding: 12 Essential Guidelines"
Source: https://curogram.com/blog/mastering-radiology-coding-guidelines (provided by user)

| Guideline | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| 1. Documentation completeness (indication/findings/impression clarity) | Partial | `src/Services/Extraction.Worker/Services/SectionDetector.cs`, `src/Services/Extraction.Worker/Services/DocumentationCompletenessScorer.cs` | Section detection + completeness scoring exist; add patient/DOS/procedure metadata fields if required for audit. |
| 2. Diagnostic vs interventional distinction | Partial | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs` | Intervention/guidance flags exist; add diagnostic vs therapeutic classification and component coding. |
| 3. Modifiers (26/TC/RT/LT) | Partial | `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs` | Modifiers applied by `BillingContext`/laterality; extraction currently defaults to GLOBAL and lacks modifier-level rationale. |
| 4. ICD-10-CM specificity | Partial | `src/Services/Coding.Worker/Services/RadiologyCodingService.cs` | Specificity depends on terminology ranking; add tie-breakers or explicit specificity scoring. |
| 5. NCCI bundling/unbundling | Partial | `src/Services/Coding.Worker/Services/BundlingValidator.cs` | Minimal rules enforced (duplicate CPT, guidance bundling); expand to full NCCI edits and quarterly updates. |
| 6. Stay current with code updates | Missing | N/A | Add versioned update cadence and configuration for CPT/ICD/HCPCS/LCD/NCD changes. |
| 7. With/without contrast accuracy | Implemented | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs`, `src/Services/Coding.Worker/Services/RadiologyCptCodingService.cs` | Contrast state extracted and used for CPT mapping. |
| 8. Component coding for interventional procedures | Missing | N/A | Add IR component model (primary, S&I, catheter, supply) + bundling logic. |
| 9. Medical necessity alignment | Partial | `src/Services/Coding.Worker/Services/RadiologyCodingService.cs` | ICDs generated from indication/impression; no CPT/ICD necessity validator. |
| 10. MIPS/quality reporting codes | Missing | N/A | Add optional quality code generation rules if in scope. |
| 11. Operative report detail capture for IR | Partial | `src/Services/Extraction.Worker/Services/RadiologyAttributesExtractor.cs` | Captures guidance/laterality/intervention but not full operative component parsing. |
| 12. Denial management/audit readiness | Partial | `src/Services/Coding.Worker/Contracts/IcdCandidate.cs`, `src/Services/Coding.Worker/Contracts/CptCodingResult.cs` | Evidence/rationale fields exist; add denial analytics loop and audit-ready exports if required. |

## Competitive Capability Gaps (Auto-Coding AI)
| Capability | Status | Evidence | Notes/Recommendations |
| --- | --- | --- | --- |
| Clinical semantic understanding | Partial | `src/Services/Extraction.Worker/Services/ClinicalConceptExtractor.cs`, `src/Services/Terminology.Api/Services/TerminologySearchService.cs` | Rule-based extraction + terminology search; add richer NLP/ontology normalization and broader concept packs. |
| Workflow integration | Missing | `src/Services/Extraction.Worker/Worker.cs`, `src/Services/Coding.Worker/Worker.cs` | File-based sample pipeline only; add EHR/FHIR/HL7 ingestion, queueing, and review UI integration. |
| Payer-specific rules engine | Partial | `src/Services/Coding.Worker/Services/RulesEngine.cs`, `src/Services/Coding.Worker/Models/ClaimContext.cs`, `src/Services/Coding.Worker/appsettings.json` | Canonical claim model + layered rule packs + evaluation contract added; expand rule types for auth, POS, frequency, and client overrides. |
| Auto-scrubbing & denial prediction | Missing | N/A | Implement pre-claim edits (NCCI/LCD/medical necessity) and denial prediction heuristics/models. |
| AI augmentation of coder decision-making | Partial | `src/Services/Coding.Worker/Contracts/DecisionTrace.cs`, `IcdCandidate` | Evidence/trace output exists; add interactive UI, confidence routing, and feedback loop. |

## Payer Rules Engine (MVP)
- Canonical claim model: `src/Services/Coding.Worker/Models/ClaimContext.cs`
- Rule pack schema: `src/Services/Coding.Worker/Services/RulesOptions.cs`
- Evaluation contract: `src/Services/Coding.Worker/Contracts/RuleEvaluationResult.cs`
- Layered execution order: GLOBAL -> NCCI_MUE -> PAYER -> CLIENT (`src/Services/Coding.Worker/Services/RulesEngine.cs`)
