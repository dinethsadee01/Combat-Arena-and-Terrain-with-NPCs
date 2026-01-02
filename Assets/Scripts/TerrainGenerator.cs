using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Dimensions")]
    public int width = 200;  // Size X
    public int depth = 200;  // Size Z

    [Header("Noise Settings")]
    public float scale = 25f;      // Zoom level of noise
    public float heightMultiplier = 20f; // How tall mountains are
    public float offsetX = 100f;   // Random scroll X
    public float offsetZ = 100f;   // Random scroll Z

    [Header("Colors")]
    public Gradient terrainGradient;

    public Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Randomize the map every time we play
        //offsetX = 451.9166f;
        //offsetZ = 8161.233f;
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);

        CreateShape();
        UpdateMesh();

        UnityEngine.Debug.Log($"SEED :: OffsetX: {offsetX}f | OffsetZ: {offsetZ}f");
    }

    void Update()
    {
        // Regenerate on 'X' for testing
        if (Input.GetKeyDown(KeyCode.X))
        {
            offsetX = Random.Range(0f, 9999f);
            offsetZ = Random.Range(0f, 9999f);
            CreateShape();
            UpdateMesh();

            Debug.Log($"SEED :: OffsetX: {offsetX}f | OffsetZ: {offsetZ}f");

            ArtefactSpawner spawner = GetComponent<ArtefactSpawner>();
            if (spawner != null)
            {
                spawner.RegenerateAttributes();
            }
        }
    }

    void CreateShape()
    {
        vertices = new Vector3[(width + 1) * (depth + 1)];
        colors = new Color[vertices.Length];

        for (int i = 0, z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                // Calculate Perlin Noise Height
                float y = Mathf.PerlinNoise((x + offsetX) / scale, (z + offsetZ) / scale);

                // Save vertex position
                vertices[i] = new Vector3(x, y * heightMultiplier, z);

                // Assign Color based on Height (0 to 1)
                colors[i] = terrainGradient.Evaluate(y);

                i++;
            }
        }

        triangles = new int[width * depth * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors; // Apply Vertex Colors
        mesh.RecalculateNormals(); // Fix lighting

        //Add a Mesh Collider so we can walk on it later
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null) collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }
}