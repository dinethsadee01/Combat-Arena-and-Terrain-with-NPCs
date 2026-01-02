using UnityEngine;

public class Artefact : MonoBehaviour
{
    // Dropdown to select type in Inspector
    public enum ArtefactType { Coin, Gem, Scroll, Key, Potion, Trap }
    public ArtefactType type;

    public int value = 1; // Count to add

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the player script
            TerrainPlayer player = other.GetComponent<TerrainPlayer>();

            if (player != null)
            {
                player.CollectArtefact(type, value);

                // Destroy this object (and its cyan line)
                Destroy(gameObject);
            }
        }
    }
}