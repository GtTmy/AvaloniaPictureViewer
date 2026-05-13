from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

import torch
from aesthetics_predictor import AestheticsPredictorV1
from PIL import Image, ImageOps
from transformers import CLIPProcessor


DEFAULT_MODEL_ID = "shunk031/aesthetics-predictor-v1-vit-large-patch14"


def _resolve_device(requested_device: str) -> torch.device:
    if requested_device != "auto":
        return torch.device(requested_device)

    if torch.cuda.is_available():
        return torch.device("cuda")

    if torch.backends.mps.is_available():
        return torch.device("mps")

    return torch.device("cpu")


def score_image(
    image_path: Path,
    *,
    model_id: str = DEFAULT_MODEL_ID,
    requested_device: str = "auto",
    local_files_only: bool = False,
) -> dict[str, Any]:
    image_path = image_path.expanduser().resolve()
    if not image_path.is_file():
        raise FileNotFoundError(f"Image file not found: {image_path}")

    device = _resolve_device(requested_device)
    processor = CLIPProcessor.from_pretrained(
        model_id,
        local_files_only=local_files_only,
    )
    model = AestheticsPredictorV1.from_pretrained(
        model_id,
        local_files_only=local_files_only,
    )
    model.eval()
    model.to(device)

    with Image.open(image_path) as raw_image:
        image = ImageOps.exif_transpose(raw_image).convert("RGB")

    inputs = processor(images=image, return_tensors="pt")
    inputs = {name: value.to(device) for name, value in inputs.items()}

    with torch.inference_mode():
        outputs = model(**inputs)
        score = float(outputs.logits.squeeze().detach().cpu().item())

    return {
        "image": str(image_path),
        "model": model_id,
        "score": round(score, 4),
        "device": str(device),
    }


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Score a photo with a local aesthetic assessment model.",
    )
    parser.add_argument("image", type=Path, help="Path to the photo to score.")
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
    return parser


def main() -> None:
    parser = _build_parser()
    args = parser.parse_args()

    try:
        result = score_image(
            args.image,
            model_id=args.model,
            requested_device=args.device,
            local_files_only=args.local_files_only,
        )
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False), file=sys.stderr)
        raise SystemExit(1) from exc

    indent = 2 if args.pretty else None
    print(json.dumps(result, ensure_ascii=False, indent=indent))
