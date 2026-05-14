from __future__ import annotations

import argparse
import json
import sys
from collections.abc import TextIO
from pathlib import Path
from typing import Any

import torch
from aesthetics_predictor import AestheticsPredictorV1
from PIL import Image, ImageOps
from transformers import CLIPProcessor


DEFAULT_MODEL_ID = "shunk031/aesthetics-predictor-v1-vit-large-patch14"
SUPPORTED_IMAGE_EXTENSIONS = {
    ".bmp",
    ".gif",
    ".jpeg",
    ".jpg",
    ".png",
    ".tif",
    ".tiff",
    ".webp",
}


def _resolve_device(requested_device: str) -> torch.device:
    if requested_device != "auto":
        return torch.device(requested_device)

    if torch.cuda.is_available():
        return torch.device("cuda")

    if torch.backends.mps.is_available():
        return torch.device("mps")

    return torch.device("cpu")


def _iter_image_paths(path: Path, recursive: bool) -> list[Path]:
    path = path.expanduser().resolve()
    if path.is_file():
        if path.suffix.lower() not in SUPPORTED_IMAGE_EXTENSIONS:
            raise ValueError(f"Unsupported image extension: {path}")
        return [path]

    if not path.is_dir():
        raise FileNotFoundError(f"Input path not found: {path}")

    pattern = "**/*" if recursive else "*"
    return sorted(
        candidate
        for candidate in path.glob(pattern)
        if candidate.is_file()
        and candidate.suffix.lower() in SUPPORTED_IMAGE_EXTENSIONS
    )


class AestheticScorer:
    def __init__(
        self,
        *,
        model_id: str = DEFAULT_MODEL_ID,
        requested_device: str = "auto",
        local_files_only: bool = False,
    ) -> None:
        self.model_id = model_id
        self.device = _resolve_device(requested_device)
        self.processor = CLIPProcessor.from_pretrained(
            model_id,
            local_files_only=local_files_only,
        )
        self.model = AestheticsPredictorV1.from_pretrained(
            model_id,
            local_files_only=local_files_only,
        )
        self.model.eval()
        self.model.to(self.device)

    def score(self, image_path: Path) -> dict[str, Any]:
        image_path = image_path.expanduser().resolve()
        if not image_path.is_file():
            raise FileNotFoundError(f"Image file not found: {image_path}")

        with Image.open(image_path) as raw_image:
            image = ImageOps.exif_transpose(raw_image).convert("RGB")

        inputs = self.processor(images=image, return_tensors="pt")
        inputs = {name: value.to(self.device) for name, value in inputs.items()}

        with torch.inference_mode():
            outputs = self.model(**inputs)
            score = float(outputs.logits.squeeze().detach().cpu().item())

        return {
            "file_name": image_path.name,
            "image": str(image_path),
            "model": self.model_id,
            "score": round(score, 4),
            "device": str(self.device),
        }


def _write_json_line(stream: TextIO, payload: dict[str, Any]) -> None:
    print(json.dumps(payload, ensure_ascii=False), file=stream, flush=True)


def _worker_response(
    request: dict[str, Any],
    *,
    ok: bool,
    payload: dict[str, Any] | None = None,
    error: str | None = None,
) -> dict[str, Any]:
    response: dict[str, Any] = {"ok": ok}
    if "id" in request:
        response["id"] = request["id"]
    if payload is not None:
        response.update(payload)
    if error is not None:
        response["error"] = error
    return response


