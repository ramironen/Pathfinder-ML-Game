# Pathfinder

A 2D cognitive training game built in Unity with optional **ML adaptive difficulty**. Players memorize colored paths on a grid and trace them from memory. The game logs per-path and per-session CSV data for analysis and offline model training.

**Repository:** [github.com/ramironen/Pathfinder-ML-Game](https://github.com/ramironen/Pathfinder-ML-Game)

**Full documentation:** [ProjectDescription.md](ProjectDescription.md) (PDF: generate with `pandoc ProjectDescription.md -o PathFinder.pdf --pdf-engine=pdflatex`)

## Quick start

1. Open this folder in **Unity Hub** (Unity **2022.3.62f3**).
2. Play **RegistrationScene** -> **SnakeScene**.
3. Register (or skip as Guest), choose session length and **ML On / Off**, then press Start.

## How to play

1. **Memorize** - Paths appear one segment at a time. Remember only the **colored** snakes:
   - **Red** = tail (start)
   - **Blue / Cyan / Magenta** = body (one color per real snake)
   - **Green** = head (end)
   - **Gray** = dummy (ignore)
2. **Wait** - Paths disappear. Higher stages add a **recall delay** (blank grid before tracing).
3. **Trace** - Move with arrow keys (active cell has a **yellow** highlight). Space to start drawing and to submit.
4. **Progress** - Six stages (Benchmark -> Expert). Normal mode: advance after 3 paths with 2 successes. **ML On:** stage is chosen automatically from predicted success (not shown on screen).

| Stage | Name | Grid |
|:-----:|------|:----:|
| 0 | Benchmark | 5x5 |
| 1 | Easy | 5x5 |
| 2 | Medium | 6x6 |
| 3 | Hard | 7x7 |
| 4 | Very Hard | 7x7 |
| 5 | Expert | 8x8 |

Benchmark and Easy share the same grid size; Benchmark is a fixed reference task for cross-session comparison.

## ML pipeline

Model: **17 inputs -> 16 -> 8 -> 1** (PyTorch), exported to JSON for C# inference.

```bash
pip install -r ML/requirements.txt
python ML/train.py                 # needs Data/*_paths.csv
python ML/export_for_unity.py      # writes Assets/StreamingAssets/ML/model.json
```

When **ML On** is selected at registration, Unity loads `model.json` and adjusts stage each path:

- P(success) **> 0.75** -> harder stage
- P(success) **< 0.45** -> easier stage
- otherwise -> keep stage

## Data export

Per player under `Data/` (project folder in Editor; beside executable in builds):

| File | Content |
|------|---------|
| `{Name}_paths.csv` | One row per path: 11 difficulty parameters, timing, success, `MlAdaptive`, `PredictedPSuccess` |
| `{Name}_sessions.csv` | One row per session: stats, max stage, benchmark score, `MlAdaptive` |

## Project layout

| Path | Purpose |
|------|---------|
| `Assets/` | Unity scenes, gameplay scripts, `StreamingAssets/ML/model.json` |
| `ML/` | Train / export scripts (`model.py`, `train.py`, `export_for_unity.py`, ...) |
| `docs/pic/` | Screenshots for documentation |
| `ProjectDescription.md` | Course presentation document |

## Requirements

- Unity **2022.3.62f3**
- Python 3.10+ with PyTorch (training / export only)

## License

Created for the Workshop in Machine Learning and Computer Games course at The Open University.
