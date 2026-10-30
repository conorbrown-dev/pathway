using System.Text.Json.Serialization;

namespace Pathway.Domain;

public record CircuitDefinition(
    string Title,
    string Description,
    string SafetyNote,
    string[] MeterModes,
    CircuitNode[] Nodes,
    CircuitComponent[] Components,
    CircuitMeasurement[] Measurements,
    CircuitTask Task);

public record CircuitNode(string Id, string Label, double X, double Y, bool Ground = false);

public record CircuitComponent(
    string Id,
    string Kind,
    string Label,
    string FromNode,
    string ToNode,
    string? Value = null);

public record CircuitMeasurement(
    string MeterMode,
    string RedNode,
    string BlackNode,
    double? Value,
    string Unit,
    string Display);

public record CircuitTask(
    string Instruction,
    [property: JsonIgnore] string ExpectedMeterMode,
    [property: JsonIgnore] string ExpectedRedNode,
    [property: JsonIgnore] string ExpectedBlackNode,
    Choice[] DiagnosisChoices,
    [property: JsonIgnore] string? CorrectDiagnosis = null);

public record CircuitReading(double? Value, string Unit, string Display);
