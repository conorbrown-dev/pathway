namespace Pathway.Content;

/// <summary>
/// The published course catalog: every course, its modules, and a lookup of lessons by slug.
/// This is the only public entry point to course content.
/// </summary>
/// <remarks>
/// Kept separate from the <c>Curriculum</c> partial class on purpose. C# initializes a class's
/// static fields in file order, so cross-file references inside one partial class can read null.
/// A separate class is only initialized after <c>Curriculum</c> is completely built.
/// </remarks>
public static class CourseCatalog
{
    /// <summary>Reported by /health; the E2E suite pins this value.</summary>
    public const string Version = "C# 14 / .NET 10";

    public static readonly Dictionary<string, Lesson> BySlug = Curriculum.ComputingLessons
        .Concat(Curriculum.ElectricalEngineeringLessons)
        .Concat(Curriculum.PrealgebraLessons)
        .Concat(Curriculum.AlgebraOneLessons)
        .Concat(Curriculum.PrecalculusLessons)
        .Concat(Curriculum.LlmLessons)
        .Concat(Curriculum.WebBasicsLessons)
        .Concat(Curriculum.TailwindLessons)
        .Concat(Curriculum.SQLiteLessons)
        .Concat(Curriculum.LinuxCliLessons)
        .Concat(Curriculum.KubernetesLessons)
        .Concat(Curriculum.GitCliLessons)
        .Concat(Curriculum.ReactCourseLessons)
        .Concat(Curriculum.ReactLiteCourseLessons)
        .Concat(Curriculum.CSharpCourseLessons)
        .Concat(Curriculum.PythonCourseLessons)
        .Concat(Curriculum.RustCourseLessons)
        .Concat(Curriculum.ClaudeLessons)
        .ToDictionary(lesson => lesson.Slug, StringComparer.Ordinal);

