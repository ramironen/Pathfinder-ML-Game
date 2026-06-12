#!/usr/bin/env python3
"""Export trained PyTorch weights to JSON for Unity PathfinderMlPredictor."""

import json
from pathlib import Path

import torch

from model import HIDDEN1, HIDDEN2, INPUT_SIZE, build_model

ROOT = Path(__file__).resolve().parent.parent
MODEL_DIR = Path(__file__).resolve().parent / "models"
OUT_PATH = ROOT / "Assets" / "StreamingAssets" / "ML" / "model.json"


def flatten_weight(linear_layer) -> list[float]:
    return linear_layer.weight.detach().cpu().flatten().tolist()


def main() -> None:
    ckpt_path = MODEL_DIR / "pathfinder.pt"
    if not ckpt_path.exists():
        raise FileNotFoundError(f"Run ML/train.py first. Missing {ckpt_path}")

    ckpt = torch.load(ckpt_path, weights_only=False)
    model = build_model()
    model.load_state_dict(ckpt["model_state_dict"])

    linear_layers = [m for m in model if isinstance(m, torch.nn.Linear)]
    w1, w2, w3 = linear_layers

    payload = {
        "input_size": INPUT_SIZE,
        "hidden1": HIDDEN1,
        "hidden2": HIDDEN2,
        "scaler_mean": ckpt["scaler_mean"],
        "scaler_scale": ckpt["scaler_scale"],
        "w1": flatten_weight(w1),
        "b1": w1.bias.detach().cpu().tolist(),
        "w2": flatten_weight(w2),
        "b2": w2.bias.detach().cpu().tolist(),
        "w3": flatten_weight(w3),
        "b3": w3.bias.detach().cpu().tolist(),
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(payload))
    print(f"Exported Unity model: {OUT_PATH}")


if __name__ == "__main__":
    main()
