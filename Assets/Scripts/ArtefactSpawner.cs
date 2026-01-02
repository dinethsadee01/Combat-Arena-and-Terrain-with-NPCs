using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshSurface))]
public class ArtefactSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject[] artefactPrefabs;
    public int countPerType = 3;
    public TerrainGenerator terrain;
    public GameObject playerPrefab;

    [Header("Validation")]
    public LayerMask terrainLayer;
    public float waterHeight = 7f;
    public float mountainHeight = 18.4f;

    [Header("Debug")]
    public bool showPath = false; // Press 'P' to toggle

    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> blockers = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private GameObject spawnedPlayer;

    private Material lineMaterial;

    [Header("Enemies")]
    public GameObject enemyAPrefab;
    public GameObject enemyBPrefab;
    public int enemyCount = 2;

    void Start()
    {
        // Material for the line
        lineMaterial = new Material(Shader.Find("Sprites/Default"));

        // Wait for Terrain to generate, then setup scene
        Invoke("SetupScene", 0.5f);
    }

    void SetupScene()
    {
        // 1. Create Invisible "Not Walkable" Zones for Water and Peaks
        CreateNavMeshBlockers();

        // 2. Bake the NavMesh on the generated terrain
        GetComponent<NavMeshSurface>().BuildNavMesh();

        // 3. Spawn Entities
        SpawnPlayer();
        SpawnArtefacts();
        SpawnEnemies();
    }

    void CreateNavMeshBlockers()
    {
        //1. WATER BLOCKER
        GameObject waterVol = new GameObject("Water_Blocker");
        waterVol.transform.position = new Vector3(terrain.width / 2f, waterHeight / 2f, terrain.depth / 2f);

        NavMeshModifierVolume waterMod = waterVol.AddComponent<NavMeshModifierVolume>();
        waterMod.size = new Vector3(terrain.width, waterHeight, terrain.depth);
        waterMod.area = 1; // Area 1 is "Not Walkable" in Unity standard

        blockers.Add(waterVol);

        //2. MOUNTAIN BLOCKER
        float skyLimit = 50f;
        float centerHeight = mountainHeight + (skyLimit / 2f);

        GameObject mountVol = new GameObject("Mountain_Blocker");
        mountVol.transform.position = new Vector3(terrain.width / 2f, centerHeight, terrain.depth / 2f);

        NavMeshModifierVolume mountMod = mountVol.AddComponent<NavMeshModifierVolume>();
        mountMod.size = new Vector3(terrain.width, skyLimit, terrain.depth);
        mountMod.area = 1; // "Not Walkable"

        blockers.Add(mountVol);
    }

    void SpawnPlayer()
    {
        int attempts = 0;

        // Loop to try multiple times
        while (attempts < 500)
        {
            attempts++;

            // Try to find a valid spot
            if (GetValidPointOnTerrain(out Vector3 spawnPos))
            {
                // SUCCESS!
                // Lift player UP by 2 units so they don't fall through the floor
                spawnedPlayer = Instantiate(playerPrefab, spawnPos + Vector3.up * 2f, Quaternion.identity);

                //If we found a spot, we stop looking.
                return;
            }
        }

        // If we tried 500 times and STILL failed (rare, but possible)
        Debug.LogError("Could not find a valid spawn point for player after 500 attempts!");
    }

    void SpawnArtefacts()
    {
        if (artefactPrefabs.Length == 0) return;

        foreach (GameObject prefab in artefactPrefabs)
        {
            for (int i = 0; i < countPerType; i++)
            {
                PlaceValidArtefact(prefab);
            }
        }
    }

    void PlaceValidArtefact(GameObject prefab)
    {
        int attempts = 0;
        while (attempts < 500) // Try 500 times to find a valid spot per item
        {
            attempts++;
            if (GetValidPointOnTerrain(out Vector3 pos))
            {
                // CHECK 1: Is it too low (Underwater)?
                if (pos.y < waterHeight) continue;

                // CHECK 2: Can the player reach it? (Path Validation)
                if (spawnedPlayer != null)
                {
                    NavMeshPath path = new NavMeshPath();
                    // Calculate path from Player to Potential Spot
                    NavMesh.CalculatePath(spawnedPlayer.transform.position, pos, NavMesh.AllAreas, path);

                    // If path is Partial (blocked) or Invalid, skip this spot
                    if (path.status != NavMeshPathStatus.PathComplete) continue;
                }

                // If player got here, it's valid!
                // Lift item slightly (0.5f) so it sits ON the grass, not IN it
                GameObject item = Instantiate(prefab, pos + Vector3.up * 0.5f, Quaternion.identity);
                SetupLineRenderer(item);
                spawnedItems.Add(item);
                return; // Done with this item
            }
        }
        Debug.LogWarning("Could not place an artefact after 500 attempts.");
    }

    void SetupLineRenderer(GameObject item)
    {
        LineRenderer lr = item.AddComponent<LineRenderer>();
        lr.startWidth = 0.3f; // Thinner lines look better when there are many
        lr.endWidth = 0.3f;
        lr.material = lineMaterial;
        lr.startColor = Color.cyan;
        lr.endColor = Color.cyan;
        lr.enabled = false; // Hide initially
    }

    //Finds a random point on the terrain that isn't underwater or too steep
    bool GetValidPointOnTerrain(out Vector3 result)
    {
        // Pick random X/Z inside terrain bounds
        float x = Random.Range(0, terrain.width);
        float z = Random.Range(0, terrain.depth);

        // Raycast down from the sky
        Ray ray = new Ray(new Vector3(x, 100, z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f, terrainLayer))
        {
            // CHECK 1: Height (Water)
            // If it is underwater, fail immediately
            if (hit.point.y < waterHeight)
            {
                result = Vector3.zero;
                return false;
            }

            // CHECK 2: Slope (Steepness)
            // Vector3.Angle calculates the angle between the ground normal and straight up
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 45f) // If steeper than 45 degrees
            {
                result = Vector3.zero;
                return false;
            }

            result = hit.point;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    void SpawnEnemies()
    {
        if (enemyAPrefab == null && enemyBPrefab == null) return;

        for (int i = 0; i < enemyCount; i++)
        {
            int attempts = 0;
            while (attempts < 500)
            {
                attempts++;
                if (GetValidPointOnTerrain(out Vector3 pos))
                {
                    // Ensure reachability
                    if (spawnedPlayer != null)
                    {
                        NavMeshPath path = new NavMeshPath();
                        NavMesh.CalculatePath(spawnedPlayer.transform.position, pos, NavMesh.AllAreas, path);
                        if (path.status != NavMeshPathStatus.PathComplete) continue;
                    }

                    // Pick A or B
                    GameObject prefab = (i % 2 == 0) ? enemyAPrefab : enemyBPrefab;

                    if (prefab != null)
                    {
                        GameObject en = Instantiate(prefab, pos + Vector3.up * 1f, Quaternion.identity);
                        spawnedEnemies.Add(en);
                    }
                    break; // Spawned successfully
                }
            }
        }
    }

    void Update()
    {
        // Toggle Visualization with 'P'
        if (Input.GetKeyDown(KeyCode.P)) showPath = !showPath;

        // If enabled, draw lines for EVERY item
        if (spawnedItems.Count > 0 && spawnedPlayer != null)
        {
            foreach (GameObject item in spawnedItems)
            {
                if (item == null) continue;

                LineRenderer lr = item.GetComponent<LineRenderer>();

                if (showPath)
                {
                    lr.enabled = true;
                    DrawPathForItem(item, lr);
                }
                else
                {
                    lr.enabled = false;
                }
            }
        }
    }

    void DrawPathForItem(GameObject item, LineRenderer lr)
    {
        NavMeshPath path = new NavMeshPath();
        // Calculate path from Player -> To This Item
        NavMesh.CalculatePath(spawnedPlayer.transform.position, item.transform.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            // This prevents the line from clipping into the hills.
            Vector3[] pathCorners = path.corners;
            for (int i = 0; i < pathCorners.Length; i++)
            {
                pathCorners[i] += Vector3.up * 1.0f; // Lift line by 1 meter
            }

            lr.positionCount = pathCorners.Length;
            lr.SetPositions(pathCorners);
        }
        else
        {
            // If path becomes invalid, hide line
            lr.positionCount = 0;
        }
    }

    // CALL THIS from TerrainGenerator when 'X' is pressed
    public void RegenerateAttributes()
    {
        // 1. Destroy the old Player
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
            spawnedPlayer = null;
        }

        // 2. Destroy all old Artefacts
        foreach (GameObject item in spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();

        // 3. Destroy Old Blockers
        foreach (GameObject b in blockers) { if (b != null) Destroy(b); }
        blockers.Clear();

        //4. Destroy Old Enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        spawnedEnemies.Clear();

        // 5. Re-run the setup
        SetupScene();
    }
}