using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhotoScore.DotNet;

internal sealed class WorkerHost
{
    private readonly string _modelPath;
    private readonly string _requestedDevice;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkerHost(
        string modelPath,
        string requestedDevice,
        TextReader input,
        TextWriter output,
        JsonSerializerOptions jsonOptions)
    {
        _modelPath = modelPath;
        _requestedDevice = requestedDevice;
        _input = input;
        _output = output;
        _jsonOptions = jsonOptions;
    }

    public int Run()
    {
        using var scorer = new AestheticScorer(_modelPath, _requestedDevice);
        WriteJson(new
        {
            type = "ready",
            ok = true,
            model = scorer.ModelPath,
            device = scorer.Device,
        });

        string? line;
        while ((line = _input.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            JsonObject request;
            try
            {
                var parsed = JsonNode.Parse(line);
                if (parsed is not JsonObject jsonObject)
                {
                    WriteJson(new { ok = false, error = "Request must be a JSON object." });
                    continue;
                }

                request = jsonObject;
            }
            catch (JsonException ex)
            {
                WriteJson(new { ok = false, error = $"Invalid JSON: {ex.Message}" });
                continue;
            }

            var requestType = request["type"]?.GetValue<string>() ?? "score";
            if (requestType == "shutdown")
            {
                WriteResponse(request, ok: true, payload: new Dictionary<string, object?>
                {
                    ["type"] = "shutdown",
                });
                return 0;
            }

            if (requestType != "score")
            {
                WriteResponse(request, ok: false, error: $"Unsupported request type: {requestType}");
                continue;
            }

            var image = request["image"]?.GetValue<string>() ?? request["input"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(image))
            {
                WriteResponse(request, ok: false, error: "Score requests require a non-empty 'image' path.");
                continue;
            }

            try
            {
                var result = scorer.Score(image);
                WriteResponse(request, ok: true, payload: new Dictionary<string, object?>
                {
                    ["file_name"] = result.FileName,
                    ["image"] = result.Image,
                    ["model"] = result.Model,
                    ["score"] = result.Score,
                    ["device"] = result.Device,
                });
            }
            catch (Exception ex)
            {
                WriteResponse(request, ok: false, error: ex.Message);
            }
        }

        return 0;
    }

    private void WriteResponse(
        JsonObject request,
        bool ok,
        IReadOnlyDictionary<string, object?>? payload = null,
        string? error = null)
    {
        var response = new Dictionary<string, object?>
        {
            ["ok"] = ok,
        };

        if (request.TryGetPropertyValue("id", out var idNode))
        {
            response["id"] = idNode?.GetValue<object>();
        }

        if (payload is not null)
        {
            foreach (var pair in payload)
            {
                response[pair.Key] = pair.Value;
            }
        }

        if (error is not null)
        {
            response["error"] = error;
        }

        WriteJson(response);
    }

    private void WriteJson<T>(T payload)
    {
        _output.WriteLine(JsonSerializer.Serialize(payload, _jsonOptions));
        _output.Flush();
    }
}
