from __future__ import annotations

import argparse
from pathlib import Path

import torch
from aesthetics_predictor import AestheticsPredictorV1


DEFAULT_MODEL_ID = "shunk031/aesthetics-predictor-v1-vit-large-patch14"


class ExportableAestheticModel(torch.nn.Module):
    def __init__(self, model: torch.nn.Module) -> None:
        super().__init__()
        self.model = model

    def forward(self, pixel_values: torch.Tensor) -> torch.Tensor:
        outputs = self.model(pixel_values=pixel_values)
        return outputs.logits


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export the photo aesthetic scorer to ONNX.",
    )
    parser.add_argument(
        "--model",
        default=DEFAULT_MODEL_ID,
        help=f"Hugging Face model id. Default: {DEFAULT_MODEL_ID}",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("models/aesthetic-score.onnx"),
        help="Output ONNX path relative to photoscore-dotnet.",
    )
    parser.add_argument(
        "--local-files-only",
        action="store_true",
        help="Use only models already present in the local Hugging Face cache.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    output = args.output.expanduser().resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    model = AestheticsPredictorV1.from_pretrained(
        args.model,
        local_files_only=args.local_files_only,
    )
    model.eval()
    model.cpu()

    wrapper = ExportableAestheticModel(model)
    wrapper.eval()
    dummy = torch.randn(1, 3, 224, 224, dtype=torch.float32)

    torch.onnx.export(
        wrapper,
        dummy,
        output,
        input_names=["pixel_values"],
        output_names=["logits"],
        dynamic_axes={
            "pixel_values": {0: "batch"},
            "logits": {0: "batch"},
        },
        opset_version=18,
        external_data=False,
    )

    print(output)


if __name__ == "__main__":
    main()
