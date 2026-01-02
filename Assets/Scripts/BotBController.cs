using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BotBController : MonoBehaviour
{
    public enum State { WANDER, ATTACK, FLEE }
    public State currentState;

    [Header("Stats")]
    public string botID = "BOT2";
    public int maxHP = 100;
    private int currentHP;

    [Header("AI Settings")]
    public float detectionRadius = 20f;
    public float fleeRadius = 8f; // Runs away if player is closer than this
    public float attackRange = 15f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1.0f; // Shoots slower
    private float nextFireTime = 0f;

    [Header("UI")]
    public TextMesh statusText;

    private NavMeshAgent agent;
    private Transform player;
    private Renderer botRenderer; 
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentHP = maxHP;

        // Make sure agent doesn't get stuck on walls
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    void Update()
    {
        UpdateVisuals();

        // Simple FSM
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer < fleeRadius)
        {
            SwitchState(State.FLEE);
        }
        else if (distToPlayer < detectionRadius && CanSeePlayer())
        {
            SwitchState(State.ATTACK);
        }
        else
        {
            SwitchState(State.WANDER);
        }

        RunStateLogic();
    }

    void RunStateLogic()
    {
        switch (currentState)
        {
            case State.WANDER:
                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    Vector3 randomPoint = GetRandomPoint(transform.position, 10f);
                    agent.SetDestination(randomPoint);
                }
                break;

            case State.ATTACK:
                agent.ResetPath(); // Stop moving to shoot
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

                if (Time.time > nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    Shoot();
                }
                break;

            case State.FLEE:
                // Run in opposite direction of player
                Vector3 dirToPlayer = transform.position - player.position;
                Vector3 newPos = transform.position + dirToPlayer.normalized * 5f;
                agent.SetDestination(newPos);
                break;
        }
    }

    void SwitchState(State newState)
    {
        currentState = newState;
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

            // Ignore collision with self
            Physics.IgnoreCollision(GetComponent<Collider>(), bullet.GetComponent<Collider>());

            Destroy(bullet, 2f);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;

        // Flash Effect
        StartCoroutine(FlashRed());

        if (currentHP <= 0)
        {
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator FlashRed()
    {
        if (botRenderer)
        {
            Color oldColor = botRenderer.material.color;
            botRenderer.material.color = Color.red; // Flash Red
            yield return new WaitForSeconds(0.1f);
            botRenderer.material.color = oldColor;
        }
    }

    IEnumerator DeathSequence()
    {
        isDead = true;
        GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = true; // Stop moving
        GetComponent<Collider>().enabled = false;

        // Spin and Shrink
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        while (timer < 1f)
        {
            timer += Time.deltaTime * 2f; // Die fast (0.5 seconds)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer);
            transform.Rotate(0, 10f, 0); // Spin around
            yield return null;
        }
        Destroy(gameObject);
    }

    // Helper: Pick random point on NavMesh
    Vector3 GetRandomPoint(Vector3 center, float range)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, range, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return center;
    }

    bool CanSeePlayer()
    {
        // Simple raycast check
        Vector3 dir = (player.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRadius))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    void UpdateVisuals()
    {
        if (statusText)
        {
            statusText.text = $"ID: {botID}\nHP: {currentHP}\n{currentState}";
            statusText.transform.rotation = Quaternion.LookRotation(statusText.transform.position - Camera.main.transform.position);
        }
    }
}