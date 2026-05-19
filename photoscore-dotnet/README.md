# PhotoScore.DotNet

`PhotoScore.DotNet` is a .NET port of `experimental/scoring-tool`.

It keeps the Python tool's command shape and JSON/JSON Lines worker protocol,
but it runs inference with ONNX Runtime. By default it uses
`models/aesthetic-score.onnx` in this directory. You can also pass another local
ONNX model path with `--model`, or set `PHOTOSCORE_ONNX_MODEL`.

The ONNX model is expected to accept a CLIP-style `pixel_values` tensor with
shape `[1, 3, 224, 224]` and return one scalar aesthetic score.

## Usage

```bash
dotnet run --project photoscore-dotnet -- ./image/example.jpg
```

Pretty JSON:

```bash
dotnet run --project photoscore-dotnet -- --pretty ./image
```

Worker mode:

```bash
dotnet run --project photoscore-dotnet -- --worker
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

- The checked-in model was exported from
  `shunk031/aesthetics-predictor-v1-vit-large-patch14` with `export_onnx.py`.
- ONNX Runtime expects `aesthetic-score.onnx.data` next to
  `aesthetic-score.onnx`; keep both files together.
- `--local-files-only` is accepted for CLI compatibility and is always treated
  as true.
- `--device auto` and `--device cpu` use the CPU execution provider. Other
  devices are rejected until a supported ONNX Runtime provider is configured.
