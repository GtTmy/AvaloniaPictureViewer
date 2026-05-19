# PhotoScore.DotNet

`PhotoScore.DotNet` is a .NET port of `experimental/scoring-tool`.

It keeps the Python tool's command shape and JSON/JSON Lines worker protocol,
but it runs inference with ONNX Runtime. Pass a local ONNX model path with
`--model`, or set `PHOTOSCORE_ONNX_MODEL`.

The ONNX model is expected to accept a CLIP-style `pixel_values` tensor with
shape `[1, 3, 224, 224]` and return one scalar aesthetic score.

## Usage

```bash
dotnet run --project photoscore-dotnet -- --model ./models/aesthetic.onnx ./image/example.jpg
```

Pretty JSON:

```bash
dotnet run --project photoscore-dotnet -- --pretty --model ./models/aesthetic.onnx ./image
```

Worker mode:

```bash
dotnet run --project photoscore-dotnet -- --worker --model ./models/aesthetic.onnx
```

The worker reads JSON Lines from stdin and writes JSON Lines to stdout:

```json
{"id":"current-photo","type":"score","image":"/absolute/path/to/photo.jpg"}
```

Shutdown:

```json
{"type":"shutdown"}
```

## Notes

- The tool does not download Hugging Face models. Convert or export the model to
  ONNX first and keep the weights outside this repository.
- `--local-files-only` is accepted for CLI compatibility and is always treated
  as true.
- `--device auto` and `--device cpu` use the CPU execution provider. Other
  devices are rejected until a supported ONNX Runtime provider is configured.
