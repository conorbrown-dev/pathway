static partial class Curriculum
{
    // Authored lessons for the "Staff practice" module. These skills are judgement, writing and
    // influence, so the exercises are a mix of graded estimates (Numeric, checked by the API with a
    // worked solution) and written deliverables against a checklist (Presentation).

    private static Dictionary<string, double> StaffUnits(params (string Unit, double Factor)[] units)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (unit, factor) in units) map[unit] = factor;
        return map;
    }

    private static Lesson RustStaffDesign(Lesson lesson) => lesson with
    {
        Subtitle = "Turn a vague goal into a design others can trust: state the constraints, estimate the load with simple arithmetic, find the bottleneck and the failure modes, and prefer decisions you can reverse.",
        Concept = "Good system design starts from constraints, not technologies: who uses it, how much, how fast, how reliable, at what cost, and what must never happen. Back-of-the-envelope estimates turn those into numbers that rule designs in or out. Then name the bottleneck, the ways it fails, and which decisions are expensive to undo, and spend the care there.",
        Body = """
            ## What changes at staff level
            Earlier lessons asked "how do I build this correctly?". Staff-level engineering adds "*what* should we build, and how will we know it's right?", usually for problems that span several teams, with incomplete information and real costs. The output is less often code and more often a decision others can act on: a design document, a recommendation, a plan with checkpoints.

            ## Start from constraints
            Before choosing a database or a framework, write down what the system must satisfy:

            - **Users and use cases**: who does what, and which journeys matter most.
            - **Scale**: how many users, requests per second at peak, data volume and its growth.
            - **Latency and availability**: the SLOs from the observability lesson. "Submissions graded within 15 seconds for 99% of attempts" is a constraint; "fast" is not.
            - **Consistency and durability**: what may be briefly stale; what must never be lost.
            - **Security and compliance**: what data is sensitive, who may see it, which rules apply.
            - **Cost and team**: budget, the skills the team has, how long it must be maintained.
            - **Non-goals**: what this design deliberately won't do. They prevent scope creep and settle arguments early.

            Constraints usually conflict. Naming them is what makes the trade-offs discussable.

            ## Back-of-the-envelope estimates
            You rarely need precision to make a design decision; you need the right order of magnitude. Simple multiplication with stated assumptions answers "will this fit?" in minutes:

            - **Requests**: daily active users × actions per user per day ÷ 86,400 seconds per day gives the average rate. Peaks are often 2 to 10 times the average.
            - **Storage**: records per day × bytes per record × retention days.
            - **Compute**: operations × time per operation.
            - **Concurrency**: Little's law from the capacity lesson.

            Useful numbers to keep in mind: a day has 86,400 seconds (about 100,000) and a month about 2.6 million; a main-memory reference is around 100 nanoseconds; a same-datacenter network round trip is around half a millisecond; an SSD random read around 0.1 milliseconds; a cross-continent round trip 100 milliseconds or more.

            Write the assumptions next to the arithmetic. When someone disagrees with a number, the disagreement is about an assumption you can check, not a vague feeling. And state which assumption would change the design if it turned out ten times larger.

            ## Bottlenecks and failure modes
            For each part of the design ask: what's the first thing to run out (CPU, connections, disk I/O, a third-party rate limit, a single-threaded component)? And what happens when each dependency is slow, down, or returns garbage? The resilience lesson's tools (timeouts, retries with backoff, circuit breakers, queues, graceful degradation) are the answers; the design document says which applies where.

            ## Reversible and irreversible decisions
            Some decisions are cheap to change later: a library, an internal API, a cache. Others are expensive: a public API contract, a data model with years of records, a storage format other systems read, a security boundary. Amazon describes these as **two-way** and **one-way doors**. Move quickly through two-way doors; slow down, prototype, and get review for one-way doors. Much of senior judgement is telling the two apart, and designing to keep more doors two-way: version formats, hide implementations behind interfaces, roll out behind flags.

            ## The design document
            A short, readable structure works for most designs:

            1. **Context and problem**: why this, why now.
            2. **Goals and non-goals.**
            3. **Constraints and estimates**, with assumptions.
            4. **Proposed design**: components, data flow, a diagram.
            5. **Alternatives considered**, and why not.
            6. **Failure modes, security and operations**: what breaks and how you'll know.
            7. **Rollout and reversal**: phases, the signal to stop, how to roll back.
            8. **Open questions.**

            Circulate it early, as a draft, to the people who'll operate it and the teams it touches. A design that changes in review is the process working.

            ## Common mistakes
            - Choosing technology before stating constraints.
            - Estimates without assumptions, or no estimates at all.
            - Designing only the happy path.
            - Treating every decision with the same weight, either agonising over reversible ones or rushing irreversible ones.
            - Finishing the document before anyone has seen it.

            ## Try it
            Estimate the compute this platform's code sandboxes need, using the numbers in the exercise. Then ask which assumption you'd want to measure first, and what you'd change if it were ten times worse.
            """,
        Example = """
            200,000 active learners
            × 30 graded submissions per learner per month
            × 2 vCPU-seconds per submission (compile + run)
            = 12,000,000 vCPU-seconds per month
            ÷ 3,600 seconds per hour
            ≈ 3,333 vCPU-hours per month
            """,
        Exercise = new Exercise(
            ExerciseKind.Numeric,
            "Estimate sandbox compute",
            "Pathway plans for 200,000 active learners a month. Each makes about 30 graded code submissions a month, and each submission uses about 2 vCPU-seconds to compile and run. How many vCPU-hours of sandbox compute is that per month?",
            ["Multiply learners by submissions by vCPU-seconds", "Convert seconds to hours", "Enter the number and choose a unit"],
            null,
            null,
            [],
            "Total vCPU-seconds is learners × submissions × seconds per submission. An hour has 3,600 seconds.",
            ["States the assumptions behind each number", "Converts units correctly"],
            3333.33,
            15,
            "vCPU-hours",
            StaffUnits(("vCPU-seconds", 1d / 3600d), ("vCPU-minutes", 1d / 60d)),
            "First find the total compute in vCPU-seconds: 200,000 learners each make 30 submissions, which is 6,000,000 submissions a month; each takes 2 vCPU-seconds, so the total is 12,000,000 vCPU-seconds. An hour is 3,600 seconds, so divide by 3,600 to get about 3,333 vCPU-hours a month. That is the average; peak hours (assignments due, evenings) can be several times higher, which is what sizes the concurrency limits."),
    };

    private static Lesson RustStaffArchitecture(Lesson lesson) => lesson with
    {
        Subtitle = "Record significant decisions so they survive the people who made them: context, options, the choice, its consequences, and when to revisit it, in a short architecture decision record.",
        Concept = "An architecture decision record (ADR) is a short document capturing one significant decision: the context and forces at the time, the options considered, the choice, and its consequences, good and bad. ADRs live next to the code, are numbered and never rewritten, only superseded, so future engineers can tell why things are the way they are and when the reasons no longer hold.",
        Body = """
            ## Why decisions get lost
            Six months after a team picks SQLite over PostgreSQL, or a queue over direct calls, someone new asks why. The people who decided have moved on, the chat thread is buried, and the reasons existed only in their heads. The new person either reverses a good decision because they can't see the reason, or is afraid to change a decision whose reasons stopped applying long ago. Both are expensive.

            An **architecture decision record** fixes that cheaply: a page of text, written when the decision is made.

            ## What makes a decision "architectural"
            Write an ADR for decisions that are expensive to reverse or that constrain many future choices: data stores and data models, communication between services (synchronous HTTP, queues, events), security and identity boundaries, public API conventions, build and deployment platforms, the choice to build rather than buy. Don't write one for which logging library to use inside a module.

            ## The format
            Michael Nygard's original format, still the most common:

            ```text
            # 7. Store learner progress in PostgreSQL (Neon)

            Status: Accepted (2026-10-12). Supersedes ADR 3.

            ## Context
            Progress writes are about 50/s at peak and growing; the API runs as several
            stateless instances; SQLite on one disk can't be shared between them...

            ## Options considered
            1. Keep SQLite on a single instance. Simple; caps us at one API instance.
            2. Managed PostgreSQL. Shared by all instances; another service to operate.
            3. A key-value store. Scales; loses relational queries the dashboard needs.

            ## Decision
            Option 2, using Neon's managed PostgreSQL...

            ## Consequences
            + API instances scale horizontally; point-in-time recovery comes with the service.
            − A network hop on every write (~1–2 ms); a new failure mode when Neon is unavailable.
            − Migrations must stay compatible with two API versions during deploys.

            ## Revisit when
            Write volume exceeds 2,000/s, or regional latency requirements appear.
            ```

            Points that make ADRs useful rather than ceremonial:

            - **Context is the most valuable section.** It records the forces (scale, deadlines, team skills, costs) that made the decision right *at the time*. When the context changes, the decision may need to change, and the ADR says so plainly.
            - **Options, honestly.** Include the ones you rejected and the real reasons, including "we know this one better".
            - **Consequences include the bad ones.** Every choice has costs. Writing them down shows they were accepted deliberately, and tells future readers what to watch.
            - **Revisit triggers** turn "we might reconsider someday" into a measurable condition.
            - **Immutable once accepted.** A changed decision gets a new ADR that supersedes the old one, and the old one's status changes to "Superseded by ADR 12". The history of *why* is the point.

            Keep ADRs in the repository (`docs/adr/0007-progress-in-postgres.md`), reviewed through pull requests like code, so they're versioned and found by the people who change that code.

            ## Getting decisions made
            An ADR records a decision; someone still has to make it. At staff level that often means leading it: framing the question so people answer the same one, gathering input from the teams affected, making the trade-offs visible, and driving to a decision with a named owner by a date. "Disagree and commit" works only when people felt heard before the decision, and the ADR's Options section is where they can see that they were.

            ## Common mistakes
            - Writing ADRs after the fact, reconstructing reasons nobody remembers.
            - Listing only the chosen option, or only its benefits.
            - Editing an accepted ADR when the decision changes.
            - ADRs for trivial choices, so the important ones are lost in noise.
            - Keeping them in a wiki nobody connects to the code.
            """,
        Example = """
            # 7. Store learner progress in PostgreSQL

            Status: Accepted (2026-10-12). Supersedes ADR 3.

            ## Context / ## Options considered / ## Decision
            ## Consequences (good and bad) / ## Revisit when
            """,
        Exercise = new Exercise(
            ExerciseKind.Presentation,
            "Write an ADR for a real decision",
            "Choose a significant decision from a project of your own, or from this course's systems path (for example: the storage capstone's sync-every-write durability choice, or JSON versus a binary format for the key-value server's protocol). Write it as an ADR, then share it with someone and ask what they'd challenge.",
            [
                "A numbered title stating the decision, and a status with a date",
                "Context describing the forces at the time: scale, constraints, deadlines, team skills",
                "At least two options with honest pros and cons, including the rejected ones",
                "The decision, and its consequences, including at least two negative ones",
                "A concrete condition under which the decision should be revisited",
            ],
            null,
            null,
            [],
            "Write the context first; if you can't explain the forces, you're not ready to record the decision. Keep it to about a page.",
            []),
    };

    private static Lesson RustIncidentLearning(Lesson lesson) => lesson with
    {
        Subtitle = "Lead the response when production breaks: mitigate first, communicate on a rhythm, then learn without blame, using error budgets to decide how much reliability work is enough.",
        Concept = "During an incident the goal is to stop user impact as fast as possible, usually by rolling back or disabling something, before understanding the cause. Clear roles and regular updates keep the response coordinated. Afterwards a blameless review asks how the system and process allowed the failure, and error budgets from SLOs turn reliability into a shared, measurable trade-off with shipping features.",
        Body = """
            ## Mitigate first
            When users are affected, the priority order is: **stop the harm, then understand it, then fix it properly.** The fastest mitigations are usually blunt and reversible: roll back the latest deploy, switch off a feature flag, scale up, shed load, fail over to another region, block an abusive client. Debugging the root cause while users wait is the classic mistake. Many incidents are resolved by a rollback within minutes, and only investigated afterwards.

            This is why the delivery lesson insisted on fast rollbacks and gradual rollouts: they're incident tools you build before you need them.

            ## Roles
            For anything beyond a small blip, separate the jobs, even on a small team:

            - **Incident commander**: coordinates. Decides priorities, assigns work, makes the call on risky actions. Doesn't debug.
            - **Operations / subject experts**: investigate and apply mitigations.
            - **Communications lead**: updates stakeholders and users.
            - A **scribe** keeps a timeline of what was observed and done, with timestamps, in the incident channel.

            One person trying to fix, coordinate and communicate at once does all three badly.

            ## Communicate on a rhythm
            Updates go out at a fixed interval (every 30 minutes, say) even when there's nothing new; silence is read as chaos. Each says what's affected, what's being done, and when the next update will come. Separate the internal channel (where responders work) from the external status page (written for users, without speculation about causes).

            ## Blameless reviews
            After the incident, write a **postmortem** (or incident review): a timeline, the impact, how it was detected and mitigated, contributing factors, and follow-up actions with owners.

            **Blameless** doesn't mean nobody is accountable. It means assuming people acted reasonably given what they knew, and asking *how the system made the mistake easy and its impact large*. "Ada ran the wrong migration" leads nowhere useful. "The migration tool ran against production without confirmation, nothing staged it first, and monitoring took 40 minutes to notice" leads to three fixes that protect everyone. People who fear blame hide information, and then the organisation stops learning.

            Good follow-ups are specific, owned and tracked: "add a confirmation prompt for production migrations (owner, date)", not "be more careful". Look at detection and response as well as the trigger: how could we have noticed in 2 minutes instead of 40?

            ## Error budgets
            The observability lesson introduced SLOs. Their most powerful use is the **error budget**: the amount of unreliability the SLO allows. A 99.9% availability objective over 30 days allows 0.1% of that time to be bad:

            30 days × 24 hours × 60 minutes = 43,200 minutes; 0.1% of that is **43.2 minutes**.

            The budget turns a recurring argument (ship faster or stabilise?) into a shared rule. While budget remains, the team ships at full speed and accepts some risk. When an incident consumes most of it, the team shifts to reliability work until it recovers. Neither side has to win an argument; the number decides, and both product and engineering agreed to it in advance. Note how small 43 minutes is: one slow rollback can spend it, which is why fast mitigation matters so much.

            ## Common mistakes
            - Investigating before mitigating.
            - No incident commander, so everyone debugs and nobody coordinates.
            - Long silences in communication.
            - Postmortems that name a person as the root cause.
            - Follow-up actions with no owner, which never happen.
            - SLOs nobody looks at, or 100% targets that leave no budget for change.

            ## Try it
            Work out the downtime budget in the exercise, then compare it with how long your last rollback took.
            """,
        Example = """
            Availability SLO: 99.9% over 30 days
            Period: 30 × 24 × 60 = 43,200 minutes
            Budget: 0.1% × 43,200 = 43.2 minutes of full outage
            A 25-minute outage spends 25 / 43.2 ≈ 58% of the month's budget.
            """,
        Exercise = new Exercise(
            ExerciseKind.Numeric,
            "Compute an error budget",
            "Pathway's grading service has an availability SLO of 99.9% measured over a 30-day window. How many minutes of complete outage does that error budget allow in the window?",
            ["Find the number of minutes in 30 days", "Take the share the SLO allows to fail", "Enter the number and choose a unit"],
            null,
            null,
            [],
            "The budget is the part that may fail: 100% − 99.9% = 0.1% of all the minutes in 30 days.",
            ["Converts the window to minutes", "Applies the allowed failure fraction"],
            43.2,
            0.1,
            "minutes",
            StaffUnits(("seconds", 1d / 60d), ("hours", 60d)),
            "Thirty days is 30 × 24 = 720 hours, and 720 × 60 = 43,200 minutes. A 99.9% objective allows the remaining 0.1% to fail, and 0.1% is one thousandth, so 43,200 ÷ 1,000 = 43.2 minutes of complete outage per 30 days. Partial outages spend the budget more slowly: an hour at a 10% error rate uses 6 minutes of it."),
    };

    private static Lesson RustMentoringLeverage(Lesson lesson) => lesson with
    {
        Subtitle = "Multiply your impact through other engineers: paved roads that make the right way the easy way, reviews that teach, mentoring that builds independence, and writing that scales.",
        Concept = "A senior engineer's own output is capped by their hours; leverage comes from making many engineers more effective. Encode good practice into defaults and templates (paved roads), use code review and mentoring to teach rather than gatekeep, and write down knowledge once so it reaches people you'll never meet.",
        Body = """
            ## From output to leverage
            Early in a career, impact is mostly your own code. At staff level the question becomes: *what can I do that makes ten or a hundred engineers better or faster?* Rewriting a teammate's pull request yourself is output; showing them the pattern so their next ten pull requests don't need it is leverage. Heroics, being the only person who can fix the payment system at 2 a.m., feel valuable and are a risk to the organisation.

            ## Paved roads
            A **paved road** (or golden path) is the well-supported default way to do a common thing, made easier than the alternatives so people choose it without being told:

            - A service template with the patterns from this course already wired in: configuration validated at startup, `/livez` and `/readyz`, graceful shutdown, metrics middleware with bounded labels, structured logs, a multi-stage Dockerfile, CI running fmt, clippy, tests and audit.
            - A shared library for the error format (problem details), authentication extractor, and retry and backoff policy.
            - Documentation and a "create a new service" command.

            Teams can leave the paved road when they have a reason; they just have to own the extra work. Paving works better than policy: a rule that says "all services must expose metrics" is followed unevenly; a template where metrics already work is followed by default.

            ## Reviews that teach
            Code review is the most frequent mentoring channel most engineers have. Make it count:

            - Distinguish **blocking** issues (correctness, security, data loss, a contract change) from **suggestions** and **nits**. Label them, so authors know what must change.
            - Explain the *why* and link to the principle ("this holds the lock across an await; see the async lesson"), so the lesson transfers to the next change.
            - Ask questions when you're unsure rather than asserting.
            - Review promptly. A pull request waiting two days teaches people to batch huge changes.
            - Approve good-enough work. Perfection-seeking reviews slow everyone and discourage small, frequent changes.
            - Automate what can be automated (formatting, lints), so humans spend attention on design.

            ## Mentoring for independence
            The goal of mentoring is that the person needs you less over time. Instead of giving the answer, ask what they've tried and what they think; point to where the answer can be found; let them make recoverable mistakes. Give specific feedback soon after the work, privately for criticism. Sponsor as well as mentor: put people forward for visible work, and give them credit in the rooms they're not in.

            ## Writing that scales
            Explaining something once in a meeting reaches the people in the room. Writing it down reaches everyone, including future hires, at any time zone. Runbooks, design documents, ADRs, "how we do X here" guides, and short postmortem write-ups compound over years. Keep them close to the code, short, and dated, and delete what's obsolete.

            ## Common mistakes
            - Being the bottleneck every decision or review must pass through.
            - Rules without paved roads, so compliance is optional in practice.
            - Reviews that only say "fix this" without why, or that block on preferences.
            - Solving people's problems for them instead of building their ability.
            - Knowledge that lives only in your head or in chat.
            """,
        Example = """
            Review comment labels:
              blocking: holds the MutexGuard across `.await` (deadlock risk under load)
              suggestion: a `?` here would replace the match and read more simply
              nit: name `x` → `attempts`
            """,
        Exercise = new Exercise(
            ExerciseKind.Presentation,
            "Pave a road for Rust services",
            "Design the paved road a team would use to start a new Rust service, using what this course covered. Write it as a one-page proposal a team lead could approve, and if you can, build the template repository.",
            [
                "The problem: what goes wrong or takes too long today without a shared starting point",
                "What the template includes, mapped to lessons: configuration, health, shutdown, metrics, logging, errors, auth, CI checks, container build",
                "How teams adopt it, and how they may deviate and what they then own",
                "How you'll know it worked: two or three measures, such as time to first deploy or incidents from missing timeouts",
                "Who maintains it, and how changes reach services already created from it",
            ],
            null,
            null,
            [],
            "Start from the problem, not the template. A paved road that solves no felt pain won't be used.",
            []),
    };

    private static Lesson RustStaffCapstone(Lesson lesson) => lesson with
    {
        Subtitle = "Bring the whole path together: design, justify and plan the rollout of a real platform change, and present it the way a staff engineer would, with the trade-offs and risks in plain view.",
        Concept = "A staff-level capstone isn't more code; it's a defensible plan. Take a real platform need, state the constraints with estimates, propose a design built from components you understand from the inside, record the key decisions, plan a reversible rollout with clear owners and stop signals, and present it for challenge.",
        Body = """
            ## The brief
            Design the next version of a code-learning platform's **grading service**, the system that runs learners' submissions in sandboxes (much like the one grading your exercises in this course). It must:

            - grade submissions in Rust and other languages with no network access, a CPU and memory limit, and a wall-clock timeout;
            - give learners a result within 15 seconds for 99% of submissions, at ten times today's volume;
            - keep working (perhaps degraded) when the sandbox provider has an outage;
            - never let one learner's code see another's, or reach the internet;
            - stay within a monthly compute budget you set from your estimate in the system-design lesson.

            You may substitute a platform need from your own work of similar scope, as long as it involves real constraints and more than one team.

            ## What to produce
            **1. A design document** (the structure from the system-design lesson), including:

            - constraints and back-of-the-envelope estimates, with assumptions;
            - the architecture: request flow from submission to result, the queue between the API and the graders (the resilience lesson), concurrency limits sized with Little's law, storage of results (the transactions lesson), and the isolation boundary (the unsafe and processes lessons explain what a sandbox must contain);
            - failure modes: the sandbox provider slow or down, a submission that never terminates, a flood of submissions before a deadline, a poison submission that crashes graders;
            - observability: the SLOs, the metrics (with bounded labels), and the alerts, as burn-rate alerts on the error budget;
            - security: what an attacker submitting code would try, and what stops each attempt.

            **2. Two or three ADRs** for the decisions that are hardest to reverse.

            **3. A rollout plan**: phases (shadow grading alongside the current system, comparing results; a small percentage of traffic; full), the metric that would stop each phase, and how to roll back.

            **4. A ten-minute presentation** to people who'll challenge it, with the trade-offs and the risks you're least sure about stated up front, and a list of the questions you couldn't answer.

            ## How it's judged
            A good capstone is one that a reviewer would trust:

            - Every major choice traces back to a constraint or an estimate.
            - Rejected alternatives are described fairly.
            - Failure modes have specific responses, not "add monitoring".
            - The rollout can be stopped and reversed at each step.
            - The author can explain any component from the inside: what the queue's delivery guarantee is, why the timeout is where it is, what happens on SIGTERM.

            The last point is why this course built so much from first principles. Designing with components you understand, rather than names on boxes, is what separates a staff engineer's design from a diagram.

            ## Where to go next
            The systems path's capstone (the storage engine) and this one together make a portfolio: one shows you can build a correct, measured system component; the other shows you can lead the design of a system around it. Keep both in a public repository with their documents, and be ready to walk someone through them.
            """,
        Example = """
            Submission flow (sketch):
              API ── enqueue (idempotency key) ──► queue ──► N graders (semaphore = Little's law)
                                                             │ sandbox: no network, CPU/mem caps, 12 s timeout
              API ◄── result (transaction) ── results store ◄┘
            Stop signal for each rollout phase: grading p99 > 15 s or result mismatch rate > 0.1%
            """,
        Exercise = new Exercise(
            ExerciseKind.Presentation,
            "Design and defend a grading platform",
            "Produce the design document, ADRs and rollout plan described in the lesson, then present them to at least one experienced engineer and record the hardest questions they asked.",
            [
                "Constraints with back-of-the-envelope estimates and stated assumptions",
                "An architecture covering the queue, concurrency limits, result storage and the isolation boundary, with failure modes and specific responses",
                "SLOs, metrics and burn-rate alerts, and a security analysis of what submitted code might attempt",
                "Two or three ADRs for the hardest-to-reverse decisions",
                "A phased, reversible rollout with stop signals, presented for challenge, with open questions listed",
            ],
            null,
            null,
            [],
            "Write the constraints and estimates first; let them drive every later section. Ask a reviewer to attack the failure-mode section specifically.",
            []),
    };
}
