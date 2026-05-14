# Scoring Tool

This is a local scoring tool for scoring the currently displayed photo in
AvaloniaPictureViewer.

The scorer uses `simple-aesthetics-predictor`, a CLIP-based aesthetic scoring
model. The model predicts a score where higher values mean the image is more
likely to be preferred by people on average.

## Setup

```bash
uv sync
```

The first scoring run downloads the model into the Hugging Face cache. The
model weights are intentionally not committed to this repository.

## Usage

```bash
uv run score-photo "../../image/example.jpg"
```

JSON output:

```json
{
  "input": "/absolute/path/to/photo.jpg",
  "model": "shunk031/aesthetics-predictor-v1-vit-large-patch14",
  "count": 1,
  "error_count": 0,
  "results": [
    {
      "file_name": "photo.jpg",
      "image": "/absolute/path/to/photo.jpg",
      "model": "shunk031/aesthetics-predictor-v1-vit-large-patch14",
      "score": 6.42,
      "device": "mps"
    }
  ],
  "errors": []
}
```

Score every supported image under a directory:

```bash
uv run score-photo --output generated/photo-scores.json "../../image"
```

Use the local cache only:

```bash
uv run score-photo --local-files-only "../../image/example.jpg"
```

Force CPU:

```bash
uv run score-photo --device cpu "../../image/example.jpg"
```

## Worker mode

For GUI integration, keep the scorer process alive so the model is loaded only
once:

```bash
uv run score-photo --worker --local-files-only
```

The worker reads JSON Lines from stdin and writes JSON Lines to stdout. It emits
a ready message after the model has loaded:

```json
{"type":"ready","ok":true,"model":"shunk031/aesthetics-predictor-v1-vit-large-patch14","device":"mps"}
```

Score one image:

```json
{"id":"current-photo","type":"score","image":"/absolute/path/to/photo.jpg"}
```

Successful response:

```json
{"ok":true,"id":"current-photo","file_name":"photo.jpg","image":"/absolute/path/to/photo.jpg","model":"shunk031/aesthetics-predictor-v1-vit-large-patch14","score":6.42,"device":"mps"}
```

Error response:

```json
{"ok":false,"id":"current-photo","error":"Image file not found: /absolute/path/to/photo.jpg"}
```

Shutdown request:

```json
{"type":"shutdown"}
```

## Notes

- The score is an estimate of broad visual preference, not an objective
  measure of artistic value.
- Scores can reflect model and dataset bias.
- The current implementation is a Python sidecar candidate. If the result is
  useful, the next step is wiring this CLI from the Avalonia app, then deciding
  whether to keep the sidecar or port the model path to ONNX Runtime.
