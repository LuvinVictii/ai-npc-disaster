using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject npcPrefab;       // prefab NPC
    public int npcCount = 10;          // jumlah NPC yang mau di-spawn
    public float spawnRadius = 20f;    // radius area spawn dari posisi spawner
    public float sampleDistance = 5f;  // jarak max NavMesh.SamplePosition

    void Start()
    {
        SpawnNPCs();
    }

    void SpawnNPCs()
    {
        for (int i = 0; i < npcCount; i++)
        {
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            randomPos += transform.position;
            NavMeshHit hit;

            // Cari posisi valid di NavMesh
            if (NavMesh.SamplePosition(randomPos, out hit, sampleDistance, NavMesh.AllAreas))
            {
                GameObject npc = Instantiate(npcPrefab, hit.position, Quaternion.identity);
                npc.tag = "NPC"; // pastikan NPC ditandai
            }
            else
            {
                Debug.LogWarning("Gagal spawn NPC ke-" + i + " (tidak ketemu NavMesh).");
            }
        }
    }
}
