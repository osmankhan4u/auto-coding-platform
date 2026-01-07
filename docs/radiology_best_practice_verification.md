# Radiology Best Practice Verification (MVP)

## PR Description
- Summary: Added radiology attribute extraction (laterality/contrast/views/guidance/intervention), deterministic CPT mapping with audit fields, bundling validator stub, and a 10-case best-practice test suite with sample reports.
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
| E1. NCCI/bundling validation step exists | Partial | `src/Services/Coding.Worker/Services/IBundlingValidator.cs`, `BundlingValidator.Validate` | Interface + placeholder implementation with TODO note. |
| E2. Code outputs include rule_id, rule_version, confidence, evidence_span, exclusion reasons | Partial | `src/Services/Coding.Worker/Contracts/IcdCandidate.cs`, `src/Services/Coding.Worker/Contracts/CptCodingResult.cs` | Fields added; expand `ExclusionReasons` for ICD selection logic as needed. |
| E3. End-to-end tests for >=10 tricky reports | Implemented | `tests/RadiologyBestPracticeVerificationTests.cs`, `src/Services/Extraction.Worker/samples/_in` | 10 de-identified sample reports with CPT/ICD assertions. |
