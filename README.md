# Procedural Generation & AI Sandbox

A technical demonstration of Procedural Content Generation (PCG) algorithms and Autonomous AI Agent architectures built in Unity (C#). This project explores the contrast between discrete, grid-based generation and continuous, organic terrain generation, integrated with adaptive NPC behaviors.

## 🎮 Project Overview

The project is divided into three distinct modules, each focusing on specific game development algorithms:

### 1. Cellular Automata Dungeon
* **Algorithm:** Uses Cellular Automata (Game of Life rules) to generate organic cave structures from random noise.
* **Pathfinding:** Implements a custom **A* (A-Star)** algorithm on a grid system.
* **Gameplay:** Top-down dungeon crawler where the player battles "Hunter" bots in tight, procedurally generated corridors.

### 2. Perlin Noise Terrain Engine
* **Algorithm:** Generates a 3D mesh at runtime using **Perlin Noise** to create smooth, rolling hills and valleys.
* **Visualization:** Vertex coloring based on height gradients (Water -> Sand -> Grass -> Mountain) without texture dependencies.
* **Validation:** A "brute-force with validation" spawner ensures all artifacts and enemies spawn in reachable locations by querying the NavMesh before placement.

### 3. Advanced AI & Probabilistic Combat
* **AI Architecture:** Non-Player Characters (NPCs) are built on **Finite State Machines (FSM)**.
* **Archetypes:**
    * **The Hunter:** Aggressive chaser that retreats to heal when critical (Survival Instinct).
    * **The Sniper:** Defensive agent that utilizes "Kiting" behavior to maintain optimal engagement distance.
* **Navigation:** Integration of Unity's **NavMesh** with procedural terrain, handling dynamic obstacles like water and steep slopes.
* **Combat:** Replaces deterministic damage with a probabilistic model (RPG-style Evasion and Critical Hit chance).

## 🛠️ Technical Stack
* **Engine:** Unity 2021.3+
* **Language:** C#
* **Core Concepts:** * Procedural Mesh Generation
    * Cellular Automata & Smoothing
    * A* Pathfinding (Custom Implementation)
    * Unity NavMesh API
    * Finite State Machines (FSM)

## 🚀 How to Run
1.  **Clone the repository:**
    ```bash
    git clone https://github.com/dinethsadee01/Combat-Arena-and-Terrain-with-NPCs.git
    ```
2.  **Open in Unity:**
    * Launch Unity Hub.
    * Click **Open** and select the cloned folder.
    * Wait for Unity to import assets and rebuild the Library.
3.  **Play:**
    * Open `Scenes/MainMenu` to access the selection hub.
    * Press **Play** in the Editor.

## 🕹️ Controls
* **WASD / Arrow Keys:** Move Character
* **Mouse:** Aim
* **Left Click:** Shoot
* **X (in Terrain Mode):** Regenerate World Seed
* **P:** Toggle Debug Visualization (Path Validation Lines)

## 📂 Project Structure
* `/Scripts/(All scripts with Section 1 tag)`: Logic for Cellular Automata and Grid A*.
* `/Scripts/(All scripts with Section 1 tag)`: Logic for Perlin Noise, Mesh generation, and Spawning validation.
* `/Scripts/(All scripts with Advanced tag)`: FSM logic for Hunter and Sniper agents.

---
## 🤝 Contributing
Pull requests are welcome!
If you find any bugs or have feature suggestions, feel free to open an issue.

---

## 📜 License
This project is licensed under the MIT License.
See the [LICENSE](https://github.com/dinethsadee01/Combat-Arena-and-Terrain-with-NPCs.git/blob/master/LICENSE) file for details.

---

## 👨‍💻 Author
Made with ❤️ by [Dineth Sadeepa](https://github.com/dinethsadee01/)
