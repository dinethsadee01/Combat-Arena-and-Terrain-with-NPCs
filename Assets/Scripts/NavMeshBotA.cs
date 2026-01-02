using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshBotA : MonoBehaviour
{
    public enum State { PATROL, CHASE, ATTACK, RETREAT }
    public State currentState;

    [Header("Stats")]
    public string botID = "BOT1";
    public float moveSpeed = 8f;
    public int maxHP = 100;
    private int currentHP;

    [Header("Sensors")]
    public float viewRadius = 20f;
    public float attackRange = 10f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.8f;
    private float nextFireTime = 0f;

    [Header("Visuals")]
    public TextMesh statusText;
    public Renderer botRenderer;
    private Color originalColor;
    public GameObject deathEffect;

    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        if (firePoint == null)
        {
            // Looks for a child named "Gun" or defaults to transform
            Transform childFP = transform.Find("Gun");
            firePoint = (childFP != null) ? childFP : transform;
        }

        if (statusText == null)
        {
            statusText = GetComponentInChildren<TextMesh>();
        }

        if (botRenderer == null) botRenderer = GetComponentInChildren<Renderer>();
        if (botRenderer != null) originalColor = botRenderer.material.color;

        // Find Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentHP = maxHP;
        SwitchState(State.PATROL);
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        UpdateVisuals();

        switch (currentState)
        {
            case State.PATROL:
                PatrolLogic();
                break;
            case State.CHASE:
                ChaseLogic();
                break;
            case State.ATTACK:
                AttackLogic();
                break;
            case State.RETREAT:
                RetreatLogic();
                break;
        }
    }

    void PatrolLogic()
    {
        // 1. If see player -> CHASE
        if (Vector3.Distance(transform.position, player.position) < viewRadius)
        {
            SwitchState(State.CHASE);
            return;
        }

        // 2. Random Wandering
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomPoint = GetRandomPoint(transform.position, 15f);
            agent.SetDestination(randomPoint);
        }
    }

    void ChaseLogic()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // 1. If close enough -> ATTACK
        if (dist <= attackRange)
        {
            SwitchState(State.ATTACK);
            agent.ResetPath();
            return;
        }

        // 2. If lost player -> PATROL
        if (dist > viewRadius * 1.5f)
        {
            SwitchState(State.PATROL);
            return;
        }

        // 3. Move to Player
        agent.SetDestination(player.position);
    }

    void AttackLogic()
    {
        // Look at player
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        // Shoot
        if (Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        // Transitions
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange) SwitchState(State.CHASE);

        // RETREAT Logic (The Bot A Speciality)
        if (currentHP < 30) SwitchState(State.RETREAT);
    }

    void RetreatLogic()
    {
        if (botRenderer) botRenderer.material.color = Color.green;

        // Run away from player
        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            Vector3 retreatPos = GetFurthestCorner();
            agent.SetDestination(retreatPos);
        }

        // Heal Logic
        if (Vector3.Distance(transform.position, player.position) > 15f)
        {
            if (Random.Range(0, 100) < 5) currentHP++; // Slow heal
        }

        // Re-engage
        if (currentHP > 70)
        {
            if (botRenderer) botRenderer.material.color = originalColor;
            SwitchState(State.CHASE);
        }
    }

    void Shoot()
    {
        if (bulletPrefab && firePoint)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet b = bullet.GetComponent<Bullet>();
            if (b != null) b.ownerTag = "Enemy";
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = firePoint.forward * 20f;
            Destroy(bullet, 2f);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHP -= dmg;
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        this.enabled = false;
        GetComponent<Collider>().enabled = false;

        if (botRenderer) botRenderer.material.color = Color.black;
        if (deathEffect) Instantiate(deathEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 1f);
    }

    void SwitchState(State newState) { currentState = newState; }

    void UpdateVisuals()
    {
        if (statusText)
        {
            statusText.text = $"ID: HUNTER\n{currentState}\nHP: {currentHP}";
            statusText.transform.rotation = Quaternion.LookRotation(statusText.transform.position - Camera.main.transform.position);
        }
    }

    Vector3 GetRandomPoint(Vector3 center, float range)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, range, NavMesh.AllAreas)) return hit.position;
        return center;
    }

    Vector3 GetFurthestCorner()
    {
        // Simple corner logic for Terrain (0,0) to (50,50)
        //Vector3[] corners = { new Vector3(5, 0, 5), new Vector3(45, 0, 45), new Vector3(5, 0, 45), new Vector3(45, 0, 5) };
        Vector3[] corners = { new Vector3(20, 0, 20), new Vector3(180, 0, 180), new Vector3(20, 0, 180), new Vector3(180, 0, 20) };
        Vector3 best = transform.position;
        float maxDst = 0;
        foreach (Vector3 c in corners)
        {
            // Sample height so we don't pick a point inside a mountain
            float y = Terrain.activeTerrain ? Terrain.activeTerrain.SampleHeight(c) : 5f;
            Vector3 validC = new Vector3(c.x, y, c.z);
            float d = Vector3.Distance(player.position, validC);
            if (d > maxDst) { maxDst = d; best = validC; }
        }
        return best;
    }
}