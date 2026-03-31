using UnityEngine;

public class InfluenceManager : MonoBehaviour
{
    public InfluenceMap map;
    public Transform player;

    [Header("Influence Settings")]
    [Tooltip("How much danger influence the player adds")]
    public float playerInfluence = 10f;
    
    [Tooltip("Radius of player's danger influence")]
    public float playerInfluenceRadius = 5f;

    void Update()
    {
        // Clear only the influence map (not heat - it decays on its own)
        map.ClearMap();
        
        // Player adds danger influence (immediate threat)
        if (player != null)
        {
            map.AddInfluence(player.position, playerInfluence, playerInfluenceRadius);
        }
        
        // Enemies add influence
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            EnemyAI ai = e.GetComponent<EnemyAI>();
            if (ai != null)
            {
                map.AddInfluence(e.transform.position, ai.scary, 3f);
            }
        }

        // Heat map updates automatically via PlayerHeatTracker and decay
    }
}