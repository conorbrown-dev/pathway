using System.Text.Json.Serialization;

namespace Pathway.Domain;

// Answer data stays server-side: lesson responses are public, so CorrectAnswer, the numeric
// target/tolerance and the worked solution are never serialized. Validators read them from the
// in-memory record, and the worked solution is returned in ValidationResult only after a pass.
public record Exercise(ExerciseKind Kind, string Title, string Prompt, string[] Requirements, string? StarterCode, [property: JsonIgnore] string? CorrectAnswer, Choice[] Choices, string Hint, string[] Tests, [property: JsonIgnore] double? ExpectedNumeric = null, [property: JsonIgnore] double? Tolerance = null, string? Unit = null, IReadOnlyDictionary<string, double>? UnitConversions = null, [property: JsonIgnore] string? WorkedSolution = null, CircuitDefinition? Circuit = null);
public record Choice(string Id, string Text);
public enum ExerciseKind { MultipleChoice, Code, Presentation, Numeric, Circuit }
