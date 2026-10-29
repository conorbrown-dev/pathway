# Trigonometry curriculum

Course id `trigonometry` · languageId `math` · lessons planned for `backend/Pathway.Content/Courses/TrigonometryCurriculum.cs`
Reference texts: [OpenStax Precalculus 2e](https://openstax.org/details/books/precalculus-2e) chapters 5–8 and [OpenStax Algebra and Trigonometry 2e](https://openstax.org/details/books/algebra-and-trigonometry-2e) chapters 7–10 · Last reviewed 2026-10-29

Status: **outline for review**. No lessons are authored yet.

## Who it's for

Learners who finished Precalculus and are heading into calculus, physics, electrical engineering or game programming. Trigonometry is the math of anything that turns, repeats or points in a direction: wheels, waves, AC power, aiming, orbits and rotating sprites.

The course assumes the Precalculus lessons `math-angles-radians`, `math-right-triangle-trig` and `math-unit-circle`. It builds on them and doesn't repeat them. Each early lesson opens with a short "you already know" recap that links back to the Precalculus lesson.

## Authoring rules

Every rule in [precalculus-curriculum.md](precalculus-curriculum.md#authoring-rules-apply-to-every-future-math-course-too) applies unchanged: tie every formula to a situation, name every letter as a variable or constant, put words before symbols, walk through one problem, give graded answers a real unit and a plain-words worked solution, and use analogies only where the math holds. Trigonometry adds three rules:

7. **Say which angle mode you're in.** Every prompt that involves an angle names the unit (degrees or radians). Every worked solution that uses a calculator says "use degree mode" or "use radian mode." Degree answers accept radians through unit conversion, and radian answers accept degrees.
8. **Directions are words, sizes are numbers.** Trig produces negative values constantly (cos 120°, motion toward the west, a point below the axis). Rule 5 still holds: graded answers stay non-negative. Ask for the size and state the direction in words ("how many m/s is it moving **west**?"), or ask for a heading measured counterclockwise from east in **[0°, 360°)**. The worked solution explains where the minus sign went. This is also good teaching. Learners have to say what the sign means, not just copy it.
9. **Every identity is checked with numbers before it's used.** Before an identity is used, the lesson plugs in a real angle (for example 30°) and shows both sides come out equal. Identity lessons that would otherwise ask for proofs grade a numeric result the identity makes easy, or use a multiple-choice "which rewrite is correct" question.

`CurriculumInvariantTests.Precalculus_lessons_tie_math_to_situations_and_explain_every_step` should be generalized to run for every course with languageId `math`, so this course is checked by the same structural rules.

## Shape

33 lessons in 9 modules, ordered 10–330. Slugs use the prefix `trig-` so they can't collide with the Precalculus `math-` slugs. The last lesson of Precalculus doesn't link into this course. Cross-course links stay in the catalog, not in `NextSlug`.

| Module | Lessons |
| --- | --- |
| Angles beyond one turn | Coterminal and reference angles · special-angle exact values · signs by quadrant · arc length and angular speed |
| All six functions | Tangent as slope · secant, cosecant, cotangent · the Pythagorean identity |
| Waves and graphs | The sine wave · amplitude and midline · period and frequency · phase shift · building a sinusoidal model · the tangent graph |
| Finding the angle | Inverse sine and cosine · inverse tangent and atan2 · solving right triangles |
| Identities | What an identity is · sum and difference · double angle · power reduction and RMS |
| Solving trig equations | One cycle, two answers · every solution and how long it lasts · solving with identities |
| Triangles without a right angle | Law of sines · the ambiguous case · law of cosines · triangle area |
| Vectors, rotation and polar form | Vector components · the dot product · rotating a point · polar coordinates · phasors |
| Capstone | Track an orbiting drone |

## Lesson outline

Each lesson lists its everyday and game situations, then the graded exercise with its answer. The answers were checked numerically.

### Module 1 · Angles beyond one turn

**10 `trig-coterminal-reference` · Coterminal and reference angles**
- Idea: adding or removing full turns doesn't change where you're pointing. The reference angle is the acute angle to the nearest horizontal axis, and it decides the size of sine and cosine.
- Everyday: a steering wheel turned one and a half turns; a socket wrench ratcheted around a bolt.
- Game: a turret whose rotation counter keeps climbing past 360°.
- Graded: a turret has turned 1,000° counterclockwise from east. Which heading is it pointing at, in [0°, 360°)? **280°** (rad accepted).

**20 `trig-special-angles` · Exact values for 30°, 45° and 60°**
- Idea: two triangles (half a square, half an equilateral triangle) give exact values you'll see everywhere.
- Everyday: a 45° roof brace, a ladder at 60°, a 30° wheelchair-unfriendly ramp (and why real ramps are much shallower).
- Game: hex grids are built from 60° angles.
- Graded: a 10 m ladder leans at 60° to the ground. How high up the wall does it reach? **8.66 m** (ft accepted).

**30 `trig-quadrant-signs` · Signs by quadrant**
- Idea: sine and cosine are positions, so their signs tell you which way you're facing. This is where rule 8 is introduced.
- Everyday: splitting a wind direction into north-south and east-west parts.
- Game: a unit moving at heading 210° is moving left and down on the map.
- Graded: a unit moves at 5 m/s with heading 210°. How many m/s is it moving west? **4.33 m/s**. The worked solution shows cos 210° ≈ −0.866 and explains that the minus sign means "west."

**40 `trig-arc-length-angular-speed` · Arc length and angular speed**
- Idea: in radians, distance along the rim is radius × angle (s = rθ), so speed at the rim is radius × spin rate (v = rω). This is the payoff for learning radians.
- Everyday: a bike wheel, a car odometer counting tire turns.
- Game: a tank's tread speed from its wheel spin; the tip of a spinning blade.
- Graded: a bike wheel of radius 0.35 m spins at 8 rad/s. How fast is the bike moving? **2.8 m/s** (km/h accepted).

### Module 2 · All six functions

**50 `trig-tangent-slope` · Tangent is slope**
- Idea: tan θ = rise ÷ run, the same slope as in Precalculus, now attached to an angle.
- Everyday: road grade signs ("6% grade"), the 1:12 ramp rule.
- Game: line-of-sight slope from a hill.
- Graded: a road climbs at 4°. What is its grade in percent? **6.99%**.

**60 `trig-reciprocal-functions` · Secant, cosecant and cotangent**
- Idea: the three flipped ratios, named for when the length you know is on the bottom.
- Everyday: cable length from a known horizontal span.
- Game: ray length to a wall at a known distance (raycasting renderers use exactly this).
- Graded: a zip line drops at 12° and spans 150 m horizontally. How long is the cable? **153.35 m**.

**70 `trig-pythagorean-identity` · sin² θ + cos² θ = 1**
- Idea: the unit circle has radius 1, so the two positions always satisfy the Pythagorean theorem.
- Everyday: finding a ramp's run when you only know its length and steepness ratio.
- Game: a normalized direction vector always has length 1, which is why diagonal movement isn't faster.
- Graded: a 25 m ramp has sin θ = 0.28. How long is its horizontal run? **24 m**.

### Module 3 · Waves and graphs

**80 `trig-sine-graph` · The sine wave**
- Idea: unroll the unit circle's height over time and you get a wave. One full turn is one full cycle.
- Everyday: a Ferris wheel seat's height, a pendulum.
- Game: a floating pickup that bobs up and down.
- Graded: a point on a wheel starts level with the hub on the right side and turns counterclockwise. How many degrees of rotation until it's at its lowest? **270°**.

**90 `trig-amplitude-midline` · Amplitude and midline**
- Idea: y = A sin(…) + D. The midline D is the average level, and the amplitude A is how far the wave swings above and below it.
- Everyday: tides between high and low water; daily temperature swings.
- Game: a breathing idle animation, a pulsing glow.
- Graded: high tide is 11.2 ft and low tide is 2.6 ft. What is the amplitude? **4.3 ft**.

**100 `trig-period-frequency` · Period and frequency**
- Idea: period is time per cycle, and frequency is cycles per second (Hz). Each is 1 divided by the other.
- Everyday: US wall power at 60 Hz, the A above middle C at 440 Hz.
- Game: a blinking warning light, animation loops.
- Graded: US mains power is 60 Hz. How many milliseconds is one cycle? **16.67 ms** (s accepted).

**110 `trig-phase-shift` · Phase shift**
- Idea: sliding a wave left or right in time, measured in degrees of a cycle or in seconds.
- Everyday: three-phase power, with three waves 120° apart; daylight that peaks in late June.
- Game: enemies on the same patrol path started at staggered times.
- Graded: in 60 Hz three-phase power, phase B lags phase A by 120°. If A peaks at 0 ms, when does B peak? **5.56 ms**.

**120 `trig-sinusoidal-models` · Building a sinusoidal model**
- Idea: read the midline, amplitude, period and starting point from a situation and assemble the full formula.
- Everyday: a Ferris wheel 40 m across, hub 22 m up, one turn every 8 minutes, boarding at the bottom.
- Game: a moving platform on a timer.
- Graded: on that wheel, how high is a rider 1 minute after boarding? **7.86 m**.

**130 `trig-tangent-graph` · The tangent graph and its walls**
- Idea: tangent shoots off to infinity at 90°, because a vertical line has no slope. This is why aiming math breaks straight up.
- Everyday: shadow length as the sun sets.
- Game: camera field of view. The visible half-width is tan(FOV ÷ 2), and it blows up as FOV nears 180°.
- Graded: the sun is 15° above the horizon. How long is the shadow of a 2 m pole? **7.46 m**.

### Module 4 · Finding the angle

**140 `trig-inverse-sine-cosine` · Inverse sine and cosine**
- Idea: "which angle has this ratio?" Each inverse function gives one answer from a fixed range, and the lesson explains why that range is restricted.
- Everyday: the ladder safety angle (about 75°).
- Game: the angle a character must lean to reach a ledge.
- Graded: a 6 m ladder's base is 1.5 m from the wall. What angle does it make with the ground? **75.52°**.

**150 `trig-inverse-tangent-atan2` · Inverse tangent and atan2**
- Idea: arctan(dy ÷ dx) can't tell a target to the upper-left from one to the lower-right. `atan2(dy, dx)` uses both signs to get the full heading. This is the most used trig function in games.
- Everyday: a compass bearing from map coordinates.
- Game: turning a turret to face a target.
- Graded: a turret at (10, 10) must face a target at (4, 16). What heading, in [0°, 360°)? **135°**.

**160 `trig-solving-right-triangles` · Solving right triangles**
- Idea: angles of elevation and depression, combining sides and angles to find everything.
- Everyday: measuring a building's height from across the street.
- Game: a drone camera looking down at a target.
- Graded: standing 50 m from a building with your eyes 1.6 m above the ground, the top of the building is at 38° elevation. How tall is the building? **40.66 m**.

### Module 5 · Identities

**170 `trig-identities-why` · What an identity is**
- Idea: an equation is true for some values; an identity is true for every value. Includes cofunctions: sin(90° − θ) = cos θ, the same ramp seen from the wall instead of the floor.
- Graded (multiple choice): which expression always equals sin(90° − θ)? **cos θ**.

**180 `trig-sum-difference` · Sum and difference formulas**
- Idea: sin(a + b) and cos(a + b) let you combine two turns.
- Everyday: a crane arm raised in two stages.
- Game: rotating an already-rotated offset (a gun barrel on a turret on a tank).
- Graded: a 20 m crane arm is raised 30°, then raised a further 45°. How high is the tip above the pivot? **19.32 m**.

**190 `trig-double-angle` · Double-angle formulas**
- Idea: sin 2θ = 2 sin θ cos θ, which explains why 45° throws farthest.
- Everyday: kicking a ball, a water fountain arc.
- Game: artillery range, R = v² sin 2θ ÷ g.
- Graded: a shell leaves at 30 m/s and 35° (g = 9.81 m/s²). How far does it land, on flat ground? **86.21 m**.

**200 `trig-power-reduction` · Power reduction and RMS**
- Idea: sin² θ = (1 − cos 2θ) ÷ 2, so the average of sin² over a cycle is ½. That's why "120 V" outlets peak near 170 V. Bridges to the Electrical Engineering track.
- Everyday: wall voltage ratings in the US and Europe.
- Game: average damage from a pulsing beam.
- Graded: European mains peaks at 340 V. What is its RMS voltage? **240.4 V**.

### Module 6 · Solving trig equations

**210 `trig-equations-basic` · One cycle, two answers**
- Idea: sin θ = k usually has two answers per turn. Find one with the inverse function and the other by symmetry.
- Everyday: on the Ferris wheel from lesson 120, when are you 32 m up?
- Game: when a bobbing platform is at jump height.
- Graded: how many minutes after boarding does the rider first reach 32 m? **2.67 min** (s accepted).

**220 `trig-equations-all-solutions` · Every solution, and how long it lasts**
- Idea: add whole periods to get every solution, then use pairs of solutions to measure how long something lasts.
- Everyday: a tide of h = 5 + 4 sin(2πt ÷ 12.4) ft. A boat needs more than 8 ft of water to cross the harbor bar.
- Game: how long a guard's sweeping searchlight covers a doorway.
- Graded: how many hours per tide cycle can the boat cross? **2.85 h** (min accepted).

**230 `trig-equations-identities` · Solving with identities**
- Idea: rewrite with an identity so only one trig function remains, then solve.
- Game: two firing angles reach the same target, a low direct shot and a high lob.
- Graded: a shell leaves at 30 m/s (g = 9.81 m/s²). Which high-arc angle lands it 70 m away? **65.14°**.

### Module 7 · Triangles without a right angle

**240 `trig-law-of-sines` · Law of sines**
- Idea: each side is proportional to the sine of the angle opposite it.
- Everyday: two lookout towers spotting a wildfire.
- Game: triangulating a hidden enemy from two scouts.
- Graded: towers A and B are 10 km apart. The fire is 52° off the baseline from A and 61° from B. How far is the fire from A? **9.50 km**.

**250 `trig-ambiguous-case` · The ambiguous case**
- Idea: knowing two sides and an angle that isn't between them can give zero, one or two triangles.
- Everyday: a fixed-length brace that can meet a beam at two different points.
- Graded (multiple choice): given the brace, beam and angle, how many triangles are possible? (zero / one / two).

**260 `trig-law-of-cosines` · Law of cosines**
- Idea: the Pythagorean theorem with a correction term for angles that aren't 90°.
- Everyday: two ships leave port in different directions.
- Game: distance between two units given each one's range and bearing from base.
- Graded: ships sail 12 km and 9 km from port on courses 40° apart. How far apart are they? **7.72 km**.

**270 `trig-triangle-area` · Triangle area from sides and angles**
- Idea: area = ½ab sin C, plus Heron's formula when you know only the sides.
- Everyday: the area of a triangular plot of land.
- Game: the size of a triangular capture zone.
- Graded: a plot has sides of 80 m and 65 m with 72° between them. What is its area? **2,472.7 m²**.

### Module 8 · Vectors, rotation and polar form

**280 `trig-vectors-components` · Vector components**
- Idea: break a magnitude and direction into x and y parts, add the parts, then rebuild.
- Everyday: an airplane in a crosswind.
- Game: movement plus knockback.
- Graded: a plane flies east at 200 km/h, and wind pushes it at 50 km/h toward heading 60°. What is its ground speed? **229.13 km/h**.

**290 `trig-dot-product` · The dot product**
- Idea: a · b = |a||b| cos θ measures how much two directions agree.
- Everyday: work done pulling a sled with a slanted rope.
- Game: "is the player inside this guard's 60° vision cone?" and surface lighting.
- Graded: you pull a sled 30 m with 120 N of force on a rope at 25°. How much work do you do? **3,262.7 J**.

**300 `trig-rotation` · Rotating a point**
- Idea: x′ = x cos θ − y sin θ, y′ = x sin θ + y cos θ, derived from the sum formulas in lesson 180.
- Everyday: a robot arm joint.
- Game: rotating a sprite's hitbox corners.
- Graded: a hitbox corner at offset (4, 2) m rotates 30° counterclockwise. What is its new y offset? **3.73 m**.

**310 `trig-polar-coordinates` · Polar coordinates**
- Idea: describe a point by distance and angle (r, θ) instead of (x, y), and convert between the two.
- Everyday: a radar screen, a microphone pickup pattern.
- Game: a minimap radar.
- Graded: a radar blip is 12 km away at heading 150°. How far west of the radar is it? **10.39 km**.

**320 `trig-phasors` · Phasors: adding waves as arrows**
- Idea: a sine wave can be drawn as a spinning arrow, and two waves of the same frequency add like vectors. Bridges to the Electrical Engineering track's AC lessons.
- Everyday: two AC sources 60° out of phase.
- Game: two overlapping sound effects reinforcing or cancelling each other.
- Graded: two 100 V waves of the same frequency are 60° out of phase. What is the peak of their sum? **173.2 V**.

### Module 9 · Capstone

**330 `trig-capstone` · Capstone: track an orbiting drone**
- Idea: as in the Precalculus capstone, nobody tells you which tool to use. Turn time into angle (angular speed), place the drone (unit circle), then measure the distance from someone off-center (law of cosines).
- Game/everyday: a patrol drone orbits a tower at 30 m radius, once every 20 s, starting due east. A sniper hides 50 m east of the tower.
- Graded: how far is the drone from the sniper 7.5 s after it passes due east? **74.31 m**.

## Wiring the course in (implementation checklist)

The app assumes the only math course is Precalculus in a few places:

- [ ] `TrigonometryCurriculum.cs` with `BuildTrigonometryLessons()`, reusing (or renaming to shared math helpers) `PrecalculusNumeric`, `PrecalculusChoice` and `PrecalculusUnits`.
- [ ] `Curriculum.TrigonometryLessons`, plus `TrigonometryCourse` in `CourseCatalog.cs` (lesson list, course map and catalog item).
- [ ] Generalize the Precalculus authoring-rule test in `CurriculumInvariantTests.cs` to every languageId `math` course, and add a check that angle prompts name their unit (rule 7).
- [ ] `frontend/src/App.tsx:273`: the page title maps languageId `math` to "Precalculus". Switch math courses to the course title.
- [ ] `frontend/src/App.tsx:120`: check the math-specific label.
- [ ] `frontend/src/App.tsx:850`: add Trigonometry to onboarding, after Precalculus.
- [ ] `frontend/src/App.tsx:1620`: project-studio stages for `trigonometry`.
- [ ] `frontend/e2e/api-contracts.spec.ts`: add `trigonometry` to the course list and a contract test like the Precalculus one (last lesson is `trig-capstone`; a numeric answer with unit conversion validates).
- [ ] Career readiness and assessments: check whether Precalculus has entries in `CareerReadinessCatalog.cs` / `AssessmentCatalog.cs` and mirror them.

## Open questions

1. **Negative answers.** Rule 8 works within rule 5, but some lessons would read more naturally with a signed answer. An alternative is a ± toggle on the numeric input. That would be a frontend change that also benefits future calculus and linear algebra courses.
2. **Graph exercises.** Lessons 80–130 would teach better with an interactive graph (drag amplitude/period/phase sliders). The current exercise kinds are numeric and multiple choice, so the outline grades a number from each graph. An interactive graph could be added later.
3. **Phasors (320).** It's the strongest link to the EE track, but it's the most advanced lesson. It could move to an optional extension if the course feels long.
