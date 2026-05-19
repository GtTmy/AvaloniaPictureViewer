using System.Text.Encodings.Web;
using System.Text.Json;
using PhotoScore.DotNet;

var jsonOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
};

try
{
    var options = CommandLineOptions.Parse(args);
    if (options.Help)
    {
        CommandLineOptions.WriteHelp(Console.Out);
        return 0;
    }

    if (options.Worker)
    {
        try
        {
            return new WorkerHost(
                options.Model,
                options.Device,
                Console.In,
                Console.Out,
                jsonOptions).Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }, jsonOptions));
            return 1;
        }
    }

    if (options.Input is null)
    {
        Console.Error.WriteLine("error: the following arguments are required: input");
        return 2;
    }

    var service = new ScoringService(options.Model, options.Device);
    var result = service.ScorePaths(options.Input, options.Recursive);
    var outputOptions = new JsonSerializerOptions(jsonOptions)
    {
        WriteIndented = options.Pretty,
    };
    var json = JsonSerializer.Serialize(result, outputOptions);

    if (options.Output is null)
    {
        Console.WriteLine(json);
    }
    else
    {
        var outputPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.Output));
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, json + Environment.NewLine);
    }

    return result.ErrorCount == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }, jsonOptions));
    return 1;
}
