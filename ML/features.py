"""Feature engineering from path CSV rows (matches Unity predictor)."""

from pathlib import Path

import pandas as pd

CURRENT_FEATURES = [
    "Stage",
    "StageAttempt",
    "GridSize",
    "PathLength",
    "NumberOfTurns",
    "NumberOfSnakes",
    "NumberOfDummySnakes",
    "FlipColors",
    "DisplayTime",
    "SegmentDelay",
    "DelayBeforeRecall",
]

HISTORY_FEATURES = [
    "paths_done",
    "success_rate_so_far",
    "benchmark_success_rate",
    "stage_success_rate_so_far",
    "avg_duration_prev",
    "last_success",
]

FEATURE_COLUMNS = CURRENT_FEATURES + HISTORY_FEATURES

TEST_PLAYERS = {"Tamar", "Rina", "Shira", "Amnon"}


def player_from_filename(path: Path) -> str:
    return path.stem.replace("_paths", "")


def history_from_prev(prev: list[dict], current_stage: int) -> dict:
    if not prev:
        return {
            "paths_done": 0,
            "success_rate_so_far": 0.0,
            "benchmark_success_rate": 0.0,
            "stage_success_rate_so_far": 0.0,
            "avg_duration_prev": 0.0,
            "last_success": 0,
        }
    successes = sum(int(p["Success"]) for p in prev)
    bench = [p for p in prev if int(p["Stage"]) == 0]
    stage_paths = [p for p in prev if int(p["Stage"]) == current_stage]
    durations = [float(p["PathDurationMs"]) for p in prev if float(p["PathDurationMs"]) > 0]
    return {
        "paths_done": len(prev),
        "success_rate_so_far": successes / len(prev),
        "benchmark_success_rate": (
            sum(int(p["Success"]) for p in bench) / len(bench) if bench else 0.0
        ),
        "stage_success_rate_so_far": (
            sum(int(p["Success"]) for p in stage_paths) / len(stage_paths)
            if stage_paths
            else 0.0
        ),
        "avg_duration_prev": sum(durations) / len(durations) if durations else 0.0,
        "last_success": int(prev[-1]["Success"]),
    }


def build_features_for_row(row: dict, prev: list[dict]) -> dict:
    hist = history_from_prev(prev, int(row["Stage"]))
    out = {k: float(row[k]) for k in CURRENT_FEATURES}
    out.update(hist)
    return out


def load_dataset(data_dir: Path) -> pd.DataFrame:
    frames = []
    for csv_path in sorted(data_dir.glob("*_paths.csv")):
        df = pd.read_csv(csv_path)
        df["player"] = player_from_filename(csv_path)
        frames.append(df)
    if not frames:
        raise FileNotFoundError(f"No *_paths.csv in {data_dir}")
    full = pd.concat(frames, ignore_index=True)

    rows = []
    for (_, session_df) in full.groupby(["player", "SessionNumber"], sort=False):
        session_df = session_df.sort_values("PathNumber").reset_index(drop=True)
        prev: list[dict] = []
        for _, row in session_df.iterrows():
            feat = build_features_for_row(row.to_dict(), prev)
            feat["Success"] = int(row["Success"])
            feat["player"] = row["player"]
            rows.append(feat)
            prev.append(row.to_dict())
    return pd.DataFrame(rows)
