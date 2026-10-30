# Pre-algebra curriculum

Course id: `prealgebra` · Language id: `math` · Reviewed: 2026-10-28

Primary reference: [OpenStax Prealgebra 2e](https://openstax.org/books/prealgebra-2e/pages/preface). The Pathway explanations, scenarios, assessments, and exercises here are original.

## Intended learner and prerequisites

For learners who can count and perform basic arithmetic but lack confidence with fractions, decimal placement, signed quantities, or equations. This is an independent foundation before Pathway's `precalculus` course. Do not require programming, calculators, symbolic-algebra familiarity, or previous formal algebra lessons to get started.

## Pathway teaching contract

1. **Define terminology before using it** (numerator, denominator, coefficient, slope, median, etc.). Teach the concept in words before notation.
2. Every lesson includes a relatable physical, financial, or digital example, an explicitly explained worked problem, a common mistake, and short guided practice.
3. **Instruction comes before evaluation.** The attached lesson exercise is practice; each module's separate checkpoint is the summative assessment. Multiple-choice checkpoints include lesson-specific review references.
4. Explain what a symbol represents, with example values and units. Do not introduce abstract letters before defining variables.
5. Give numeric practice a canonical unit and a fully worked solution after the successful submission; wrong answers receive a hint. Signed values are supported by the dedicated ± control and must retain their mathematical sign; do not disguise signed changes as magnitudes.
6. Check operations against number sense (estimation), context (money, storage, geometry), and units before teaching a shortcut.
7. For a physical quantity like distance, distinguish the signed change from the nonnegative magnitude. Distinguish percentages from percentage points, rates from amounts, and area from perimeter.
8. Use age-neutral, inclusive examples. Do not assume access to expensive equipment, software, or services.

## Module progression

| Module | Topics |
| --- | --- |
| Arithmetic foundations | Place value, estimation, four operations, order of operations, factors, multiples, GCF/LCM |
| Fractions and rational numbers | Meaning of fractions, equivalence, comparison, addition/subtraction, multiplication/division |
| Decimals and percentages | Decimal conversion and arithmetic, ratios, unit rates, proportions, constants of proportionality, origin graphs, scale, discounts |
| Signed numbers and powers | Integer ordering and operations, absolute value, exponents, square roots |
| Algebraic expressions | Variables, constants, substitution, like terms, distribution and factoring |
| Equations and inequalities | Equality, inverse operations, one-step and two-step equations, boundary values |
| Graphs and relationships | Coordinate plane, tables and patterns, constant rate and slope |
| Geometry and data | Perimeter, area, volume, measurement units, averages and probability |
| Applied problem solving | Translate word problems, plan, verify, and finish a budgeting capstone |

There are 33 lessons and nine module-end checkpoints. The course uses the shared Pathway math calculator and the existing lesson/assessment deep-link system. The last module is a bridge to the `precalculus` course, not a substitute for more advanced Algebra I.

## Quality checks

`CurriculumInvariantTests` verifies the course publishes, has contiguous lesson ordering, resolvable next links, nonempty teaching content, numerically graded solutions, correct multiple-choice answer keys, and a checkpoint for every module. The checkpoint's remediation links must lead to lessons **in its own module**.

To validate in a local development checkout:

```sh
dotnet test backend/Pathway.slnx
cd frontend
npm ci
npm run build
```

Add further lessons only after checking vocabulary order and the course's progression; follow-ons such as solving equations on both sides, polynomial operations, geometry theorems, and systems of equations can be placed in a subsequent Algebra I course.
