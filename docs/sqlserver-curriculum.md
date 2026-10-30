# SQL Server: T-SQL and the database engine

Course id `sql-server` · languageId `sqlserver` · lessons in `backend/Pathway.Content/Courses/SqlServer*.cs` · graders in `sandbox-broker/src/sql-graders/` (one file per lesson slug) · engine in `sandbox-broker/src/sql-graders/_tsql.py`
Status: engine and first grader merged on branch `sql-server-course-engine`; lessons not yet authored.
Last reviewed 2026-10-30

## Who it's for

Developers, analysts and students who need to query, design and look after a SQL Server database. The course assumes a computer and a terminal; relational ideas, SQL and the engine's internals are all taught inside the course before they are used. By the end a learner can write and tune T-SQL, design tables that protect their own data, explain transactions, locking, indexes and execution plans, and present how a SQL Server database works to a team or class.

## Authoring rules

Same rules as the LLM course (`docs/llm-curriculum.md`):

1. Nothing before its prerequisites. Each lesson declares the earlier lessons it builds on; the lesson page shows them as "What you need first" and `CurriculumInvariantTests` fails on a forward reference.
2. Words first, symbols second. Every step, clause and formula is explained in a sentence before it is shown, and a "What the words mean" section names each term.
3. Tie every idea to something real: a shop, a bank, a booking system.
4. Say what is not known or version-dependent instead of picking a side.
5. Every lesson ends with "Explain it to your team", the outline of the final presentation.
6. Tooling notes carry a version stamp and a source link.

## How T-SQL is graded

SQL Server cannot run in the sandbox (no network, 1 vCPU, 12 s, no Windows or container runtime). So:

- **Graded in the sandbox.** Learner T-SQL is parsed as T-SQL by `_tsql.py` and executed on SQLite inside the `tools` image. The graders compare result sets, schema and constraint behaviour, never the learner's text. Reference solutions live in `sandbox-broker/test/sql-solutions/` (`<slug>.pass.sql` and `<slug>.fail.sql`, the latter with a `-- expect:` line) and run under `npm test` and again in the built image.
- **Local labs.** Topics that depend on the real engine (execution plans, locking and isolation, backups, SQL Server Agent, stored procedures and other procedural T-SQL, security) are Presentation exercises with an evidence checklist, run on SQL Server Developer in Docker on the learner's machine.

### Supported T-SQL subset

SELECT with TOP, OFFSET/FETCH, joins, GROUP BY/HAVING, subqueries, CTEs (including recursive), window functions, set operators; INSERT/UPDATE/DELETE; CREATE/ALTER/DROP for tables, views and indexes with PRIMARY KEY, UNIQUE, FOREIGN KEY, CHECK, DEFAULT and IDENTITY; variables, PRINT, GO batches; transactions; and the common string, date and conversion functions. Anything outside the subset (PRAGMA, ATTACH, procedures, triggers, cursors, dynamic SQL) is refused with a plain-words message that points to the local lab.

### Known fidelity gaps (taught honestly in the lessons)

DECIMAL is stored as a floating-point value, divide-by-zero returns NULL instead of an error, integer AVG is not truncated like SQL Server's, IDENTITY_INSERT and MAXRECURSION are approximated, and collation is always case-insensitive. Lessons that touch these say so and tell the learner what the real engine does.

## Planned modules

| Module | Lessons |
| --- | --- |
| Foundations | What a relational database is · tables, rows, keys · installing SQL Server Developer and SSMS (lab) |
| Querying | SELECT and WHERE · NULL logic · sorting and paging · expressions and functions |
| Combining data | Joins · set operators · subqueries · CTEs |
| Summarising | Aggregates · GROUP BY and HAVING · window functions |
| Changing data | INSERT, UPDATE, DELETE · transactions · constraints |
| Designing | Normalisation · data types · keys and relationships · views |
| Performance | Indexes · execution plans (lab) · statistics · query tuning (lab) |
| Concurrency | Locking and isolation (lab) · deadlocks (lab) |
| Operating | Backups and restore (lab) · security (lab) · SQL Server Agent (lab) |
| Capstone | Design and ship a small database, then present it |

About 40 lessons in total.

## Sources

- Microsoft Learn, SQL Server 2025 documentation and [build versions](https://learn.microsoft.com/en-us/troubleshoot/sql/releases/sqlserver-2025/build-versions)
- Microsoft Learn, Transact-SQL reference
- SQLite documentation (the grading backend, not taught)

The exact SQL Server build and SSMS version for the version stamp are still to be confirmed against Microsoft's pages before the first lesson merges.
