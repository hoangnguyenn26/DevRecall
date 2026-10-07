# Internal lesson authoring

Focus: [.NET Backend Foundations](content-curriculum.md). Teach one owned concept and a useful decision: when, why, what goes wrong. Natural conceptual progression does not require Course, Module or prerequisite entities.

## Before publishing

- Specific title/stable slug and a short summary setting expectations; mention assumed familiarity where useful.
- 2–4 observable objectives: explain, decide, identify or demonstrate; not just “understand”.
- Usually 3–6 sections: problem/context, explanation, concrete example, pitfall/tradeoff, takeaway. Avoid definition-only prose.
- Small focused code examples, language-tagged Markdown fences, readable paragraphs and heading hierarchy; no raw HTML, giant controller dumps or fake executable guarantees.
- 1–2 concise KeyTakeaway sections useful as personal Knowledge prefill; not an entire section copied again.
- Usually 2–5 standalone, important, answerable Review candidates; answers 1–4 sentences or short bullets sufficient for self-rating. Prefer reasoning over method-name trivia.
- Truthful Technology/Topic/Goal metadata, consistent difficulty and an editorial time estimate.
- Check all six quality questions in the curriculum audit. Failure of three or more means rewrite before publishing, not a new compensating UI feature.

These are editorial checks, not new database constraints. Existing hard validation still requires valid title/slug/summary/enums, positive bounded minutes, and at least one topic/objective/section for a Published Lesson. Technology can be empty; Review candidates are optional in the general model. At least one canonical goal is expected for this internal curriculum, but is not a universal schema requirement. Do not publish weak content to fill a quota; leave new incomplete lessons Draft.

## Metadata rules

Technology = directly taught ecosystem/tool, not the typical deployment stack. Do not add PostgreSQL, Docker or ASP.NET tags to an EF query lesson merely because they are often used together. Generic HTTP/REST lessons can be technology-neutral; do not invent an HTTP enum for matching.

Topic = durable concept, not a Technology alias, broad “Backend” bucket or method-specific trivia. Reuse an existing concept when suitable. A new topic must have meaningful future reuse; it is not an inferred link to the user's personal Weak Topic taxonomy.

Goals use exact `LearningProfileGoal` values. Usually 1–2 goals; three is the upper bound, not a target. Add interview/project goals only when the lesson directly supports them. Audit distribution; >80% interview coverage warrants review, not an automatic ban.

Difficulty: Beginner introduces one concept with little assumed knowledge; Intermediate assumes basic language/framework familiarity and teaches lifecycle or tradeoffs; Advanced involves subtle runtime or multi-system reasoning. Labels are not measured user ability.

EstimatedMinutes covers reading, understanding the example and thinking through the takeaway. Use 10/15/20/25/30-minute buckets; split 40–60-minute concepts when possible. Estimates are editorial, exclude later Review and must never become Analytics StudyMinutes. Actual timing can refine future estimates.

## Stable identity and snapshots

Keep lesson IDs/slugs stable. Changing a title is not a reason to replace a row; progress/evidence and Knowledge/Review provenance may reference it. Source edits must not rewrite historical completion facts or user-owned cards.

CandidateKey is a readable stable concept key within a lesson, e.g. `captive-dependency`. Keep it when wording changes without changing the concept; use a new key for a new concept. Existing Review items own prompt/answer snapshots: editing a candidate is not synchronization into created cards.

Explicit Development seeding can fill missing candidates and perform guarded known-metadata upgrades. It does not automatically rewrite existing bodies/candidates or overwrite customized metadata. Any future editorial content upgrade needs its own bounded, identity-preserving change and regression check. No startup seed, destructive delete/reinsert, admin CMS or AI generation.

The bounded editorial batch compares complete source text with known published seed baselines before revising it (the original eight lessons and the preceding authored batch). It updates wording in place while retaining objective/section positions and candidate IDs/keys; custom text is skipped. New lesson definitions and the revised batch live in `LearningContentSeeder.Curriculum.cs`, separate from the retained legacy baseline. Review each definition before using the existing Draft → Published seed construction. Do not extend this guard into a generic content synchronization engine.

## Foundation closure and next authoring boundary

The current 11 Published lessons pass the editorial foundation review; no current lesson is identified as requiring Draft demotion. Future internal expansion is demand-driven, not a target count. Preserve the flat catalog at this scale. REST foundations are a useful future gap, not a blocker to this vertical. Editorial review and agent-operated workflow smoke do not prove human retention; delayed cold recall remains user validation.

Code-based authoring is still usable. Escaped Markdown and retained baseline duplication are maintenance friction to watch; extract Markdown plus a metadata manifest only if repeated authoring makes this painful. Do not build a CMS, Course, LearningPath or prerequisite graph to close this phase.

Next-phase external resources are references, not DevRecall-taught lessons. Start with manually curated title, a short original description, URL, provider/source and truthful metadata/provenance. Prefer official documentation, official vendor guides, explicitly licensed educational sources, then selected trusted community material. Public access does not grant redistribution rights: do not copy full articles or scrape content. Source-provided metadata requires appropriate permission.

Define engagement before integration. The safe initial direction is **Open external source only**, with no completion, learning evidence, Active Day or StudyMinutes from a click. Do not automatically add external links to Study Plans. Provider APIs, sync jobs, crawlers and AI summaries remain deferred. These are next-phase constraints, not functionality introduced by the foundation checkpoint.