    public static readonly Course ComputingCourse = BuildCourse("computing-foundations", "Computing Foundations", "computing", "Core computing", "Machine · data · processes · OS", "2026-09-24", Curriculum.ComputingLessons);
    public static readonly Course ElectricalEngineeringCourse = BuildCourse("electrical-engineering-foundations", "Electrical Engineering Foundations", "electrical-engineering", "EE Foundations", "Circuits · measurement · components · practical builds", "2026-10-16", Curriculum.ElectricalEngineeringLessons);
    public static readonly Course PrealgebraCourse = BuildCourse("prealgebra", "Pre-algebra: the bridge to equations", "math", "Pre-algebra", "Number sense · fractions · percentages · equations", "2026-10-28", Curriculum.PrealgebraLessons);
    public static readonly Course AlgebraOneCourse = BuildCourse("algebra-1", "Algebra I: from equations to functions", "math", "Algebra I", "Linear models · systems · polynomials · quadratics", "2026-10-28", Curriculum.AlgebraOneLessons);
    public static readonly Course PrecalculusCourse = BuildCourse("precalculus", "Precalculus: math you can picture", "math", "Precalculus", "Functions · growth · trigonometry · real-world models", "2026-10-25", Curriculum.PrecalculusLessons);
    public static readonly Course LlmCourse = BuildCourse("ai-llm-engineering", "How AI works: LLMs end to end", "llm", "AI & LLMs", "Math · neural networks · transformers · training · your own model in opencode", "2026-10-26", Curriculum.LlmLessons);
    public static readonly Course GitCliCourse = BuildCourse("git-cli", "Git CLI", "git", "Git CLI", "Repositories · branches · remotes · recovery", "2026-09-26", Curriculum.GitCliLessons);
    public static readonly Course WebBasicsCourse = BuildCourse("web-development-basics", "Web Development Basics 2026", "web", "Web Platform 2026", "HTML · CSS · JavaScript · TypeScript · HTTP", "2026-09-26", Curriculum.WebBasicsLessons);
    public static readonly Course TailwindCourse = BuildCourse("tailwind-css", "Tailwind CSS 2026: from utilities to Pathway", "tailwind", "Tailwind CSS v4", "Vite · Responsive UI · Design tokens", "2026-10-12", Curriculum.TailwindLessons);
    public static readonly Course SQLiteCourse = BuildCourse("sqlite", "SQLite", "sqlite", "SQLite 3.53.4", "sqlite3 CLI · DB Browser for SQLite 3.13.1", "2026-09-26", Curriculum.SQLiteLessons);
    public static readonly Course LinuxCliCourse = BuildCourse("linux-cli-bash-vim", "Linux CLI / Bash / Vim", "linux", "GNU/Linux CLI · Bash 5.3", "GNU coreutils · Vim 9.2", "2026-09-26", Curriculum.LinuxCliLessons);
    public static readonly Course KubernetesCourse = BuildCourse("kubernetes", "Kubernetes: foundations to internals", "kubernetes", "Kubernetes 1.35", "kubectl · containerd · CNI · CSI", "2026-09-26", Curriculum.KubernetesLessons);
    public static readonly Course ReactCourse = BuildCourse("react-enterprise", "React 2026: enterprise applications", "react", "React 19.3 · TypeScript 6.0", "Vite 8.1 · React Router 8 · Tailwind CSS 4.3", "2026-09-26", Curriculum.ReactCourseLessons);
    public static readonly Course ReactLiteCourse = BuildCourse("react-lite", "React Lite 2026: your first admin dashboard", "react", "React 19.3 · TypeScript 6.0", "Vite 8.1 · React Router 8 · Tailwind CSS 4.3", "2026-09-26", Curriculum.ReactLiteCourseLessons);
    public static readonly Course CSharpCourse = BuildCourse("csharp-dotnet", "C# / .NET: zero to staff", "csharp", "C# 14", ".NET 10", "2026-10-21", Curriculum.CSharpCourseLessons);
    public static readonly Course PythonCourse = BuildCourse("python-web", "Python Web: zero to staff", "python", "Python 3.14", "FastAPI · Flask · Django", "2026-09-24", Curriculum.PythonCourseLessons);
    public static readonly Course RustCourse = BuildCourse("rust-systems", "Rust Systems: zero to staff", "rust", "Rust 1.97", "Edition 2024 · Tokio · Axum", "2026-09-24", Curriculum.RustCourseLessons);
    public static readonly Course ClaudeCourse = BuildCourse("claude-engineering", "Claude Engineering: Hooks", "claude", "Claude Code", "Hooks · Agent workflow automation", "2026-09-24", Curriculum.ClaudeLessons);

    public static readonly IReadOnlyDictionary<string, Course> Courses = new Dictionary<string, Course>(StringComparer.Ordinal)
    {
        [ComputingCourse.Id] = ComputingCourse,
        [ElectricalEngineeringCourse.Id] = ElectricalEngineeringCourse,
        [PrealgebraCourse.Id] = PrealgebraCourse,
        [AlgebraOneCourse.Id] = AlgebraOneCourse,
        [PrecalculusCourse.Id] = PrecalculusCourse,
        [LlmCourse.Id] = LlmCourse,
        [GitCliCourse.Id] = GitCliCourse,
        [WebBasicsCourse.Id] = WebBasicsCourse,
        [TailwindCourse.Id] = TailwindCourse,
        [SQLiteCourse.Id] = SQLiteCourse,
        [LinuxCliCourse.Id] = LinuxCliCourse,
        [KubernetesCourse.Id] = KubernetesCourse,
        [ReactCourse.Id] = ReactCourse,
        [ReactLiteCourse.Id] = ReactLiteCourse,
        [CSharpCourse.Id] = CSharpCourse,
        [PythonCourse.Id] = PythonCourse,
        [RustCourse.Id] = RustCourse,
        [ClaudeCourse.Id] = ClaudeCourse
    };

