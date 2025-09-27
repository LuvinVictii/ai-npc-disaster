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
}
