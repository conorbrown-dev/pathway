# Precalculus curriculum

Course id `precalculus` · languageId `math` · lessons in `backend/Pathway.Api/PrecalculusCurriculum.cs`
Reference text: [OpenStax Precalculus 2e](https://openstax.org/details/books/precalculus-2e) · Last reviewed 2026-10-25

## Who it's for

Learners heading into computer science, engineering or electrical work who need precalculus, including people who find formulas hard to follow. The course assumes basic arithmetic and nothing else.

## Authoring rules (apply to every future math course too)

1. **Every formula is tied to a problem in the physical or digital world.** No bare `x`. Each lesson names everyday situations (money, travel, temperature, depth, machines) and at least one game situation (map coordinates, resources per second, artillery arcs, upgrade costs, orbiting units).
2. **Every letter is named as a variable or a constant, with example values.** Each body has a "What the letters stand for" section (or "What the symbols stand for" before letters appear).
3. **Words first, symbols second.** Steps say what is happening and why before showing the shorthand. The `Example` block shows the symbols with short labels.
4. **Walk through one problem** with numbered steps, each explaining the reason for the step, and finish with a check where it fits.
5. **Graded answers have a real unit and a plain-words worked solution.** Answers are non-negative so mobile decimal keypads (which often lack a minus key) can enter them.
6. **Use analogies only where the math holds.** Example: temperature is good for adding and subtracting negatives, but not for multiplying ("four times as cold" has no meaning on the Fahrenheit scale because 0°F isn't "no heat"). Depth below sea level works for multiplying.

`CurriculumInvariantTests.Precalculus_lessons_tie_math_to_situations_and_explain_every_step` enforces rules 1, 2 and 5 structurally.

## Modules

| Module | Lessons |
| --- | --- |
| Number foundations | Negative numbers · order of operations · rates · exponents · variables and constants |
| Equation foundations | Linear equations · inequalities |
| Function foundations | Coordinate plane and distance · functions · domain and range · slope · linear models · systems · transformations · piecewise functions |
| Quadratics and polynomials | Quadratics · solving quadratics · polynomials · rational functions |
| Exponential growth and logarithms | Exponential growth · decay and half-life · logarithms · solving for time |
| Angles and trigonometry | Degrees and radians · right-triangle trig · unit circle and circular motion |
| Sequences and capstone | Arithmetic and geometric sequences · capstone (choose the model, then solve) |

## Planned follow-on courses

Trigonometry, calculus, differential equations and linear algebra, written to the same rules.
