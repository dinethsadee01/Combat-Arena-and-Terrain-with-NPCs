using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BotController : MonoBehaviour
{
    public enum State { PATROL, CHASE, ATTACK, SEARCH, RETREAT }
    public State currentState;

    [Header("Stats")]
    public string botID = "BOT1";
    public int maxHP = 100;
    private int currentHP;
    public float moveSpeed = 5f;

    [Header("Sensors")]
    public float viewRadius = 15f;
    public float attackRange = 8f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("References")]
    public TextMesh statusText;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Combat Settings")]
    public float fireRate = 1.0f;
    private float nextFireTime = 0f;
    public float bulletSpeed = 15f;

    [Header("Visual Effects")]
    public Renderer botRenderer;
    private Color originalColor;
    private bool isDead = false;

    //PATHFINDING VARIABLES
    Vector3[] path;
    int targetIndex;
    Transform player;
    Vector3 lastKnownPosition;
    bool isPathfinding = false;
    float pathUpdateTimer = 0f;

    void Start()
    {
        currentHP = maxHP;
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Auto-find renderer if not assigned
        if (botRenderer == null) botRenderer = GetComponentInChildren<Renderer>();

        // Save the starting color
        if (botRenderer != null) originalColor = botRenderer.material.color;

        // Start in Patrol state
        SwitchState(State.PATROL);
    }

    void Update()
    {
        UpdateVisuals();

        // FSM Logic
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
            case State.SEARCH:
                SearchLogic();
                break;
            case State.RETREAT:
                RetreatLogic();
                break;
        }
    }

    void PatrolLogic()
    {
        // If see player -> CHASE
        if (CanSeePlayer())
        {
            SwitchState(State.CHASE);
            return;
        }

        if (path == null || targetIndex >= path.Length)
        {
            if (!isPathfinding) StartCoroutine(UpdatePath(GetRandomPoint()));
        }
    }

    void ChaseLogic()
    {
        // 1. If player too far/hidden -> SEARCH (Last known pos)
        if (!CanSeePlayer())
        {
            lastKnownPosition = player.position;
            SwitchState(State.SEARCH);
            return;
        }

        // 2. If close enough -> ATTACK
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            SwitchState(State.ATTACK);
            StopMoving();
            return;
        }

        // 3. Keep moving towards player
        if (Time.time > pathUpdateTimer)
        {
            pathUpdateTimer = Time.time + 0.5f; // Don't recalculate path every frame (will be laggy)
            StartCoroutine(UpdatePath(player.position));
        }
    }

    void AttackLogic()
    {
        // Look at player
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }

        // Fire
        if (Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate; // Reset timer
            Shoot(); // Actually fire
        }

        // Transitions
        if (!CanSeePlayer() || Vector3.Distance(transform.position, player.position) > attackRange)
        {
            SwitchState(State.CHASE);
        }

        // Low Health -> RETREAT
        if (currentHP < 30)
        {
            SwitchState(State.RETREAT);
        }
    }

    void SearchLogic()
    {
        // Go to last known pos. If arrived and still no player -> PATROL
        if (Vector3.Distance(transform.position, lastKnownPosition) < 2f)
        {
            SwitchState(State.PATROL);
        }
        else
        {
            if (!isPathfinding && path == null) StartCoroutine(UpdatePath(lastKnownPosition));
        }

        if (CanSeePlayer()) SwitchState(State.CHASE);
    }

    void RetreatLogic()
    {
        if (botRenderer != null) botRenderer.material.color = Color.green;

        // 1. MOVEMENT: If not moving, run to the furthest corner
        if (path == null)
        {
            Vector3 safeSpot = GetFurthestCorner();
            // Only request a new path if didn't already calculating one
            if (!isPathfinding) StartCoroutine(UpdatePath(safeSpot));
        }

        // 2. HEALING: If its far enough away, start recovering health
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > 15f)
        {
            // Simple regeneration (1 HP per frame approx 5% of the time to throttle speed)
            if (Random.Range(0, 100) < 5)
            {
                currentHP++;
                if (currentHP > maxHP) currentHP = maxHP; // Don't overload
            }
        }

        // 3. TRANSITION: If we healed enough, go back to the fight!
        if (currentHP > 70)
        {
            //Reset Visual Effect
            if (botRenderer != null) botRenderer.material.color = originalColor;
            // Reset path so we don't keep running to the corner
            StopCoroutine("FollowPath");
            path = null;
            SwitchState(State.CHASE); // Re-engage
        }
    }

    void SwitchState(State newState)
    {
        currentState = newState;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < viewRadius)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            // Check if wall is blocking view
            if (!Physics.Raycast(transform.position, dir, dist, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    Vector3 GetRandomPoint()
    {
        // Crude random point - in production use MapGenerator grid to pick valid floor
        return new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
    }

    Vector3 GetFurthestCorner()
    {
        // The map is roughly 200x200, centered at (0,0).
        // The edges are at +/- 100. We pick spots slightly inside (80) to avoid walls.
        float safeX = 20f;
        float safeZ = 20f;

        //4 corners of the arena
        Vector3[] corners = new Vector3[] {
        new Vector3(safeX, 1, safeX),   // Top Right
        new Vector3(safeX, 1, -safeZ),  // Bottom Right
        new Vector3(-safeX, 1, safeX),  // Top Left
        new Vector3(-safeX, 1, -safeZ)  // Bottom Left
    };

        Vector3 bestSpot = transform.position;
        float maxDist = -1f;

        // Loop through corners and find the one furthest from the player
        foreach (Vector3 corner in corners)
        {
            float dist = Vector3.Distance(player.position, corner);
            if (dist > maxDist)
            {
                maxDist = dist;
                bestSpot = corner;
            }
        }
        return bestSpot;
    }

    void UpdateVisuals()
    {
        if (statusText != null)
        {
            statusText.text = $"ID: {botID}\nSTATE: {currentState}\nHP: {currentHP}";
            // Make text face camera
            statusText.transform.rotation = Quaternion.LookRotation(statusText.transform.position - Camera.main.transform.position);
        }
    }

    void Shoot()
    {
        // Check if we have a bullet and firepoint
        if (bulletPrefab != null && firePoint != null)
        {
            // 1. Create Bullet
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // 2. Assign Owner (This works now because we fixed Bullet.cs!)
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.ownerTag = "Enemy";
            }

            // 3. Move Bullet
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = firePoint.forward * 20f;
            }

            // 4. Ignore collision with self
            Physics.IgnoreCollision(GetComponent<Collider>(), bulletObj.GetComponent<Collider>());

            Destroy(bulletObj, 2f);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Don't take damage if already dying

        currentHP = Mathf.Max(0, currentHP - damage);

        // Update the text above head immediately so we see the health drop
        UpdateVisuals();

        // Flash Red Effect
        StartCoroutine(FlashColor(Color.white)); // Flash white briefly indicates impact

        if (currentHP <= 0)
        {
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator FlashColor(Color flashColor)
    {
        if (botRenderer != null)
        {
            Color currentColor = botRenderer.material.color;
            botRenderer.material.color = flashColor;
            yield return new WaitForSeconds(0.1f);

            // If we are retreating, go back to green, otherwise original
            if (currentState == State.RETREAT) botRenderer.material.color = Color.green;
            else botRenderer.material.color = originalColor;
        }
    }

    IEnumerator DeathSequence()
    {
        isDead = true;

        // 1. Disable Logic
        // This stops the bot from moving, shooting, or thinking
        StopCoroutine("FollowPath");
        StopCoroutine("UpdatePath");
        this.enabled = false;
        GetComponent<Collider>().enabled = false; // Prevent bullets hitting it while dying

        // 2. Visual Death (Turn Black)
        if (botRenderer != null) botRenderer.material.color = Color.black;

        // 3. Shrink Animation
        Vector3 initialScale = transform.localScale;
        float timer = 0f;

        while (timer < 1.0f)
        {
            timer += Time.deltaTime;
            // Shrink from 100% to 0% over 1 second
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, timer);
            yield return null;
        }

        // 4. Finally Destroy
        Destroy(gameObject);
    }

    // --- PATHFINDING INTEGRATION ---
    // This connects to the Pathfinding script we made earlier

    IEnumerator UpdatePath(Vector3 targetPos)
    {
        isPathfinding = true;
        // Find the Pathfinding object in scene
        Pathfinding pathfinder = FindObjectOfType<Pathfinding>();

        if (pathfinder != null)
        {
            // Ask for path
            List<Node> pathList = pathfinder.FindPath(transform.position, targetPos);

            if (pathList != null && pathList.Count > 0)
            {
                // Convert Node list to Vector3 array
                path = new Vector3[pathList.Count];
                for (int i = 0; i < pathList.Count; i++)
                {
                    path[i] = pathList[i].worldPosition;
                }
                targetIndex = 0;

                StopCoroutine("FollowPath");
                StartCoroutine("FollowPath");
            }
        }
        isPathfinding = false;
        yield return null;
    }

    IEnumerator FollowPath()
    {
        if (path == null || path.Length == 0) yield break;

        int targetIndex = 0;
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            // This prevents the bot from being dragged down to Y=0.
            Vector3 targetPosition = new Vector3(currentWaypoint.x, transform.position.y, currentWaypoint.z);

            // Check distance ignoring height
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                targetIndex++;
                if (targetIndex >= path.Length)
                {
                    path = null;
                    yield break;
                }
                currentWaypoint = path[targetIndex];
                // Update target to next waypoint, keeping height correct
                targetPosition = new Vector3(currentWaypoint.x, transform.position.y, currentWaypoint.z);
            }

            // Move towards the adjusted targetPosition, NOT currentWaypoint
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    void StopMoving()
    {
        StopCoroutine("FollowPath");
        path = null;
    }
}