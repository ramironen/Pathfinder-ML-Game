#!/usr/bin/env python3
"""Run inference with trained Pathfinder model."""

import argparse
import json
from pathlib import Path

import numpy as np
import torch

from features import FEATURE_COLUMNS
from model import build_model

MODEL_DIR = Path(__file__).resolve().parent / "models"


def load_checkpoint():
    ckpt = torch.load(MODEL_DIR / "pathfinder.pt", weights_only=False)
    model = build_model()
    model.load_state_dict(ckpt["model_state_dict"])
    model.eval()
    mean = np.array(ckpt["scaler_mean"], dtype=np.float32)
    scale = np.array(ckpt["scaler_scale"], dtype=np.float32)
    return model, mean, scale


def predict_vector(model, mean, scale, features: list[float]) -> float:
    x = (np.array(features, dtype=np.float32) - mean) / scale
    with torch.no_grad():
        return float(model(torch.tensor(x).unsqueeze(0)).item())


def main() -> None:
    parser = argparse.ArgumentParser(description="Pathfinder ML inference")
    parser.add_argument(
        "--features-json",
        type=Path,
        help="JSON file with feature keys or a list of 17 floats",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help='Write {"p_success": ...} to this file',
    )
    args = parser.parse_args()

    model, mean, scale = load_checkpoint()

    if args.features_json:
        data = json.loads(args.features_json.read_text())
        if isinstance(data, list):
            vec = [float(v) for v in data]
        else:
            vec = [float(data[k]) for k in FEATURE_COLUMNS]
    else:
        vec = [0.0] * 17
        vec[8] = 0.5

    p = predict_vector(model, mean, scale, vec)
    result = {"p_success": round(p, 4)}
    print(f"P(success) = {p:.4f}")
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result))


if __name__ == "__main__":
    main()
