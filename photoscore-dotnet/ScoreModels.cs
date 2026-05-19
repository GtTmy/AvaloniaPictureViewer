using System.Text.Json.Serialization;

namespace PhotoScore.DotNet;

public sealed record ScoreResult(
    [property: JsonPropertyName("file_name")] string FileName,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("device")] string Device);

public sealed record ScoreError(
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("error")] string Error);

public sealed record ScoreBatchResult(
    [property: JsonPropertyName("input")] string Input,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("error_count")] int ErrorCount,
    [property: JsonPropertyName("results")] IReadOnlyList<ScoreResult> Results,
    [property: JsonPropertyName("errors")] IReadOnlyList<ScoreError> Errors);
