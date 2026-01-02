using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class TerrainPlayer : MonoBehaviour
{
    [Header("Visuals")]
    public TextMesh hpText;

    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float minHeight = 7.1f;  // Water Level (HeightMultiplier * 0.35 roughly)
    public float maxHeight = 18.4f;  // Mountain Peak (HeightMultiplier * 0.94 roughly)
    public float mapSize = 200f;

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.2f;
    private float nextFireTime = 0f;

    [Header("Stats")]
    public int maxHP = 100;
    private int currentHP;
    private bool isDead = false;

    // Inventory
    private int coins = 0;
    private int gems = 0;
    private int scrolls = 0;
    private int keys = 0;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 mousePos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHP = maxHP;
        UpdateHPText();
    }

    void Update()
    {
        // 0. Stop everything if dead
        if (isDead) return;

        // 1. Mouse Aiming (Find where mouse is on the ground)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast against everything (Default layer)
        if (Physics.Raycast(ray, out hit, 200f))
        {
            mousePos = hit.point;
        }

        // 2. Keep HP Text facing camera
        if (hpText != null)
        {
            hpText.transform.rotation = Quaternion.LookRotation(hpText.transform.position - Camera.main.transform.position);
        }

        //Shooting Input
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void FixedUpdate()
    {
        // 0. Stop phyics if dead
        if (isDead) return;

        // 1. Rotation: Face the mouse position
        Vector3 lookDir = mousePos - transform.position;
        lookDir.y = 0; // Keep rotation flat (don't look down at feet)

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            // Rotate smoothly
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 15f);
        }

        // 2. Movement: Relative to where we are facing
        float x = Input.GetAxisRaw("Horizontal"); // A/D = Strafe
        float z = Input.GetAxisRaw("Vertical");   // W/S = Forward/Back

        // Calculate direction relative to Player's rotation
        Vector3 moveDir = (transform.forward * z + transform.right * x).normalized;
        Vector3 targetVelocity = moveDir * moveSpeed;

        //VALIDATION CHECKS (Water/Mountain)
        //Predict future position
        if (moveDir != Vector3.zero)
        {
            Vector3 futurePos = transform.position + moveDir * 1.0f;
            if (!IsPositionWalkable(futurePos))
            {
                targetVelocity = Vector3.zero;
            }
        }

        //Apply Velocity (Keep Gravity)
        targetVelocity.y = rb.velocity.y;
        rb.velocity = targetVelocity;

        // 3. Map Boundaries
        float clampedX = Mathf.Clamp(transform.position.x, 0f, mapSize);
        float clampedZ = Mathf.Clamp(transform.position.z, 0f, mapSize);

        if (transform.position.x != clampedX || transform.position.z != clampedZ)
        {
            rb.MovePosition(new Vector3(clampedX, transform.position.y, clampedZ));
        }
    }

    //Raycast down to check ground height
    bool IsPositionWalkable(Vector3 targetPos)
    {
        // Raycast from sky (y=100) downwards
        Ray ray = new Ray(new Vector3(targetPos.x, 100f, targetPos.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200f))
        {
            float groundHeight = hit.point.y;

            // Check Water (Too Low)
            if (groundHeight < minHeight) return false;

            // Check Mountain (Too High)
            if (groundHeight > maxHeight) return false;

            return true; // Safe to walk
        }

        return false; // Out of bounds
    }

    void Shoot()
    {
        if (projectilePrefab && firePoint)
        {
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Bullet b = bullet.GetComponent<Bullet>();
            if (b != null) b.ownerTag = "Player"; //Mark as Player bullet

            Rigidbody brb = bullet.GetComponent<Rigidbody>();
            if (brb != null) brb.velocity = firePoint.forward * 20f; // Speed

            Destroy(bullet, 2f);
        }
    }

    public void CollectArtefact(Artefact.ArtefactType type, int value)
    {
        if (isDead) return;

        switch (type)
        {
            case Artefact.ArtefactType.Potion:
                currentHP = Mathf.Min(currentHP + 10, maxHP); // Heal 10
                UpdateHPText();
                break;
            case Artefact.ArtefactType.Trap:
                currentHP = Mathf.Max(currentHP - 10, 0); // Damage 10
                UpdateHPText();
                if (currentHP == 0) Die();
                break;
            case Artefact.ArtefactType.Coin:
                coins++;
                break;
            case Artefact.ArtefactType.Gem:
                gems++;
                break;
            case Artefact.ArtefactType.Scroll:
                scrolls++;
                break;
            case Artefact.ArtefactType.Key:
                keys++;
                break;
        }
    }

    //VISUALS & GUI
    // Call this function when enemies damage the player
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        // Probability Logic
        int roll = Random.Range(0, 100);

        if (roll < 20) // 20% Chance to MISS
        {
            Debug.Log("Missed!");
            return;
        }
        else if (roll >= 90) // 10% Chance for CRITICAL
        {
            Debug.Log("Critical!");
            amount *= 2;
        }

        // Apply Damage
        currentHP -= amount;
        UpdateHPText();

        if (currentHP <= 0) Die();
    }

    void UpdateHPText()
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + currentHP;
            hpText.color = (currentHP < 30) ? Color.red : Color.white;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpText != null) hpText.text = "DEAD";
        Debug.Log("Player Died");

        // Pause Game Logic
        Time.timeScale = 0f;
    }

    // Draw Inventory on Top Right
    void OnGUI()
    {
        if (isDead)
        {
            //GAME OVER SCREEN
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 50;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.red;

            // Draw Big Red Text
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER", headerStyle);

            // Draw Restart Button
            if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 50, 100, 40), "Restart"))
            {
                // Unpause time before reloading!
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
            //INVENTORY HUD (Only visible when alive)
            float w = 150; float h = 120;
            float x = Screen.width - w - 10; float y = 10;

            GUI.Box(new Rect(x, y, w, h), "Inventory");

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;

            GUI.Label(new Rect(x + 10, y + 25, w, 20), $"Coins: {coins}", style);
            GUI.Label(new Rect(x + 10, y + 45, w, 20), $"Gems: {gems}", style);
            GUI.Label(new Rect(x + 10, y + 65, w, 20), $"Scrolls: {scrolls}", style);
            GUI.Label(new Rect(x + 10, y + 85, w, 20), $"Keys: {keys}", style);
        }
    }
}