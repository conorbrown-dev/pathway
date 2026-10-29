using System.Globalization;

namespace Pathway.Content;

/// <summary>Server-owned, generated mathematics problems. Nothing in the lesson JSON exposes solutions.</summary>
public record MathMasteryQuestion(string Id, string Skill, string Kind, string Prompt, string? Unit, string[] Choices, double? Expected, string? CorrectChoice)
{
    public MathMasteryPublicQuestion Public() => new(Id, Skill, Kind, Prompt, Unit, Choices);
}
public record MathMasteryPublicQuestion(string Id, string Skill, string Kind, string Prompt, string? Unit, string[] Choices);

/// <summary>
/// Eight independent exercises per attempt, with two samples from each of four
/// representations: calculation, modeling, interpretation and transfer.
/// All 94+ math lessons are mapped to a reviewed mathematical problem family.
/// </summary>
public static class MathMasteryCatalog
{
    public const int QuestionCount = 8;
    public const int RequiredCorrect = 7;
    private static readonly string[] Skills = ["Calculate", "Model", "Interpret", "Apply"];
    public static bool IsMathLesson(string slug) => CourseCatalog.Courses.Values
        .Where(course => course.LanguageId == "math")
        .SelectMany(course => course.Modules)
        .SelectMany(module => module.Lessons)
        .Any(lesson => lesson.Slug == slug);

    // Intentional mappings: there is no generic fallback that can silently test the wrong topic.
    public static string Family(string slug) => slug switch
    {
        "prealgebra-place-value" => "place",
        "prealgebra-four-operations" => "arithmetic",
        "prealgebra-operation-order" or "math-order-of-operations" => "precedence",
        "prealgebra-factors-multiples" => "factors",
        "prealgebra-fraction-meaning" or "prealgebra-equivalent-fractions" => "fraction",
        "prealgebra-fraction-add-subtract" => "fractionAdd",
        "prealgebra-fraction-multiply-divide" => "fractionMul",
        "prealgebra-decimals" or "prealgebra-decimal-arithmetic" => "decimal",
        "prealgebra-ratios-rates" or "prealgebra-proportions" or "prealgebra-constant-proportionality" or "math-rates" => "rate",
        "prealgebra-percent" => "percent",
        "prealgebra-integers" or "prealgebra-signed-arithmetic" or "math-negative-numbers" => "signed",
        "prealgebra-exponents" or "algebra1-exponent-rules" or "math-exponents" => "power",
        "prealgebra-roots" => "roots",
        "prealgebra-variables" or "prealgebra-evaluate-expressions" or "prealgebra-like-terms" or "prealgebra-distributive"
          or "algebra1-meaning-of-expressions" or "algebra1-like-terms" or "algebra1-distribution-properties"
          or "math-variables-expressions" => "expression",
        "prealgebra-equality-balance" or "prealgebra-one-step" or "prealgebra-two-step"
          or "algebra1-multistep-equations" or "algebra1-both-sides-equations" or "algebra1-rearrange-formulas"
          or "math-linear-equations" => "equation",
        "prealgebra-inequalities" or "algebra1-inequality-operations" or "algebra1-compound-inequalities"
          or "algebra1-inequality-word-models" or "math-inequalities" => "inequality",
        "prealgebra-coordinate-plane" or "math-coordinate-plane" => "coordinate",
        "prealgebra-tables-patterns" or "prealgebra-slope"
          or "algebra1-slope-from-points" or "algebra1-slope-intercept" or "algebra1-parallel-perpendicular"
          or "math-slope" or "math-linear-models" => "linear",
        "prealgebra-area-perimeter" => "geometry",
        "prealgebra-volume-measurement" => "volume",
        "prealgebra-data-probability" => "statistics",
        "prealgebra-word-problems" or "prealgebra-capstone" or "algebra1-algebra-capstone" or "math-precalc-capstone" => "budget",
        "algebra1-systems-graphing" or "algebra1-systems-substitution" or "algebra1-systems-elimination" or "math-systems" => "systems",
        "algebra1-scientific-notation" => "scientific",
        "algebra1-exponential-functions" or "math-exponential-growth" or "math-exponential-decay" or "math-solving-exponential" => "growth",
        "algebra1-polynomial-terms" or "algebra1-add-polynomials" or "algebra1-multiply-polynomials" or "math-polynomials" => "polynomial",
        "algebra1-greatest-common-factor" or "algebra1-factor-trinomials" or "algebra1-difference-of-squares" => "factoring",
        "algebra1-parabola-graphs" or "algebra1-complete-square" or "algebra1-quadratic-formula"
          or "math-quadratics" or "math-solving-quadratics" => "quadratic",
        "algebra1-function-notation" or "algebra1-function-transformations" or "algebra1-piecewise-functions"
          or "math-functions" or "math-domain-range" or "math-transformations" or "math-piecewise" => "function",
        "algebra1-choose-math-model" or "algebra1-model-from-data" => "models",
        "math-rational-functions" => "rational",
        "math-logarithms" => "log",
        "math-angles-radians" or "math-right-triangle-trig" or "math-unit-circle" => "trig",
        "math-sequences" => "sequences",
        _ => throw new ArgumentException("Math mastery family is not defined for " + slug, nameof(slug))
    };

