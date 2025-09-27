using UnityEngine;

public class DisasterManager : MonoBehaviour
{
    public bool evacuateMode = true;

    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         TriggerDisaster();
    //     }
    // }

    public void TriggerDisaster()
    {
        NPCController[] npcs = FindObjectsOfType<NPCController>();
        foreach (var npc in npcs)
        {
            npc.TriggerPanic(evacuateMode);
        }
        Debug.Log("Disaster Triggered!");
    }
}
