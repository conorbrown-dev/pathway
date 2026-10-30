# Math mastery checks

Mathematics courses (Pre-algebra, Algebra I, Precalculus) use two independent experiences:

1. **Guided practice** is formative. Answering the single lesson question correctly provides feedback and the worked solution but **never unlocks the next math lesson**.
2. **Mastery assessment** is summative for that lesson. The learner completes eight server-created questions across four skills: calculate, model, interpret, and apply. At least **7/8 correct (87.5%)** are required. Each attempt uses a fresh random seed. Learners see skill-specific review recommendations after a failed attempt, and can retry with a new problem set.

### Content and generation

MathMasteryCatalog maps *every published math lesson slug* to a declared mathematical question family. There is no generic fallback. Each family provides 4 distinct exercise types twice with new parameters, combining numeric and interpretation multiple choice. Correct answers stay in server-owned attempt data and are never sent to the browser. Check the generated bank tests when adding a new math lesson; a missing family fails the test build.

### Storage and progression

The \`math_mastery_attempts\` table records the learner, lesson, exact generated questions, submission status, and score. This ensures that answers cannot be fabricated client-side and persists attempts and mastery across restarts. PostgreSQL startup schema is extended idempotently.

A passing mastery attempt marks \`learner_progress\` and schedules spaced review; failing attempts do not. Preexisting single-question math completions are excluded from the progress response until a mastery attempt is passed. Non-math lessons retain their existing progression logic. Development without a database uses a process-local store for mastery and will not persist after API restart. Guest identity uses the existing learner header, and an attempt cannot be submitted by another identity or twice.

### Learner experience

Students study a lesson, complete its guided problem, then click **Begin mastery check**. They answer all eight questions, using the ± toggle when needed, and submit the whole set for grading. Failure shows a score and missed skill types, with a fresh retry option; the Next lesson action appears only after mastery. Earlier instruction and practice remain available for remediation.

### Improvements beyond this release

- Expand families into per-lesson authored tasks and richer error-specific feedback; the initial question generator groups related mathematical topics and is not a substitute for teacher review of each lesson's objectives.
- Add teacher dashboards, retention checks and skill-level diagnostics. The existing spaced review mechanism can support retention checks, but a past passing score remains labeled as demonstrated mastery, not permanent knowledge.
- Add progression and shared persistence tests against a configured PostgreSQL instance. For now CI exercises the database-free API and cross-learner request rules.