    public static readonly IReadOnlyList<CourseCatalogItem> Catalog =
    [
        new(ComputingCourse.Id, ComputingCourse.Title, ComputingCourse.LanguageId, ComputingCourse.LanguageVersion, ComputingCourse.FrameworkVersion, true),
        new(ElectricalEngineeringCourse.Id, ElectricalEngineeringCourse.Title, ElectricalEngineeringCourse.LanguageId, ElectricalEngineeringCourse.LanguageVersion, ElectricalEngineeringCourse.FrameworkVersion, true),
        new(PrealgebraCourse.Id, PrealgebraCourse.Title, PrealgebraCourse.LanguageId, PrealgebraCourse.LanguageVersion, PrealgebraCourse.FrameworkVersion, true),
        new(AlgebraOneCourse.Id, AlgebraOneCourse.Title, AlgebraOneCourse.LanguageId, AlgebraOneCourse.LanguageVersion, AlgebraOneCourse.FrameworkVersion, true),
        new(PrecalculusCourse.Id, PrecalculusCourse.Title, PrecalculusCourse.LanguageId, PrecalculusCourse.LanguageVersion, PrecalculusCourse.FrameworkVersion, true),
        new(LlmCourse.Id, LlmCourse.Title, LlmCourse.LanguageId, LlmCourse.LanguageVersion, LlmCourse.FrameworkVersion, true),
        new(GitCliCourse.Id, GitCliCourse.Title, GitCliCourse.LanguageId, GitCliCourse.LanguageVersion, GitCliCourse.FrameworkVersion, true),
        new(WebBasicsCourse.Id, WebBasicsCourse.Title, WebBasicsCourse.LanguageId, WebBasicsCourse.LanguageVersion, WebBasicsCourse.FrameworkVersion, true),
        new(TailwindCourse.Id, TailwindCourse.Title, TailwindCourse.LanguageId, TailwindCourse.LanguageVersion, TailwindCourse.FrameworkVersion, true),
        new(SQLiteCourse.Id, SQLiteCourse.Title, SQLiteCourse.LanguageId, SQLiteCourse.LanguageVersion, SQLiteCourse.FrameworkVersion, true),
        new(LinuxCliCourse.Id, LinuxCliCourse.Title, LinuxCliCourse.LanguageId, LinuxCliCourse.LanguageVersion, LinuxCliCourse.FrameworkVersion, true),
        new(KubernetesCourse.Id, KubernetesCourse.Title, KubernetesCourse.LanguageId, KubernetesCourse.LanguageVersion, KubernetesCourse.FrameworkVersion, true),
        new(ReactCourse.Id, ReactCourse.Title, ReactCourse.LanguageId, ReactCourse.LanguageVersion, ReactCourse.FrameworkVersion, true),
        new(ReactLiteCourse.Id, ReactLiteCourse.Title, ReactLiteCourse.LanguageId, ReactLiteCourse.LanguageVersion, ReactLiteCourse.FrameworkVersion, true),
        new(CSharpCourse.Id, CSharpCourse.Title, CSharpCourse.LanguageId, CSharpCourse.LanguageVersion, CSharpCourse.FrameworkVersion, true),
        new(PythonCourse.Id, PythonCourse.Title, PythonCourse.LanguageId, PythonCourse.LanguageVersion, PythonCourse.FrameworkVersion, true),
        new(RustCourse.Id, RustCourse.Title, RustCourse.LanguageId, RustCourse.LanguageVersion, RustCourse.FrameworkVersion, true),
        new(ClaudeCourse.Id, ClaudeCourse.Title, ClaudeCourse.LanguageId, ClaudeCourse.LanguageVersion, ClaudeCourse.FrameworkVersion, true),
        new("sql-server", "SQL Server", "sql-server", "Coming Soon", "T-SQL · SQL Server · SSMS", false),
        new("mysql", "MySQL", "mysql", "Coming Soon", "MySQL SQL · MySQL Server · Workbench", false),
        new("postgresql", "PostgreSQL", "postgresql", "Coming Soon", "PostgreSQL SQL · PostgreSQL · pgAdmin", false),
        new("networking-fundamentals", "Networking Fundamentals", "networking", "Coming Soon", "Packets · IP · routing · ports", false),
        new("dns", "DNS", "dns", "Coming Soon", "Names · records · resolvers · caching", false),
        new("http-apis", "HTTP & APIs", "http", "Coming Soon", "Methods · status · headers · contracts", false),
        new("https-tls", "HTTPS & TLS", "security", "Coming Soon", "Certificates · encryption · trust", false),
        new("distributed-systems", "Distributed Systems", "distributed", "Coming Soon", "Latency · failure · retries · idempotency", false),
        new("digital-electronics", "Digital Electronics", "electrical-engineering", "Coming Soon", "Logic families · timing · state · interfaces", false),
        new("analog-electronics", "Analog Electronics", "electrical-engineering", "Coming Soon", "Biasing · amplifiers · filters · feedback", false),
        new("ac-circuit-analysis", "AC Circuit Analysis", "electrical-engineering", "Coming Soon", "Phasors · impedance · resonance · power", false),
        new("embedded-systems", "Embedded Systems", "electrical-engineering", "Coming Soon", "Firmware · peripherals · timing · integration", false),
        new("microcontrollers", "Microcontrollers", "electrical-engineering", "Coming Soon", "GPIO · timers · ADC · buses", false),
        new("pcb-design", "PCB Design", "electrical-engineering", "Coming Soon", "Schematic capture · layout · fabrication", false),
        new("signals-systems", "Signals & Systems", "electrical-engineering", "Coming Soon", "Signals · systems · transforms · response", false),
        new("control-systems", "Control Systems", "electrical-engineering", "Coming Soon", "Feedback · stability · controllers", false),
        new("electromagnetics", "Electromagnetics", "electrical-engineering", "Coming Soon", "Fields · waves · transmission", false),
        new("power-electronics", "Power Electronics", "electrical-engineering", "Coming Soon", "Converters · switching · magnetics", false)
    ];