    public static MathMasteryQuestion[] Create(string slug, int seed)
    {
        if (!IsMathLesson(slug)) throw new ArgumentException("Not a mathematics lesson.", nameof(slug));
        var family = Family(slug);
        var random = new Random(seed);
        var questions = new MathMasteryQuestion[QuestionCount];
        for (var i = 0; i < questions.Length; i++)
            questions[i] = CreateQuestion(slug, family, random, i);
        if (questions.Select(q => q.Prompt).Distinct().Count() != questions.Length)
            throw new InvalidOperationException("Generated duplicate mastery questions for " + slug);
        return questions;
    }

    private static MathMasteryQuestion CreateQuestion(string slug, string family, Random r, int index)
    {
        var k = index % 4;
        var a = r.Next(3, 12) + index * 3; // varying inputs, no identical repeats
        var b = r.Next(2, 8) + index * 2;
        var c = r.Next(2, 6) + index;
        var id = "q" + (index + 1).ToString(CultureInfo.InvariantCulture);
        var skill = Skills[k];
        MathMasteryQuestion N(string prompt, double answer, string? unit = null)
            => new(id, skill, "Numeric", prompt, unit, [], answer, null);
        MathMasteryQuestion C(string prompt, string answer, string wrong1, string wrong2)
            => new(id, skill, "Choice", prompt, null, new[] { answer, wrong1, wrong2 }.OrderBy(_ => r.Next()).ToArray(), null, answer);


        // Lessons requiring specific methods get explicit question families rather than
        // being assessed by general questions from the surrounding unit.
        switch (slug)
        {
            case "algebra1-parallel-perpendicular":
                return k switch {
                    0 => N("A line has slope " + a + ". Find the perpendicular slope (3 decimal places).", Math.Round(-1d/a,3)),
                    1 => N("A road parallel to y=" + a + "x+" + b + " has which slope?",a),
                    2 => C("A line with slope " + a + " is perpendicular to which slope?",(-1d/a).ToString("0.###",CultureInfo.InvariantCulture),a.ToString(CultureInfo.InvariantCulture),(-a).ToString(CultureInfo.InvariantCulture)),
                    _ => N("For a line rising " + (a*b) + " units over " + b + " units of run, find the perpendicular slope (3 decimals).",Math.Round(-1d/a,3))
                };
            case "algebra1-rearrange-formulas":
                return k switch {
                    0 => N("Area = length × width. A=" + a*b + " cm² and length=" + a + " cm. Find width.",b,"cm"),
                    1 => N("Distance = rate × time. Distance=" + a*b + " km in " + b + " hours. Find rate.",a,"km/h"),
                    2 => C("From F = ma with F = " + a + " N, which formula isolates mass?", "m=F/a", "m=Fa", "m=a/F"),
                    _ => N("From P=2(length+width), perimeter is " + 2*(a+b) + " m and length is " + a + " m. Find width.",b,"m")
                };
            case "algebra1-compound-inequalities":
                return k switch {
                    0 => N("Find the lower boundary for " + a + " ≤ x + " + b + " ≤ " + (a+b+c) + ".",a-b),
                    1 => N("Find the upper boundary of " + a + " ≤ 2x ≤ " + (2*(a+b)) + ".",a+b),
                    2 => C("Does the interval " + a + " ≤ x < " + (a+b) + " include its lower endpoint?", "Yes, but not the upper endpoint", "No, neither endpoint", "Yes, both endpoints"),
                    _ => N("Find the lower boundary for " + (a+2*b) + " ≤ 2x + " + a + " ≤ " + (a+2*b+2*c) + ".",b)
                };
            case "algebra1-factor-trinomials":
                return k switch {
                    0 => N("Factor x²+" + (a+b) + "x+" + a*b + " as (x+" + a + ")(x+n). Find n.",b),
                    1 => N("A rectangular area's polynomial is x²+" + (a+b) + "x+" + a*b + ". One side is x+" + a + ". Find the constant in the other side.",b),
                    2 => C("Which pair adds to " + (a+b) + " and multiplies to " + a*b + "?",a + " and " + b,a + " and " + (b+1),(a+b) + " and 1"),
                    _ => N("Solve x²+" + (a+b) + "x+" + a*b + "=0. Find the larger (less negative) real root.",-Math.Min(a,b))
                };
            case "algebra1-complete-square":
                return k switch {
                    0 => N("Complete the perfect square x²+" + 2*a + "x+n. Find n.",a*a),
                    1 => N("Complete x²+" + 2*a + "x+" + b + " and find its vertex's minimum y-coordinate.",b-a*a),
                    2 => C("What completes x²+" + 2*a + "x+n into a perfect square?",(a*a).ToString(),(a*a+1).ToString(),(a*a+2).ToString()),
                    _ => N("Rewrite x²+" + 2*a + "x+" + b + " as (x+" + a + ")²+k. Find k.",b-a*a)
                };
            case "algebra1-quadratic-formula":
                return k switch {
                    0 => N("Using the quadratic formula, solve x²−" + (a+b) + "x+" + a*b + "=0. Find the larger root.",Math.Max(a,b)),
                    1 => N("For x²−" + (a+b) + "x+" + a*b + "=0, calculate the discriminant b²−4ac.",(a-b)*(a-b)),
                    2 => C("If the discriminant is " + (a-b)*(a-b) + ", how many distinct real roots?",a==b ? "One" : "Two",a==b ? "Two" : "One","Zero"),
                    _ => N("Solve x²+" + 2*a + "x+" + a*a + "=0 using the quadratic formula. Find the repeated root.",-a)
                };
            case "algebra1-multiply-polynomials":
                return k switch {
                    0 => N("Expand (x+" + a + ")(x+" + b + "). What is the coefficient of x?",a+b),
                    1 => N("A rectangle has sides x+" + a + " and x+" + b + ". Its area when x=" + c + " is?",(c+a)*(c+b),"units²"),
                    2 => C("Expand (x+" + a + ")². What is the coefficient of x?",(2*a).ToString(),a.ToString(),(a*a).ToString()),
                    _ => N("Expand (x+" + a + ")(x+" + b + "). What is the constant term?",a*b)
                };
            case "algebra1-function-transformations":
            case "math-transformations":
                return k switch {
                    0 => N("The parabola y=(x−" + a + ")²+" + b + " has a vertex at which x?",a),
                    1 => N("Shift y=x² to the right " + a + " and down " + b + ". What is the new vertex's y-coordinate?",-b),
                    2 => C("Which transformation moves f(x) right by " + a + "?", "f(x−" + a + ")", "f(x+" + a + ")", "f(x)+" + a),
                    _ => N("The parabola y=−(x+" + a + ")²+" + b + " has what maximum y-value?",b)
                };
            case "algebra1-piecewise-functions":
            case "math-piecewise":
                return k switch {
                    0 => N("f(x)=" + a + " for x≤0, and f(x)=2x+" + b + " for x>0. Find f(0).",a),
                    1 => N("A service costs $" + a + " for up to two hours, then $" + b + " per extra hour. What does " + (c+2) + " hours cost?",a+b*c,"USD"),
                    2 => C("f(x)=" + a + " for x≤" + b + ", and f(x)=" + c + " for x>" + b + ". At x=" + b + ", which rule applies?", "The first rule", "The second rule", "Both values added"),
                    _ => N("f(x)=x² for x<0, and f(x)=x+" + a + " for x≥0. Find f(" + c + ").",c+a)
                };
            case "math-domain-range":
                return k switch {
                    0 => N("For f(x)=1/(x−" + a + "), which x is excluded from the domain?",a),
                    1 => N("For f(x)=√(x−" + a + "), what is the minimum real input x?",a),
                    2 => C("For y=(x−" + a + ")²+" + b + ", what is its range?", "y≥" + b, "y≤" + b, "all real y"),
                    _ => N("For y=(x+" + a + ")²−" + b + ", find its minimum output.",-b)
                };
            case "math-solving-exponential":
                return k switch {
                    0 => N("Solve 2^x=" + Math.Pow(2,c) + ". Find x.",c),
                    1 => N("A population of " + a + " doubles every hour. How many hours until it reaches " + a*Math.Pow(2,b) + "?",b,"hours"),
                    2 => C("What operation isolates x in 3^x=" + Math.Pow(3,c) + "?", "Apply log base 3", "Subtract 3", "Multiply by 3"),
                    _ => N("Solve 10^x=" + Math.Pow(10,b) + ". Find x.",b)
                };
            case "math-angles-radians":
                return k switch {
                    0 => N("Convert " + 30*c + " degrees to radians (3 decimals).",Math.Round(30*c*Math.PI/180,3),"rad"),
                    1 => N("Convert " + a + "π radians to degrees.",180*a,"degrees"),
                    2 => index<4 ? C("A full revolution is how many radians?", "2π", "π", "π/2") : C("A half revolution is how many radians?", "π", "2π", "π/2"),
                    _ => N("Convert " + 45*b + " degrees to radians (3 decimals).",Math.Round(45*b*Math.PI/180,3),"rad")
                };
            case "math-right-triangle-trig":
                return k switch {
                    0 => N("A right triangle has legs " + 3*c + " and " + 4*c + ". Find sin of the angle opposite the shorter leg.",.6),
                    1 => N("The opposite side is " + 3*c + " and the adjacent side is " + 4*c + ". Find tan(angle).",.75),
                    2 => index<4 ? C("Which ratio defines cosine?", "Adjacent / hypotenuse", "Opposite / hypotenuse", "Opposite / adjacent") : C("Which ratio defines sine?", "Opposite / hypotenuse", "Adjacent / hypotenuse", "Opposite / adjacent"),
                    _ => N("A right triangle has legs " + 3*c + " and " + 4*c + ". Find its hypotenuse.",5*c)
                };
            case "math-unit-circle":
                return k switch {
                    0 => index<4 ? N("On the unit circle, x-coordinate at 0°?",1) : N("On the unit circle, x-coordinate at 180°?",-1),
                    1 => index<4 ? N("On the unit circle, y-coordinate at 90°?",1) : N("On the unit circle, y-coordinate at 270°?",-1),
                    2 => index<4 ? C("What is the point at 180°?", "(-1,0)", "(1,0)", "(0,1)") : C("What is the point at 90°?", "(0,1)", "(1,0)", "(0,-1)"),
                    _ => index<4 ? N("At 270°, what is sin(angle)?",-1) : N("At 90°, what is cos(angle)?",0)
                };
        }

        switch (family)
        {
            case "place":
                return k switch {
                    0 => N("Round " + (a * 100 + b * 10 + c) + " to the nearest hundred.", Math.Round((a*100 + b*10 + c)/100d, MidpointRounding.AwayFromZero) * 100),
                    1 => N("A warehouse has " + (a * 1000 + b * 100) + " items and ships " + (c * 100) + ". How many remain?", a * 1000 + b * 100 - c * 100, "items"),
                    2 => C("Which is the value of the hundreds digit in " + (a * 1000 + 400 + c) + "?", "400", "40", "4"),
                    _ => N("Estimate " + (a * 10 + 1) + " + " + (b * 10 + 2) + " by rounding each number to the nearest ten.", (a + b) * 10)
                };
            case "arithmetic":
                return k switch {
                    0 => N("Compute " + a + " × " + b + ".", a*b),
                    1 => N("You purchase " + b + " packs of " + a + " screws. How many screws?", a*b, "screws"),
                    2 => C("Which operation finds how many equal groups of " + b + " fit into " + (a*b) + "?", "Division", "Addition", "Subtraction"),
                    _ => N("Compute (" + (a*b) + " ÷ " + b + ") + " + c + ".", a+c)
                };
            case "precedence":
                return k switch {
                    0 => N("Evaluate " + a + " + " + b + " × " + c + ".", a+b*c),
                    1 => N("Evaluate (" + a + " + " + b + ") × " + c + ".", (a+b)*c),
                    2 => C("When evaluating " + a + " + " + b + " × " + c + ", which part comes first?", "Multiplication", "Addition", "Whichever is on the left"),
                    _ => N("Evaluate " + a + " × (" + b + " + " + c + ") − " + b + ".", a*(b+c)-b)
                };
            case "factors":
                return k switch {
                    0 => N("Find the greatest common factor of " + (a*b) + " and " + (a*c) + " and " + (a*c) + ". Find their greatest common factor.", Gcd(a*b,a*c)),
                    1 => N("Find the least common multiple of " + a + " and " + b + ".", a*b/Gcd(a,b)),
                    2 => index < 4 ? C("Is 1 a prime number?", "No, it has only one positive factor", "Yes, it is odd", "Yes, it divides every integer") : C("Is 2 a prime number?", "Yes, it has exactly two positive factors", "No, it is even", "No, because 1 divides it"),
                    _ => N("How many positive factors does " + (a*b) + " have?", DivisorCount(a*b))
                };
            case "fraction":
                return k switch {
                    0 => N("What decimal represents " + a + " out of " + (a+b) + " equal parts? Round to three decimal places.", Math.Round((double)a/(a+b), 3)),
                    1 => N("A recipe needs " + a + "/" + b + " cups. Doubling it gives how many cups? Enter a decimal rounded to three places.", Math.Round(2d*a/b, 3), "cups"),
                    2 => C("Which fraction is equivalent to " + a + "/" + b + "?", (a*2) + "/" + (b*2), a + "/" + (b+1), (a+1) + "/" + b),
                    _ => N("Convert " + a + "/" + b + " into a decimal. Round to three decimal places.", Math.Round((double)a/b,3))
                };
            case "fractionAdd":
                return k switch {
                    0 => N("Evaluate " + a + "/" + b + " + " + c + "/" + b + " as a decimal (3 places).", Math.Round((double)(a+c)/b,3)),
                    1 => N("You use " + a + "/" + b + " L, then another " + c + "/" + b + " L. How many liters in total? Decimal (3 places).", Math.Round((double)(a+c)/b,3),"L"),
                    2 => C("To add 1/" + b + " and 1/" + c + ", what must you find first?", "A common denominator", "The product of the numerators", "The sum of the denominators"),
                    _ => N("Evaluate " + a + "/" + b + " − " + c + "/" + b + " as a decimal (3 places).", Math.Round((double)(a-c)/b,3))
                };
            case "fractionMul":
                return k switch {
                    0 => N("Evaluate (" + a + "/" + b + ") × (" + c + "/" + a + "). Enter a decimal rounded to three places.",Math.Round((double)c/b,3)),
                    1 => N("A plan uses " + a + "/" + b + " kg per batch. What mass do " + c + " batches use? Decimal (3 places).",Math.Round((double)a*c/b,3),"kg"),
                    2 => C("Which operation turns division by " + a + "/" + (a+b) + " into multiplication?", "Multiply by " + (a+b) + "/" + a, "Multiply by " + a + "/" + (a+b), "Add " + (a+b) + "/" + a),
                    _ => N("Evaluate (" + a + "/" + b + ") ÷ (" + c + "/" + b + "). Decimal (3 places).",Math.Round((double)a/c,3))
                };
            case "decimal":
                return k switch {
                    0 => N("Add " + (a/10d).ToString("F1",CultureInfo.InvariantCulture) + " and " + (b/10d).ToString("F1",CultureInfo.InvariantCulture) + ".", (a+b)/10d),
                    1 => N("You buy " + a + " items at $0." + (b%9+1) + " each. What is the total cost in dollars?",a*(b%9+1)/10d,"USD"),
                    2 => C("In the decimal " + a + ".37, what is the value of the digit 7?", "7 hundredths", "7 tenths", "7 thousandths"),
                    _ => N("Convert " + a + " hundredths into a decimal.",a/100d)
                };
            case "rate":
                return k switch {
                    0 => N("A conveyor creates " + (a*b) + " parts in " + b + " minutes. Find the constant of proportionality in parts per minute.",a,"parts/min"),
                    1 => N("A proportional ride covers " + a + " kilometers each hour. How far in " + b + " hours?",a*b,"km"),
                    2 => C("A proportional relationship has constant of proportionality " + a + ". Which equation represents it?", "y = " + a + "x", "y = " + a + "x + 5", "y = x² + " + a),
                    _ => N("A table has x = 1, 2, 3 and y = " + a + ", " + 2*a + ", " + 3*a + ". Find k = y/x.",a)
                };
            case "percent":
                return k switch {
                    0 => N("Find " + b*5 + "% of " + (a*20) + ".",a*20*b*5/100d),
                    1 => N("A $"+ (a*20) +" jacket is discounted " + b*5 + "%. Find the sale price.",a*20*(1-b*5/100d),"USD"),
                    2 => C("An increase of " + b*5 + "% means multiply the original by which factor?",(1+b*5/100d).ToString("0.##",CultureInfo.InvariantCulture),b*5/100d+"", "1"),
                    _ => N("A quantity rises from " + (a*10) + " to " + (a*10 + b*10) + ". What is the percent increase? Round to 2 decimals.",Math.Round(100d*b/a,2),"%")
                };
            case "signed":
                return k switch {
                    0 => N("Compute (−" + a + ") + " + b + ".",-a+b),
                    1 => N("It is −" + a + " °C and rises " + b + " °C. What is the final temperature?",-a+b,"°C"),
                    2 => C("What does |−" + a + "| measure?", "Distance " + a + " from zero", "Negative distance −" + a, "Two different positions"),
                    _ => N("Compute −" + a + " − " + b + ".",-a-b)
                };
            case "power":
                return k switch {
                    0 => N("Evaluate " + b + "³.",b*b*b),
                    1 => N("Starting with " + a + " cells, doubling " + c + " times gives how many cells?",a*Math.Pow(2,c),"cells"),
                    2 => C("What does " + b + "⁻² mean?", "1/" + (b*b), "−"+(b*b),b*b+""),
                    _ => N("Simplify 2⁵ × 2" + "² ÷ 2⁴.",8)
                };
            case "roots":
                return k switch {
                    0 => N("What is √" + (a*a) + " (principal square root)?",a),
                    1 => N("A square has area " + (a*a) + " cm². How long is one side?",a,"cm"),
                    2 => C("Which root does √" + (a*a) + " denote?", "The nonnegative root "+a,"Both +"+a+" and −"+a,"The negative root −"+a),
                    _ => N("The hypotenuse of a right triangle has legs " + (3*c) + " and " + (4*c) + ". Find its length.",5*c)
                };
            case "expression":
                return k switch {
                    0 => N("If x = " + b + ", evaluate " + a + "x + " + c + ".",a*b+c),
                    1 => N("Each of " + b + " boxes contains " + a + " units plus " + c + " extra units overall. How many total?",a*b+c,"units"),
                    2 => C("Which simplifies "+a+"x + "+b+"x?",(a+b)+"x",(a*b)+"x",(a+b)+"x²"),
                    _ => N("For x = " + b + ", evaluate " + a + "(x + " + c + ") − " + a + "x.",a*c)
                };
            case "equation":
                return k switch {
                    0 => N("Solve "+a+"x + "+b+" = "+(a*c+b)+". Find x.",c),
                    1 => N("A ride costs $"+b+" upfront plus $"+a+" per mile. Total charge is $"+(b+a*c)+". How many miles?",c,"miles"),
                    2 => C("What keeps an equation balanced?", "Apply the same operation to both sides", "Change only the left side", "Divide by zero"),
                    _ => N("Solve "+(a+b)+"x + "+b+" = "+b+"x + "+(a*c+b)+". Find x.",c)
                };
            case "inequality":
                return k switch {
                    0 => N("Find the boundary x in "+a+"x + "+b+" ≤ "+(a*c+b)+".",c),
                    1 => N("A $"+b+" entry fee and $"+a+" rides must fit a $"+(b+a*c)+" budget. Maximum whole rides?",c,"rides"),
                    2 => C("When dividing −"+a+"x < −"+(a*c)+" by −"+a+", what must happen?", "Reverse < into >", "Keep < unchanged", "Replace < with ="),
                    _ => N("Find the boundary in −"+a+"x + "+b+" ≥ "+(b-a*c)+".",c)
                };
            case "coordinate":
                return k switch {
                    0 => N("For the point ("+a+", −"+b+"), what is its y-coordinate?",-b),
                    1 => N("Move "+a+" steps right from x = −"+b+". What is the new x-coordinate?",a-b),
                    2 => C("Which quadrant contains ("+a+", −"+b+")?", "Quadrant IV", "Quadrant II", "Quadrant I"),
                    _ => N("A point starts at ("+a+","+b+"). Move "+c+" units left. What is the new x-coordinate?",a-c)
                };
            case "linear":
                return k switch {
                    0 => N("A line passes through (0,"+b+") and (2,"+(2*a+b)+"). Find its slope.",a),
                    1 => N("For y = "+a+"x + "+b+", find y at x = "+c+".",a*c+b),
                    2 => C("Does y = "+a+"x + "+b+" pass through the origin?", "No, it has a nonzero y-intercept", "Yes, all lines pass through the origin", "Only for negative x"),
                    _ => N("A tank starts with "+b+" L and gains "+a+" L/min. How much after "+c+" minutes?",b+a*c,"L")
                };
            case "geometry":
                return k switch {
                    0 => N("A rectangle has width "+b+" cm and length "+a+" cm. Find area.",a*b,"cm²"),
                    1 => N("A fence encloses a "+a+" m by "+b+" m rectangle. How many meters of perimeter?",2*(a+b),"m"),
                    2 => index < 4 ? C("Which unit describes rectangle area?", "cm²", "cm", "cm³") : C("Which unit describes the distance around a rectangle?", "cm", "cm²", "cm³"),
                    _ => N("A triangle has base "+(2*b)+" cm and height "+a+" cm. Find area.",a*b,"cm²")
                };
            case "volume":
                return k switch {
                    0 => N("A box measures "+a+" cm × "+b+" cm × "+c+" cm. Find its volume.",a*b*c,"cm³"),
                    1 => N("Convert "+a+" L to milliliters.",a*1000,"mL"),
                    2 => index < 4 ? C("What unit describes the volume of a cube?", "cm³", "cm²", "cm") : C("What unit describes a square face of a cube?", "cm²", "cm", "cm³"),
                    _ => N("A cube has side length "+b+" cm. Find its volume.",b*b*b,"cm³")
                };
            case "statistics":
                return k switch {
                    0 => N("Find the mean of "+a+", "+(a+2)+", "+(a+4)+".",a+2),
                    1 => N("A bag has "+a+" blue beads and "+b+" red beads. Probability of blue? Decimal (3 places).",Math.Round((double)a/(a+b),3)),
                    2 => C("For values "+a+", "+(a+3)+", "+(a+100)+", which is less affected by the outlier?", "Median", "Mean", "Maximum"),
                    _ => N("Find the median of "+a+", "+(a+2)+", "+(a+5)+", "+(a+10)+", "+(a+12)+".",a+5)
                };
            case "budget":
                return k switch {
                    0 => N("You start with $"+(a*20)+" and spend $"+(b*7)+" on supplies. How much remains?",a*20-b*7,"USD"),
                    1 => N("Plan A costs $"+(a*b)+" plus $"+c+" per item; plan B costs $"+(c+b)+" per item. At how many items do costs tie?",a),
                    2 => C("A budget begins with $" + a + " and spends $" + b + ". Which calculation finds the remainder?", "Start minus spending", "Spending plus start", "Start multiplied by spending"),
                    _ => N("Buy "+c+" parts at $"+a+" each, and pay a fixed $"+b+" shipping fee. Find total cost.",a*c+b,"USD")
                };
            case "systems":
                return k switch {
                    0 => N("Solve x+y="+(a+b)+" and x−y="+(a-b)+". What is x?",a),
                    1 => N("Two groups total "+(a+b)+" people; the first group has "+(a>=b ? (a-b)+" more" : (b-a)+" fewer")+" than the second. How many in the first?",a,"people"),
                    2 => C("If y=" + a + "x+" + b + " and y=x+" + c + " intersect, what does their intersection represent?", "A point satisfying both equations", "Only the first line's slope", "Always the origin"),
                    _ => N("For y=x+"+b+" and y=−x+"+(2*a+b)+", find the x-coordinate where they intersect.",a)
                };
            case "scientific":
                return k switch {
                    0 => N("Convert "+a+" × 10³ to ordinary notation.",a*1000),
                    1 => N("A sensor detects "+a+" × 10⁻³ volts. Convert to decimal volts.",a/1000d,"V"),
                    2 => C("In "+a+" × 10⁻⁴, what does a negative power of ten indicate?", "A small fraction", "A negative physical quantity", "Always an integer"),
                    _ => N("Convert "+a*100000+" to the coefficient in scientific notation ×10⁵.",a)
                };
            case "growth":
                return k switch {
                    0 => N("A population of "+a+" doubles "+c+" times. Final population?",a*Math.Pow(2,c)),
                    1 => N("A machine loses half its stock each round. Starting from "+(a*64)+" units, what remains after "+c+" rounds?",a*64/Math.Pow(2,c),"units"),
                    2 => C("A quantity grows by " + a + "% each year. Which rule models equal time steps?", "Multiply by the same factor each year", "Add the same fixed number each year", "Draw a vertical line"),
                    _ => N("A fund starts at $"+(a*10)+" and grows 10% per year. What after 2 years? Round to cents.",Math.Round(a*10*1.21,2),"USD")
                };
            case "polynomial":
                return k switch {
                    0 => N("For P(x)=x²+"+a+"x+"+b+", find P("+c+").",c*c+a*c+b),
                    1 => N("A rectangle has sides (x+"+a+") and (x+"+b+"). What is its area when x="+c+"?",(c+a)*(c+b),"units²"),
                    2 => C("What is the degree of "+a+"x³+"+b+"x²+"+c+"?", "3", "2", a.ToString(CultureInfo.InvariantCulture)),
                    _ => N("Combine ("+a+"x² + "+b+"x) + ("+c+"x² − "+b+"x). What is the coefficient of x²?",a+c)
                };
            case "factoring":
                return k switch {
                    0 => N("x² − "+(a*a)+" = 0. Find the positive root.",a),
                    1 => N("x² + "+(a+b)+"x + "+(a*b)+" = (x+"+a+")(x+k). Find k.",b),
                    2 => C("What is the factorization of x²−"+(a*a)+"?", "(x−"+a+")(x+"+a+")", "(x−"+a+")²", "(x+"+a+")²"),
                    _ => N("Factor "+a+"x + "+(a*b)+" as "+a+"(x+k). Find k.",b)
                };
            case "quadratic":
                return k switch {
                    0 => N("Find the vertex x-coordinate of y=(x−"+a+")²+"+b+".",a),
                    1 => N("Evaluate y=(x−"+a+")²−"+b+" at x="+a+".",-b),
                    2 => C("How many distinct real roots does x²+"+(a*a)+"=0 have?", "Zero", "One", "Two"),
                    _ => N("Find the positive root of x²−"+(a*a)+"=0.",a)
                };
            case "function":
                return k switch {
                    0 => N("For f(x)="+a+"x−"+b+", evaluate f("+c+").",a*c-b),
                    1 => N("A service is modeled by C(t)="+b+"+"+a+"t. Find C("+c+").",b+a*c,"USD"),
                    2 => C("In f(x)="+a+"x+"+b+", what is the domain when x can be any real number?", "All real numbers", "Only nonnegative integers", "Only multiples of "+a),
                    _ => N("For g(x)=(x−"+a+")²+"+b+", find the vertex's y-coordinate.",b)
                };
            case "models":
                return k switch {
                    0 => N("Data: (0,"+b+"), (1,"+(a+b)+"), (2,"+(2*a+b)+"). Find slope.",a),
                    1 => N("A pump starts at "+b+" L and adds "+a+" L/min. Predict volume after "+c+" min.",b+a*c,"L"),
                    2 => C("A plan has a fixed $" + b + " fee and $" + a + " per use. What kind of cost relationship?", "Linear but not proportional", "Proportional through the origin", "Quadratic"),
                    _ => N("Plan A is $"+(a*b)+" plus $"+c+" per item. Plan B is $"+(c+b)+" per item. Find their break-even item count.",a)
                };
            case "rational":
                return k switch {
                    0 => N("Evaluate ("+a+"x)/"+b+" for x="+b+".",a),
                    1 => N("Evaluate "+(a*b)+"/x at x="+b+".",a),
                    2 => C("For f(x)=1/(x−"+a+"), which input is excluded?",a.ToString(CultureInfo.InvariantCulture),"0",(a+1).ToString(CultureInfo.InvariantCulture)),
                    _ => N("Simplify ("+a+"x)/("+b+"x) for x≠0. Decimal (3 places).",Math.Round((double)a/b,3))
                };
            case "log":
                return k switch {
                    0 => N("What is log₂("+Math.Pow(2,c)+")?",c),
                    1 => N("If 2^x="+Math.Pow(2,b)+", solve for x.",b),
                    2 => C("What does log₂("+Math.Pow(2,c)+") ask for?", "The exponent giving "+Math.Pow(2,c)+" from base 2", "Half of "+Math.Pow(2,c), "The square root"),
                    _ => N("How many factors of 10 make 10^"+b+"? Evaluate log₁₀(10^"+b+").",b)
                };
            case "trig":
                return k switch {
                    0 => N("Convert "+(30*c)+"° to radians. Round to 3 decimals.",Math.Round(30*c*Math.PI/180,3),"rad"),
                    1 => N("A right triangle has opposite side "+(3*c)+" and adjacent side "+(4*c)+". Find tan(angle) as a decimal.",0.75),
                    2 => index < 4 ? C("What is sin(90°)?", "1", "0", "−1") : C("What is sin(0°)?", "0", "1", "−1"),
                    _ => N("A sine wave has period "+c+" seconds. What frequency (Hz) does it have? Round to 3 decimals.",Math.Round(1d/c,3),"Hz")
                };
            case "sequences":
                return k switch {
                    0 => N("An arithmetic sequence is "+a+", "+(a+b)+", "+(a+2*b)+". What is the next term?",a+3*b),
                    1 => N("A pattern starts at "+a+" and doubles each term. What is its fourth term?",a*8),
                    2 => C("Sequence " + a + ", " + (a+b) + ", " + (a+2*b) + " follows what kind of pattern?", "Arithmetic", "Geometric", "Random"),
                    _ => N("Find the sum of the first "+c+" terms 1+2+…+"+c+".",c*(c+1)/2d)
                };
            default: throw new InvalidOperationException("Unknown mathematics family: "+family);
        }
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }
    private static int DivisorCount(int n)
    {
        var count = 0;
        for (var i = 1; i <= n; i++) if (n % i == 0) count++;
        return count;
    }

    public static bool Correct(MathMasteryQuestion question, string? submitted)
    {
        if (question.Kind == "Choice") return string.Equals(question.CorrectChoice, submitted, StringComparison.Ordinal);
        if (!double.TryParse(submitted, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
            return false;
        var target = question.Expected ?? double.NaN;
        return Math.Abs(number - target) <= Math.Max(.001, Math.Abs(target) * 0.00001);
    }
}
