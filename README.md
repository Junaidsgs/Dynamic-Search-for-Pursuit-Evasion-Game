# Dynamic Search for Pursuit-Evasion Game

A framework to analyze traditional search algorithms and explore single vs. multi-agent complexity in a dynamic Pursuit-Evasion Game.

---

## Project Goal

To design an experimental framework that evaluates **traditional search algorithms** (Dynamic A*, BFS, Minimax with Alpha-Beta pruning) in procedural mazes, analyzing how search efficiency and capture performance scale from single-agent to multi-agent pursuit.

---

## Developer

- **Junaid Ahmad** — [`ja4893@rit.edu`](mailto:ja4893@rit.edu)  
  GitHub: [@Junaidsgs](https://github.com/Junaidsgs)

---


## Algorithms

1. **Breadth-First Search (BFS):** Unweighted shortest path baseline.
2. **Dynamic A\* Search:** Heuristic-based dynamic pathfinding (Manhattan / Euclidean) with real-time replanning as the target moves.
3. **Minimax with Alpha-Beta Pruning:** Adversarial game-tree search to intercept the evader before it reaches its goal.
4. **Multi-Agent Coordination:** Scaling from a single pursuer to cooperative multi-pursuer search.

---

## Setup & Running

### Installation
```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

