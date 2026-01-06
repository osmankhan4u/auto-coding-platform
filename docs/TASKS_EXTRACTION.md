# Tasks — Extraction.Worker (Radiology MVP)

## Task E01 — Radiology report sectioning (Indication / Findings / Impression / Technique)
**Goal:** Robustly detect and extract radiology sections.
**Requirements:**
- Implement SectionDetector that finds common headings:
  - INDICATION / REASON FOR EXAM / CLINICAL HISTORY
  - TECHNIQUE
  - FINDINGS
  - IMPRESSION / CONCLUSION
- Preserve original text spans (start/end indices) for each section.
- If a section is missing, set text="" and add warning in DocumentationCompleteness.
**DoD:**
- Unit tests for at least 8 report formats.
- For missing Indication: warning "MISSING_INDICATION_SECTION".

## Task E02 — Modality + BodyRegion extraction (US/CT/MRI first)
**Goal:** Determine modality and body region to select concept packs.
**Requirements:**
- Extract Modality from headings/technique:
  - CT, MRI, US (Ultrasound), fallback UNKNOWN
- Extract BodyRegion:
  - CHEST, ABDOMEN, ABD_PELVIS, BRAIN_HEAD (minimum set)
- Add evidence span for modality and region signals.
**DoD:**
- Tests for typical phrases (e.g., "CT CHEST W CONTRAST", "MRI BRAIN", "US ABDOMEN").
- If UNKNOWN, add warning "MODALITY_UNKNOWN" or "BODY_REGION_UNKNOWN".

## Task E03 — Sentence splitting & clause boundary detection
**Goal:** Stable sentence segmentation for clinical scope resolution.
**Requirements:**
- Implement SentenceSplitter with rules:
  - Split on . ; : and line breaks
  - Avoid splitting common abbreviations (e.g., "Dr.", "vs.")
- Expose Sentence objects with start/end indices in section text.
**DoD:**
- Unit tests showing deterministic splits.

## Task E04 — Negation scope resolver (sentence-scoped)
**Goal:** Accurate negation determination inside the same sentence/clause.
**Requirements:**
- Implement NegationScopeResolver:
  - pre-negation cues: no, denies, without, negative for, free of, absence of, not
  - post-negation cues: not seen, not identified, absent, not demonstrated
  - scope terminators: but, however, except, although, yet, ; . :
  - list handling: comma + and/or expansions
- Provide API: IsNegated(sentenceText, matchIndex)
**DoD:**
- Tests:
  - "No PE or pneumothorax; pneumonia present" => PE negated, pneumothorax negated, pneumonia positive
  - "Pneumothorax not seen" => negated
  - "Fracture without displacement" => fracture NOT negated

## Task E05 — Uncertainty scope resolver (sentence-scoped)
**Goal:** Tag "cannot exclude", "suggests", "likely" as SUSPECTED.
**Requirements:**
- Implement UncertaintyScopeResolver with cues:
  - cannot exclude, may represent, suspicious for, possible, likely, probable
- Provide API: IsUncertain(sentenceText, matchIndex)
**DoD:**
- Tests covering common radiology impression phrasing.

## Task E06 — History scope resolver (sentence-scoped)
**Goal:** Tag historical/old findings as HISTORY.
**Requirements:**
- Implement HistoryScopeResolver cues:
  - history of, prior, previous, old, chronic, status post, s/p, known
- Provide API: IsHistorical(sentenceText, matchIndex)
**DoD:**
- Tests:
  - "History of CVA" => history
  - "Chronic microvascular changes" => history/ chronic (policy decide)

## Task E07 — Target-aware negation resolver
**Goal:** Prevent wrong negation attachment to the wrong target.
**Requirements:**
- Implement TargetAwareNegationResolver:
  - Detect "no <target>", "negative for <target>", "<target> not seen"
  - Do NOT negate target in patterns like "<target> without <object>"
- Integrate with negation logic: negated if scope OR target-aware triggers.
**DoD:**
- Tests for:
  - "Fracture without displacement"
  - "No fracture. Displacement present." (separate sentence)
  - "Pneumothorax not seen"

## Task E08 — Modality/BodyRegion Concept Packs
**Goal:** Reduce noise by restricting extracted concepts to relevant patterns.
**Requirements:**
- Implement ConceptPackRegistry:
  - GLOBAL pack always
  - Modality packs: CT_COMMON, MRI_COMMON, US_COMMON
  - Region packs: CT_CHEST, CT_ABDOMEN, CT_ABD_PELVIS, MRI_BRAIN, US_ABDOMEN
- Merge + de-dupe patterns by Normalized+Type
**DoD:**
- Tests:
  - CT CHEST includes PE/pneumothorax/pneumonia patterns
  - CT ABDOMEN includes appendicitis/obstruction patterns

## Task E09 — Pack coverage warnings
**Goal:** Emit warning when only GLOBAL pack applied.
**Requirements:**
- Add warning "CONCEPT_PACK_FALLBACK_GLOBAL_ONLY" when only GLOBAL pack applies.
- Add warning "CONCEPT_PACK_NO_MATCH" when zero patterns resolved.
**DoD:**
- Tests demonstrating warnings.

## Task E10 — ClinicalConcept extractor
**Goal:** Extract concepts with assertion tags and evidence spans.
**Requirements:**
- Extract from sections: INDICATION, IMPRESSION, FINDINGS
- For each match:
  - determine certainty: RULED_OUT if negated; SUSPECTED if uncertain; else CONFIRMED
  - temporality: HISTORY if historical cue else CURRENT
  - polarity: NEGATIVE if negated else POSITIVE
  - sourcePriority from section
  - relevance: compute against indication (INDICATION_RELATED/INCIDENTAL)
  - evidence spans: include match text span IDs
- Output: list of ClinicalConcepts
**DoD:**
- End-to-end tests on 6 sample reports.
- Every concept has at least 1 evidence span.

## Task E11 — Documentation completeness scoring
**Goal:** Compute a score and warnings to drive safety gates in Coding Worker.
**Requirements:**
- Score components:
  - Indication present (weight high)
  - Technique present
  - Impression present
  - Signature present (if available)
- Score 0..1 and warnings list
**DoD:**
- Tests:
  - Missing indication => score penalty + warning

## Task E12 — Local runner for Extraction.Worker
**Goal:** Batch-run extraction from text files.
**Requirements:**
- Read ./samples/_in/*.txt
- Write ./samples/_out/{EncounterId}.extracted.json
- Continue on errors
**DoD:**
- Works locally with dotnet run
