# Project Plan: Dynamic Search for Pursuit-Evasion Game
**Course:** CSCI-630.01  
**Repo:** `Dynamic-Search-for-Pursuit-Evasion-Game`

---

## Phase 1: Environment & Maze Setup
* **Repo & structure:** Set up the local project in the repository with a simple 2D grid data structure to represent walls, open paths, start points, and exits.
* **Maze generators:** Code the Recursive Backtracking and Randomized Prim’s algorithms to generate valid, solvable grids.
* **Loop modification (Crucial step):** Standard Backtracking and Prim's create "perfect" mazes (only one path between any two points, zero loops). For a Pursuit-Evasion game to work without the evader getting cornered immediately, add a post-processing step that randomly removes a small percentage of walls to create alternate routes and loops.
* **Basic visualizer:** Build a quick console or lightweight 2D grid renderer (e.g., Pygame or simple matplotlib) to visually verify that the mazes generate properly before touching any AI logic.

## Phase 2: Game Loop & Moving Target Mechanics
* **Turn/Tick system:** Set up a discrete step-based loop where both the pursuer and the target (evader) update their positions.
* **Target movement:** Give the target a baseline behavior (e.g., shortest path toward the maze exit) so that the destination and start points dynamically change every tick.
* **Win/Loss states:** Define the termination conditions: Pursuer catches target (pursuer win), target reaches the maze exit (target win), or a max-step timeout.

## Phase 3: Core Search Algorithms (Single Agent, Fully Observable)
* **BFS (Baseline):** Implement Breadth-First Search first. Use it to verify that the agent can recalculate and chase the target on every tick, and use its performance as your baseline metric.
* **Dynamic $A^*$:** Implement $A^*$ using Manhattan distance as the heuristic. Have it re-plan dynamically as the target moves, and compare the number of expanded nodes against BFS.
* **MinMax:** Implement MinMax with a strict depth limit (since searching until the end of the maze will blow up the branching factor). Define a simple evaluation function for the leaf nodes—for example: $(\text{Target distance to exit}) - (\text{Pursuer distance to target})$.

## Phase 4: Evaluation & Comparison
* **Metrics logging:** Write a test script to run $X$ games per algorithm across both maze types (Backtracking vs. Prim's).
* **Data to track:** Capture rate (win %), average steps to catch the target, and average time/nodes expanded per move to measure computational complexity.

## Phase 5: Stretch Goals (If time permits)
* **Multi-agent pursuit:** Spawn 2–3 pursuers to test how single vs. multi-agent setups change the capture rate and computation time (either sharing a centralized controller or chasing independently).
* **Shifting goal points:** Allow the maze exit to relocate mid-game to test how quickly the algorithms adapt.
* **Unity3D port:** Once the core logic is solid, port the C#/Python grid and pathfinding scripts into Unity3D to build out the playable demo.