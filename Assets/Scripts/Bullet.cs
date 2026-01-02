using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;

    public string ownerTag;

    void OnTriggerEnter(Collider other)
    {
        // 1. If ownerTag is missing, skip the safety check to avoid crashes
        if (!string.IsNullOrEmpty(ownerTag))
        {
            if (other.CompareTag(ownerTag)) return; // Don't hit the person who shot me
        }

        // 2. Ignore Artefacts
        if (other.GetComponent<Artefact>() != null) return;

        // 3. Ignore other bullets
        if (other.GetComponent<Bullet>() != null) return;

        // 4. Hit Enemy
        if (ownerTag != "Enemy")
        {
            // Try Bot A (Section 1)
            BotController botA = other.GetComponent<BotController>();
            if (botA != null) { botA.TakeDamage(damage); Destroy(gameObject); return; }

            // Try Bot B (Sniper)
            BotBController botB = other.GetComponent<BotBController>();
            if (botB != null) { botB.TakeDamage(damage); Destroy(gameObject); return; }

            // Try NavMeshBotA (Section 3)
            NavMeshBotA terrainBot = other.GetComponent<NavMeshBotA>();
            if (terrainBot != null) { terrainBot.TakeDamage(damage); Destroy(gameObject); return; }
        }

        // 5. Hit Player
        if (ownerTag != "Player")
        {
            // Try Section 1 Player
            PlayerController p1 = other.GetComponent<PlayerController>();
            if (p1 != null) { p1.TakeDamage(damage); Destroy(gameObject); return; }

            // Try Section 2/3 Terrain Player
            TerrainPlayer p2 = other.GetComponent<TerrainPlayer>();
            if (p2 != null) { p2.TakeDamage(damage); Destroy(gameObject); return; }
        }

        // Hit Terrain or Mesh Collider
        if (other.GetComponent<TerrainGenerator>() != null || other.GetComponent<MeshCollider>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // 7. Hit Wall (Section 1)
        if (other.CompareTag("Wall") || other.CompareTag("Floor"))
        {
            Destroy(gameObject);
            return;
        }

        // Default: Destroy on any other collision
        //Destroy(gameObject);
    }
}