    private static Course BuildCourse(string id, string title, string languageId, string languageVersion, string frameworkVersion, string reviewed, Lesson[] lessons) => new(id, title, languageId, languageVersion, frameworkVersion, reviewed, BuildModules(EnsureComplete(id, lessons)));

    // Lesson content is spread across partial-class files. If a lesson ever reads a value that has
    // not been set up yet it silently gets null, so fail at startup instead of serving lessons the
    // frontend cannot render.
    private static Lesson[] EnsureComplete(string courseId, Lesson[] lessons)
    {
        var incomplete = lessons.Where(lesson => lesson.Version is null || lesson.Exercise is null).Select(lesson => lesson.Slug).ToArray();
        if (incomplete.Length > 0)
            throw new InvalidOperationException($"Course '{courseId}' has lessons without a version stamp or exercise: {string.Join(", ", incomplete)}.");
        return lessons;
    }

    private static IReadOnlyList<Module> BuildModules(Lesson[] lessons)
    {
        var modules = new List<Module>();
        var current = new List<Lesson>();
        foreach (var lesson in lessons.OrderBy(lesson => lesson.Order))
        {
            if (current.Count > 0 && !string.Equals(current[0].Module, lesson.Module, StringComparison.Ordinal))
            {
                modules.Add(ToModule(current, modules.Count));
                current = [];
            }
            current.Add(lesson);
        }
        if (current.Count > 0) modules.Add(ToModule(current, modules.Count));
        return modules;
    }

    private static Module ToModule(IReadOnlyList<Lesson> lessons, int index)
    {
        var title = lessons[0].Module;
        var level = title.Contains("foundations", StringComparison.OrdinalIgnoreCase) ? "Beginner" : title.Contains("staff", StringComparison.OrdinalIgnoreCase) ? "Staff" : "Professional";
        return new Module($"{title.ToLowerInvariant().Replace(' ', '-')}-{index + 1}", title, level, lessons.Select(lesson => new LessonSummary(lesson.Slug, lesson.Title, lesson.Order)).ToArray());
    }
}
