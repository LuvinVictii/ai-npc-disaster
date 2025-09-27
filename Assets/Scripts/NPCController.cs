using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Wander, Interaction, Panic, Evacuate }

    [Header("State Settings")]
    public NPCState initialState = NPCState.Wander;

    [Header("Colors per State")]
    public Color idleColor = Color.green;
    public Color wanderColor = Color.blue;
    public Color interactionColor = Color.magenta;
    public Color panicColor = Color.red;
    public Color evacuateColor = Color.yellow;

    [Header("Wander Settings")]
    public float wanderRadius = 8f;
    public float wanderTimer = 4f;
    public float normalSpeed = 2f;

    [Header("Panic Settings")]
    public float panicSpeed = 6f;
    public Transform[] evacuatePoints;
    private int evacuationIndex = 0;

    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    [Range(0f, 1f)] public float interactionChance = 0.5f;
    public float minInteractionDuration = 2f;
    public float maxInteractionDuration = 5f;
    public float interactionCooldown = 3f;
    [Range(0f, 1f)] public float joinChance = 0.3f;

    // Private
    private NavMeshAgent agent;
    private float timer;
    private bool isCooldown = false;
    private float cooldownTimer = 0f;

    // State handling
    private NPCState _currentState;
    public NPCState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value) return; // avoid redundant updates
            _currentState = value;
            UpdateColor();
        }
    }

    // Rendering
    private Renderer rend;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        timer = wanderTimer;
        agent.speed = normalSpeed;

        if (evacuatePoints.Length == 0)
        {
            GameObject[] evacuateObjects = GameObject.FindGameObjectsWithTag("EvacuatePoint");
            evacuatePoints = evacuateObjects.Select(obj => obj.transform).ToArray();
        }

        CurrentState = initialState;
    }

    void Update()
    {
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0) isCooldown = false;
        }

        if (CurrentState != NPCState.Interaction && !isCooldown)
        {
            TryInteraction();
        }

        switch (CurrentState)
        {
            case NPCState.Idle: Idle(); break;
            case NPCState.Wander: Wander(); break;
            case NPCState.Panic: Panic(); break;
            case NPCState.Evacuate: Evacuate(); break;
            case NPCState.Interaction: /* handled in coroutine */ break;
        }
    }

    void Idle() { /* animasi idle optional */ }

    void TryInteraction()
    {
        if (CurrentState == NPCState.Panic || CurrentState == NPCState.Evacuate) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || !hit.CompareTag("NPC")) continue;

            NPCController other = hit.GetComponent<NPCController>();
            if (other == null) continue;

            // nimbrung ke interaksi
            if (other.CurrentState == NPCState.Interaction && Random.value < joinChance)
            {
                StartCoroutine(InteractWith(other));
                return;
            }

            // skip kalau cooldown
            if (other.isCooldown) continue;

            // dua-duanya bisa interaksi
            if (other.CurrentState != NPCState.Interaction && Random.value < interactionChance)
            {
                StartCoroutine(InteractWith(other));
                return;
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
        if (!agent.hasPath)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
        }
    }

    void Evacuate()
    {
        agent.speed = panicSpeed;
        if (evacuatePoints.Length > 0)
            agent.SetDestination(evacuatePoints[evacuationIndex].position);
    }

    public void TriggerPanic(bool evacuate)
    {
        StopAllCoroutines();
        agent.isStopped = false;
        agent.speed = panicSpeed;
        if (evacuate) StartCoroutine(PanicAndEvacuate(0.1f, 2f));
        else CurrentState = NPCState.Panic;
    }

    private IEnumerator PanicAndEvacuate(float minDuration, float maxDuration)
    {
        CurrentState = NPCState.Panic;
        float panicDuration = Random.Range(minDuration, maxDuration);
        yield return new WaitForSeconds(panicDuration);
        if (evacuatePoints.Length > 0)
        {
            evacuationIndex = Random.Range(0, evacuatePoints.Length);
            CurrentState = NPCState.Evacuate;
        }
    }

    private IEnumerator InteractWith(NPCController other)
    {
        CurrentState = NPCState.Interaction;
        agent.isStopped = true;

        if (other != null)
        {
            other.CurrentState = NPCState.Interaction;
            other.agent.isStopped = true;

            Vector3 dirToOther = (other.transform.position - transform.position).normalized;
            transform.forward = new Vector3(dirToOther.x, 0, dirToOther.z);
            other.transform.forward = -new Vector3(dirToOther.x, 0, dirToOther.z);
        }

        float duration = Random.Range(minInteractionDuration, maxInteractionDuration);
        yield return new WaitForSeconds(duration);

        EndInteraction();
        if (other != null) other.EndInteraction();
    }

    private void EndInteraction()
    {
        isCooldown = true;
        cooldownTimer = interactionCooldown;
        agent.isStopped = false;
        CurrentState = NPCState.Wander;
    }

    // === Utilities ===
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, layermask);
        return navHit.position;
    }

    private void UpdateColor()
    {
        Color c = wanderColor;
        switch (CurrentState)
        {
            case NPCState.Idle: c = idleColor; break;
            case NPCState.Wander: c = wanderColor; break;
            case NPCState.Interaction: c = interactionColor; break;
            case NPCState.Panic: c = panicColor; break;
            case NPCState.Evacuate: c = evacuateColor; break;
        }
        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", c);
        rend.SetPropertyBlock(propBlock);
    }
}