def run_worker(
    *,
    model_id: str = DEFAULT_MODEL_ID,
    requested_device: str = "auto",
    local_files_only: bool = False,
    input_stream: TextIO = sys.stdin,
    output_stream: TextIO = sys.stdout,
) -> int:
    scorer = AestheticScorer(
        model_id=model_id,
        requested_device=requested_device,
        local_files_only=local_files_only,
    )
    _write_json_line(
        output_stream,
        {
            "type": "ready",
            "ok": True,
            "model": model_id,
            "device": str(scorer.device),
        },
    )

    for line in input_stream:
        line = line.strip()
        if not line:
            continue

        try:
            request = json.loads(line)
        except json.JSONDecodeError as exc:
            _write_json_line(
                output_stream,
                {"ok": False, "error": f"Invalid JSON: {exc.msg}"},
            )
            continue

        if not isinstance(request, dict):
            _write_json_line(
                output_stream,
                {"ok": False, "error": "Request must be a JSON object."},
            )
            continue

        request_type = request.get("type", "score")
        if request_type == "shutdown":
            _write_json_line(
                output_stream,
                _worker_response(request, ok=True, payload={"type": "shutdown"}),
            )
            return 0

        if request_type != "score":
            _write_json_line(
                output_stream,
                _worker_response(
                    request,
                    ok=False,
                    error=f"Unsupported request type: {request_type}",
                ),
            )
            continue

        image = request.get("image") or request.get("input")
        if not isinstance(image, str) or not image:
            _write_json_line(
                output_stream,
                _worker_response(
                    request,
                    ok=False,
                    error="Score requests require a non-empty 'image' path.",
                ),
            )
            continue

        try:
            result = scorer.score(Path(image))
        except Exception as exc:
            _write_json_line(
                output_stream,
                _worker_response(request, ok=False, error=str(exc)),
            )
            continue

        _write_json_line(
            output_stream,
            _worker_response(request, ok=True, payload=result),
        )

    return 0


def score_image(
    image_path: Path,
    *,
    model_id: str = DEFAULT_MODEL_ID,
    requested_device: str = "auto",
    local_files_only: bool = False,
) -> dict[str, Any]:
    scorer = AestheticScorer(
        model_id=model_id,
        requested_device=requested_device,
        local_files_only=local_files_only,
    )
    return scorer.score(image_path)


def score_paths(
    input_path: Path,
    *,
    model_id: str = DEFAULT_MODEL_ID,
    requested_device: str = "auto",
    local_files_only: bool = False,
    recursive: bool = True,
) -> dict[str, Any]:
    image_paths = _iter_image_paths(input_path, recursive=recursive)
    scorer = AestheticScorer(
        model_id=model_id,
        requested_device=requested_device,
        local_files_only=local_files_only,
    )

    results: list[dict[str, Any]] = []
    errors: list[dict[str, str]] = []
    for index, image_path in enumerate(image_paths, start=1):
        print(f"[{index}/{len(image_paths)}] {image_path}", file=sys.stderr)
        try:
            results.append(scorer.score(image_path))
        except Exception as exc:
            errors.append({"image": str(image_path), "error": str(exc)})

    return {
        "input": str(input_path.expanduser().resolve()),
        "model": model_id,
        "count": len(results),
        "error_count": len(errors),
        "results": results,
        "errors": errors,
    }


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Score a photo with a local aesthetic assessment model.",
    )
    parser.add_argument(
        "input",
        nargs="?",
        type=Path,
        help="Path to a photo or directory.",
    )
    parser.add_argument(
        "--model",
        default=DEFAULT_MODEL_ID,
        help=f"Hugging Face model id. Default: {DEFAULT_MODEL_ID}",
    )
    parser.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="auto",
        help="Inference device. Default: auto.",
    )
    parser.add_argument(
        "--local-files-only",
        action="store_true",
        help="Use only models already present in the local Hugging Face cache.",
    )
    parser.add_argument(
        "--pretty",
        action="store_true",
        help="Pretty-print JSON output.",
    )
    parser.add_argument(
        "--no-recursive",
        action="store_true",
        help="When input is a directory, scan only the immediate children.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Write JSON output to this file instead of stdout.",
    )
    parser.add_argument(
        "--worker",
        action="store_true",
        help="Keep the model loaded and process JSON Lines requests from stdin.",
    )
    return parser


def main() -> None:
    parser = _build_parser()
    args = parser.parse_args()

    if args.worker:
        try:
            raise SystemExit(
                run_worker(
                    model_id=args.model,
                    requested_device=args.device,
                    local_files_only=args.local_files_only,
                )
            )
        except Exception as exc:
            print(
                json.dumps({"ok": False, "error": str(exc)}, ensure_ascii=False),
                file=sys.stderr,
            )
            raise SystemExit(1) from exc

    if args.input is None:
        parser.error("the following arguments are required: input")

    try:
        result = score_paths(
            args.input,
            model_id=args.model,
            requested_device=args.device,
            local_files_only=args.local_files_only,
            recursive=not args.no_recursive,
        )
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False), file=sys.stderr)
        raise SystemExit(1) from exc

    indent = 2 if args.pretty else None
    output = json.dumps(result, ensure_ascii=False, indent=indent)
    if args.output is None:
        print(output)
    else:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output + "\n", encoding="utf-8")
