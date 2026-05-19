using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PhotoScore.DotNet;

public sealed class AestheticScorer : IDisposable
{
    private const int ImageSize = 224;
    private static readonly float[] Mean = [0.48145466f, 0.4578275f, 0.40821073f];
    private static readonly float[] Std = [0.26862954f, 0.26130258f, 0.27577711f];

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string _outputName;

    public AestheticScorer(string modelPath, string requestedDevice = "auto")
    {
        ModelPath = ResolveModelPath(modelPath);
        Device = ResolveDevice(requestedDevice);

        var options = new SessionOptions();
        _session = new InferenceSession(ModelPath, options);
        _inputName = _session.InputMetadata.ContainsKey("pixel_values")
            ? "pixel_values"
            : _session.InputMetadata.Keys.First();
        _outputName = _session.OutputMetadata.Keys.First();
    }

    public string ModelPath { get; }

    public string Device { get; }

    public ScoreResult Score(string imagePath)
    {
        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(imagePath));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Image file not found: {fullPath}", fullPath);
        }

        var tensor = LoadTensor(fullPath);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor),
        };
        using var outputs = _session.Run(inputs);
        var output = outputs.First(value => value.Name == _outputName);
        var score = ReadScalar(output);

        return new ScoreResult(
            Path.GetFileName(fullPath),
            fullPath,
            ModelPath,
            Math.Round(score, 4, MidpointRounding.AwayFromZero),
            Device);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private static string ResolveModelPath(string modelPath)
    {
        var configuredPath = string.IsNullOrWhiteSpace(modelPath)
            ? Environment.GetEnvironmentVariable("PHOTOSCORE_ONNX_MODEL")
            : modelPath;

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new ArgumentException("ONNX model path is required. Pass --model or set PHOTOSCORE_ONNX_MODEL.");
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"ONNX model file not found: {fullPath}", fullPath);
        }

        return fullPath;
    }

    private static string ResolveDevice(string requestedDevice)
    {
        return requestedDevice switch
        {
            "auto" or "cpu" => "cpu",
            "cuda" => throw new NotSupportedException("CUDA is not configured for this package. Use --device cpu."),
            "mps" => throw new NotSupportedException("MPS is not supported by ONNX Runtime on macOS. Use --device cpu."),
            _ => throw new ArgumentException($"Unsupported device: {requestedDevice}"),
        };
    }

    private static DenseTensor<float> LoadTensor(string imagePath)
    {
        using var image = Image.Load<Rgb24>(imagePath);
        image.Mutate(context => context
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Size = new Size(ImageSize, ImageSize),
                Mode = ResizeMode.Crop,
                Sampler = KnownResamplers.Bicubic,
            }));

        var tensor = new DenseTensor<float>([1, 3, ImageSize, ImageSize]);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < ImageSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < ImageSize; x++)
                {
                    var pixel = row[x];
                    tensor[0, 0, y, x] = ((pixel.R / 255f) - Mean[0]) / Std[0];
                    tensor[0, 1, y, x] = ((pixel.G / 255f) - Mean[1]) / Std[1];
                    tensor[0, 2, y, x] = ((pixel.B / 255f) - Mean[2]) / Std[2];
                }
            }
        });

        return tensor;
    }

    private static double ReadScalar(DisposableNamedOnnxValue output)
    {
        var value = output.Value;
        if (value is Tensor<float> floatTensor)
        {
            return floatTensor.First();
        }

        if (value is Tensor<double> doubleTensor)
        {
            return doubleTensor.First();
        }

        if (value is IReadOnlyList<float> floatList && floatList.Count > 0)
        {
            return floatList[0];
        }

        if (value is IReadOnlyList<double> doubleList && doubleList.Count > 0)
        {
            return doubleList[0];
        }

        throw new InvalidOperationException($"Unsupported model output type: {value.GetType().FullName}");
    }
}
