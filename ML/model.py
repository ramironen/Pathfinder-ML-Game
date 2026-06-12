"""Pathfinder neural network — nn.Sequential 17 → 16 → 8 → 1."""

import torch
from torch import nn

INPUT_SIZE = 17
HIDDEN1 = 16
HIDDEN2 = 8


def build_model() -> nn.Sequential:
    return nn.Sequential(
        nn.Linear(INPUT_SIZE, HIDDEN1),
        nn.ReLU(),
        nn.Linear(HIDDEN1, HIDDEN2),
        nn.ReLU(),
        nn.Linear(HIDDEN2, 1),
        nn.Sigmoid(),
    )
