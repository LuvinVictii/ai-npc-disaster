using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Wander, Panic, Evacuate }
    public NPCState currentState = NPCState.Wander;

    private NavMeshAgent agent;
    private float timer;

    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float wanderTimer = 4f;
    public float normalSpeed = 2f;

    [Header("Panic Settings")]
    public float panicSpeed = 6f;
    public Transform[] evacuatePoints; // assign di Inspector
    private int evacuationIndex = 0;

    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    [Range(0f, 1f)] public float interactionChance = 0.5f;
    public float minInteractionDuration = 2f;
    public float maxInteractionDuration = 5f;
    public float interactionCooldown = 3f;
    [Range(0f, 1f)] public float joinChance = 0.3f; // kemungkinan nimbrung NPC lain

    private bool isInteracting = false;
    private bool isCooldown = false;
    private float cooldownTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
        agent.speed = normalSpeed;
        // Find all object with tag evacuate points in the scene
        if (evacuatePoints.Length == 0)
        {
            GameObject[] evacuateObjects = GameObject.FindGameObjectsWithTag("EvacuatePoint");
            evacuatePoints = evacuateObjects.Select(obj => obj.transform).ToArray();
        }
        // Debug.Log("Evacuate Points Found: " + evacuatePoints.Length);
    }

    void Update()
    {
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0) isCooldown = false;
        }

        if (!isInteracting && !isCooldown)
        {
            TryInteraction();
        }

        switch (currentState)
        {
            case NPCState.Idle:
                Idle();
                break;
            case NPCState.Wander:
                Wander();
                break;
            case NPCState.Panic:
                Panic();
                break;
            case NPCState.Evacuate:
                Evacuate();
                break;
        }
    }

    void Idle()
    {
        // Bisa tambahin animasi idle
    }

    void TryInteraction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("NPC"))
            {
                NPCController other = hit.GetComponent<NPCController>();

                // Kalau NPC lain lagi interaksi → ada chance nimbrung
                if (other != null && other.isInteracting && Random.value < joinChance)
                {
                    StartCoroutine(InteractWith(other));
                    return;
                }

                // Kalau NPC lain lagi cooldown → skip
                if (other != null && other.isCooldown) continue;

                // Kalau dua-duanya idle dan chance berhasil
                if (other != null && !other.isInteracting && !other.isCooldown && Random.value < interactionChance)
                {
                    StartCoroutine(InteractWith(other));
                    return;
                }
            }
        }
    }

    void Wander()
    {
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    void Panic()
    {
        agent.speed = panicSpeed;
        // lari random lebih cepat
        if (!agent.hasPath)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
        }
    }

    void Evacuate()
    {
        agent.speed = panicSpeed;
        agent.SetDestination(evacuatePoints[evacuationIndex].position);
        // Debug.Log("Evacuating to: " + evacuatePoints[evacuationIndex].position);
    }

    public void TriggerPanic(bool evacuate)
    {
        if (evacuate)
            StartCoroutine(PanicAndEvacuate(0.1f, 2f));
        else
            currentState = NPCState.Panic;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    private IEnumerator PanicAndEvacuate(float minDuration, float maxDuration)
    {
        currentState = NPCState.Panic;
        float panicDuration = Random.Range(minDuration, maxDuration);
        yield return new WaitForSeconds(panicDuration);
        evacuationIndex = Random.Range(0, evacuatePoints.Length);
        currentState = NPCState.Evacuate;
    }

    private IEnumerator InteractWith(NPCController other)
    {
        Debug.Log(name + " interacting with " + (other != null ? other.name : "self"));
        isInteracting = true;
        currentState = NPCState.Idle;
        agent.isStopped = true;

        if (other != null)
        {
            other.isInteracting = true;
            other.currentState = NPCState.Idle;
            other.agent.isStopped = true;

            // Saling berhadapan
            Vector3 dirToOther = (other.transform.position - transform.position).normalized;
            transform.forward = new Vector3(dirToOther.x, 0, dirToOther.z);
            other.transform.forward = -new Vector3(dirToOther.x, 0, dirToOther.z);
        }

        float duration = Random.Range(minInteractionDuration, maxInteractionDuration);
        yield return new WaitForSeconds(duration);

        // Selesai interaksi
        isInteracting = false;
        isCooldown = true;
        cooldownTimer = interactionCooldown;
        agent.isStopped = false;
        currentState = NPCState.Wander;

        if (other != null)
        {
            other.isInteracting = false;
            other.isCooldown = true;
            other.cooldownTimer = other.interactionCooldown;
            other.agent.isStopped = false;
            other.currentState = NPCState.Wander;
        }
    }
}
