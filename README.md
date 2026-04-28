# Pathfinder

A cognitive training game built in Unity for exercising visual-spatial memory and sequential recall.

## Overview

Pathfinder challenges players to memorize and reproduce paths on a 7×6 grid. The game displays a colored path briefly, then asks the player to trace it from memory.

## How to Play

1. **Register** - Enter your name, age, and gender (or skip for guest mode)
2. **Select Duration** - Choose session length (1, 2, 5, or 10 minutes)
3. **Watch** - Observe the path as it appears segment by segment
4. **Memorize** - Remember the path layout (red = tail, blue = body, green = head)
5. **Trace** - Navigate to the tail and recreate the path using arrow keys
6. **Submit** - Press Space to check your answer

## Controls

| Key | Action |
|-----|--------|
| Arrow Keys | Move on grid |
| Space | Start path / Submit answer |

## Features

- Configurable difficulty (path length, turns, display time)
- Multiple retry chances per path
- Session statistics tracking
- CSV export for data analysis

## CSV Output

Session data is saved to `pathfinder_sessions.csv` with:
- Player info (name, age, gender)
- Game parameters (path length, turns, display time)
- Performance metrics (success, fail, difficulty score, performance grade)

## Requirements

- Unity 2021.3 or later

## License

This project was created for the Workshop in Machine Learning and Computer Games course at The Open University.
