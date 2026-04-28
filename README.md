# Pathfinder

A cognitive training game built in Unity for exercising visual-spatial memory, sequential recall, and selective attention.

## Overview

Pathfinder challenges players to memorize and reproduce paths on a configurable grid. The game displays one or more colored paths briefly, then asks the player to trace them from memory in order.

## How to Play

1. **Register** - Enter your name, age, and gender (or skip for guest mode)
2. **Select Duration** - Choose session length (1, 2, 5, or 10 minutes)
3. **Watch** - Observe the path(s) as they appear segment by segment
4. **Memorize** - Remember the path layout:
   - **Red** = Tail (start here)
   - **Blue/Cyan/Magenta** = Body (different colors for multiple snakes)
   - **Green** = Head (end here)
   - **Gray** = Dummy snakes (ignore these!)
5. **Trace** - Navigate to the tail and recreate each path using arrow keys
6. **Submit** - Press Space to check your answer

## Controls

| Key | Action |
|-----|--------|
| Arrow Keys | Move on grid |
| Space | Start path / Submit answer |

## Features

- **12 Configurable Parameters** for difficulty tuning:
  - Grid size (dynamic square grid)
  - Path length and turns
  - Display time and segment delay
  - Color flip mode (swap red/green meaning)
  - Multiple snakes (1-3 real snakes to trace in order)
  - Dummy snakes (0-2 distractors to ignore)
  - Delay before recall (memory retention test)
  - Retry chances per path set

- **Visual Indicators**:
  - Color direction indicator (shows tail→head colors)
  - Real-time score display
  - Session timer

- **Data Export**:
  - Automatic CSV export with all parameters and performance metrics
  - Difficulty score calculation
  - Performance grade normalization

## CSV Output

Session data is saved to `pathfinder_sessions.csv` with:
- Player info (name, age, gender)
- All 12 game parameters
- Performance metrics (success, fail, difficulty score, performance grade)

## Cognitive Abilities Tested

- Visual-Spatial Memory
- Sequential Memory
- Selective Attention (ignoring dummy snakes)
- Cognitive Flexibility (color flip mode)
- Working Memory
- Memory Retention (with delay)

## Requirements

- Unity 2021.3 or later

## License

This project was created for the Workshop in Machine Learning and Computer Games course at The Open University.
