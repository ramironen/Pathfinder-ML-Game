#!/usr/bin/env python3
"""Train Pathfinder NN on Data/*_paths.csv (16 players train, 4 test)."""

import json
from pathlib import Path

import numpy as np
import torch
from sklearn.metrics import accuracy_score, roc_auc_score
from sklearn.preprocessing import StandardScaler
from torch import nn
from torch.utils.data import DataLoader, TensorDataset

from features import FEATURE_COLUMNS, TEST_PLAYERS, load_dataset
from model import build_model

ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = ROOT / "Data"
MODEL_DIR = Path(__file__).resolve().parent / "models"

EPOCHS = 100
BATCH_SIZE = 32
LR = 0.001
PATIENCE = 12


def main() -> None:
    MODEL_DIR.mkdir(parents=True, exist_ok=True)

    df = load_dataset(DATA_DIR)
    X = df[FEATURE_COLUMNS].astype(np.float32).values
    y = df["Success"].astype(np.float32).values
    players = df["player"].values

    train_mask = np.array([p not in TEST_PLAYERS for p in players])
    test_mask = ~train_mask

    scaler = StandardScaler()
    X_train = scaler.fit_transform(X[train_mask])
    X_test = scaler.transform(X[test_mask])
    y_train = y[train_mask]
    y_test = y[test_mask]

    train_ds = TensorDataset(
        torch.tensor(X_train), torch.tensor(y_train).unsqueeze(1)
    )
    loader = DataLoader(train_ds, batch_size=BATCH_SIZE, shuffle=True)

    model = build_model()
    optimizer = torch.optim.Adam(model.parameters(), lr=LR)
    loss_fn = nn.BCELoss()

    best_val = float("inf")
    best_state = None
    wait = 0
    n = len(X_train)
    val_n = max(1, int(n * 0.15))
    perm = np.random.default_rng(42).permutation(n)
    val_idx, tr_idx = perm[:val_n], perm[val_n:]
    X_val, y_val = X_train[val_idx], y_train[val_idx]

    epoch = 0
    for epoch in range(EPOCHS):
        model.train()
        for xb, yb in loader:
            optimizer.zero_grad()
            loss = loss_fn(model(xb), yb)
            loss.backward()
            optimizer.step()

        model.eval()
        with torch.no_grad():
            val_loss = loss_fn(
                model(torch.tensor(X_val)), torch.tensor(y_val).unsqueeze(1)
            ).item()
        if val_loss < best_val:
            best_val = val_loss
            best_state = {k: v.clone() for k, v in model.state_dict().items()}
            wait = 0
        else:
            wait += 1
            if wait >= PATIENCE:
                break

    if best_state:
        model.load_state_dict(best_state)

    model.eval()
    with torch.no_grad():
        y_prob = model(torch.tensor(X_test)).numpy().ravel()
    y_pred = (y_prob >= 0.5).astype(int)
    acc = accuracy_score(y_test, y_pred)
    auc = roc_auc_score(y_test, y_prob) if len(np.unique(y_test)) > 1 else float("nan")

    torch.save(
        {
            "model_state_dict": model.state_dict(),
            "scaler_mean": scaler.mean_.tolist(),
            "scaler_scale": scaler.scale_.tolist(),
            "feature_columns": FEATURE_COLUMNS,
        },
        MODEL_DIR / "pathfinder.pt",
    )

    metrics = {
        "train_rows": int(train_mask.sum()),
        "test_rows": int(test_mask.sum()),
        "test_players": sorted(TEST_PLAYERS),
        "test_accuracy": round(float(acc), 4),
        "test_auc": round(float(auc), 4) if auc == auc else None,
        "epochs_ran": epoch + 1,
    }
    (MODEL_DIR / "metrics.json").write_text(json.dumps(metrics, indent=2))

    print("\n=== Training complete ===")
    for k, v in metrics.items():
        print(f"  {k}: {v}")
    print(f"  saved: {MODEL_DIR / 'pathfinder.pt'}")


if __name__ == "__main__":
    main()
