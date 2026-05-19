namespace PhotoScore.DotNet;

internal sealed class CommandLineOptions
{
    public string? Input { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public string Device { get; private set; } = "auto";

    public bool Pretty { get; private set; }

    public bool Recursive { get; private set; } = true;

    public string? Output { get; private set; }

    public bool Worker { get; private set; }

    public bool Help { get; private set; }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options.Help = true;
                    break;
                case "--model":
                    options.Model = ReadValue(args, ref index, "--model");
                    break;
                case "--device":
                    options.Device = ReadValue(args, ref index, "--device");
                    break;
                case "--local-files-only":
                    break;
                case "--pretty":
                    options.Pretty = true;
                    break;
                case "--no-recursive":
                    options.Recursive = false;
                    break;
                case "--output":
                    options.Output = ReadValue(args, ref index, "--output");
                    break;
                case "--worker":
                    options.Worker = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new ArgumentException($"Unknown option: {arg}");
                    }

                    if (options.Input is not null)
                    {
                        throw new ArgumentException($"Unexpected argument: {arg}");
                    }

                    options.Input = arg;
                    break;
            }
        }

        return options;
    }

    public static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("Usage: score-photo [options] <input>");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --model <path>          Path to an ONNX aesthetic scoring model.");
        writer.WriteLine("  --device <device>       Inference device: auto or cpu. Default: auto.");
        writer.WriteLine("  --local-files-only      Accepted for Python CLI compatibility.");
        writer.WriteLine("  --pretty                Pretty-print JSON output.");
        writer.WriteLine("  --no-recursive          Scan only immediate children for directory input.");
        writer.WriteLine("  --output <path>         Write JSON output to this file.");
        writer.WriteLine("  --worker                Process JSON Lines requests from stdin.");
        writer.WriteLine("  -h, --help              Show help.");
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }
}
