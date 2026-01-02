using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 50;
    public int height = 50;

    [Range(0, 100)]
    public int randomFillPercent = 47;

    [Header("Seed Settings")]
    public string seed;
    public bool useRandomSeed;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Enemies")]
    public GameObject botAPrefab;
    public int numberOfBots = 4;
    public GameObject botBPrefab;

    // 1 = Wall, 0 = Floor
    public int[,] map;

    public GameObject playerPrefab;
    public Pathfinding pathfinder;
    public NavMeshSurface navMeshSurface;

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        // X key to regenerate map
        if (Input.GetKeyDown(KeyCode.X))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];

        // Step 1: Initialize map with random noise
        RandomFillMap();

        // Step 2: Smooth the map using Cellular Automata rules
        // 5 iterations is standard for a clean cave look
        for (int i = 0; i < 5; i++)
        {
            SmoothMap();
        }

        // Step 3: Instantiate the actual GameObjects
        GenerateMapVisuals();

        //Step 4: Build NavMesh AFTER visuals are created
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }

        //Update A*Grid
        if (pathfinder != null)
        {
            pathfinder.CreateGrid();
        }

        // Step 5: Spawn Entities
        SpawnPlayer();
        SpawnEnemies();
    }

    // Step 1: Random Initialization
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Boundary Check: Ensure outer walls are always present [Requirement]
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    // If random roll < fillPercent, it's a wall (1), else floor (0)
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    // Step 2: Cellular Automata Smoothing
    void SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallCount = GetSurroundingWallCount(x, y);

                // Rule 1: If > 4 neighbors are walls, become a wall
                if (neighborWallCount > 4)
                    newMap[x, y] = 1;
                // Rule 2: If < 4 neighbors are walls, become a floor
                else if (neighborWallCount < 4)
                    newMap[x, y] = 0;
                // Rule 3: If exactly 4, stay the same
                else
                    newMap[x, y] = map[x, y];

                // Force boundaries again just in case
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    newMap[x, y] = 1;
                }
            }
        }
        map = newMap;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        // Check a 3x3 grid around the target tile
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                // Ensure we are inside the map grid
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    // Don't count the tile itself
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += map[neighborX, neighborY];
                    }
                }
                else
                {
                    // Edge of map counts as a wall
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    // Step 3: Visualization
    void GenerateMapVisuals()
    {
        // Clear previous map objects (if regenerating)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (wallPrefab == null)
        {
            Debug.LogError("Please assign a Wall Prefab in the Inspector!");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);

                if (map[x, y] == 1)
                {
                    // Spawn Wall
                    GameObject wall = Instantiate(wallPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
                    wall.transform.parent = transform; // Keep hierarchy clean
                    wall.name = $"Wall_{x}_{y}";
                }
                else if (floorPrefab != null)
                {
                    // Spawn Floor
                    GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity);
                    floor.transform.parent = transform;
                    floor.name = $"Floor_{x}_{y}";
                }
            }
        }
    }

    // Helper to visualise data without meshes (Debug view in Scene tab)
    /*
    void OnDrawGizmos() {
        if (map != null) {
            for (int x = 0; x < width; x ++) {
                for (int y = 0; y < height; y ++) {
                    Gizmos.color = (map[x,y] == 1)? Color.black : Color.white;
                    Vector3 pos = new Vector3(-width/2 + x + .5f, 0, -height/2 + y + .5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }
    */

    void SpawnPlayer()
    {
        // Find a random floor tile
        for (int x = width / 2; x < width; x++)
        {
            for (int y = height / 2; y < height; y++)
            {
                if (map[x, y] == 0) // Found empty floor
                {
                    Vector3 spawnPos = new Vector3(-width / 2 + x + .5f, 0.5f, -height / 2 + y + .5f);

                    // If player exists, move them; otherwise spawn them
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = spawnPos;
                    }
                    else if (playerPrefab != null)
                    {
                        Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                    }
                    return; // Stop after spawning
                }
            }
        }
    }

    void SpawnEnemies()
    {
        // 1. Clear old bots
        BotController[] oldBotsA = FindObjectsOfType<BotController>();
        foreach (BotController bot in oldBotsA) Destroy(bot.gameObject);

        BotBController[] oldBotsB = FindObjectsOfType<BotBController>();
        foreach (BotBController bot in oldBotsB) Destroy(bot.gameObject);

        // 2. Create the "Deck" of enemies
        List<GameObject> enemyDeck = new List<GameObject>();
        for (int i = 0; i < numberOfBots; i++)
        {
            // Alternates between A and B
            if (i % 2 == 0) enemyDeck.Add(botAPrefab);
            else enemyDeck.Add(botBPrefab);
        }

        int botsSpawned = 0;
        int attempts = 0;

        // 3. Spawn them
        while (botsSpawned < numberOfBots && attempts < 1000)
        {
            attempts++;
            int x = UnityEngine.Random.Range(1, width - 1);
            int y = UnityEngine.Random.Range(1, height - 1);

            if (map[x, y] == 0)
            {
                float safeHeight = 1f;
                Vector3 spawnPos = new Vector3(-width / 2 + x + .5f, safeHeight, -height / 2 + y + .5f);

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && Vector3.Distance(spawnPos, player.transform.position) < 10f)
                {
                    continue;
                }

                // Pick the next enemy from our guaranteed list
                GameObject prefabToSpawn = enemyDeck[botsSpawned];

                if (prefabToSpawn != null)
                {
                    GameObject bot = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                    bot.transform.parent = transform;
                }

                botsSpawned++;
            }
        }
    }
}