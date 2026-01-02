using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Visuals")]
    public TextMesh hpText;

    [Header("Stats")]
    public float moveSpeed = 10f;
    public int maxHealth = 150;
    private int currentHealth;

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;
    private bool isDead = false;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 mousePos;
    private Camera mainCam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;
        currentHealth = maxHealth;
        UpdateHPText();
    }

    void Update()
    {
        // 1. Stop inputs if dead
        if (isDead) return;

        // 2. Input Processing
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(x, 0f, z).normalized;

        // 3. Mouse Aiming (Raycasting)
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Plane at height 0
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            mousePos = point;

            // Debug line to see where we are looking
            Debug.DrawLine(transform.position, point, Color.red);
        }

        // 4. Shooting
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        // 5. Keep HP Text facing the camera (Optional polish)
        if (hpText != null)
        {
            hpText.transform.rotation = Quaternion.LookRotation(hpText.transform.position - mainCam.transform.position);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        Move();
        Turn();
    }

    void Move()
    {
        // MovePosition is better for collision detection than transform.Translate
        //rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        Vector3 targetVelocity = moveInput * moveSpeed;
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
    }

    void Turn()
    {
        // Calculate the direction to look at
        Vector3 lookDir = mousePos - transform.position;
        lookDir.y = 0; // Keep rotation strictly horizontal

        if (lookDir != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(rotation);
        }
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // 1. Assign Tag
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null) bulletScript.ownerTag = "Player";

            // 2. Make it Move
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = firePoint.forward * projectileSpeed;

            Destroy(bulletObj, 2f);
        }
    }

    // Call this function when enemies damage the player
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        UpdateHPText();

        if (currentHealth <= 0) Die();
    }

    void UpdateHPText()
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + currentHealth;
            // Change color if low health
            if (currentHealth < 30) hpText.color = Color.red;
            else hpText.color = Color.green;
        }
    }

    void Die()
    {
        if (isDead) return; // Don't die twice

        isDead = true;
        Debug.Log("GAME OVER");

        // Stop the game time (pauses everything)
        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (isDead)
        {
            // 1. Create a style for Big Text
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 50;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.red;

            // 2. Draw the Text in the middle of the screen
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER", headerStyle);

            // 3. Add a Restart Button below the text
            if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 50, 100, 40), "Restart"))
            {
                // Unpause the game before reloading!
                Time.timeScale = 1f;
                // Reload current scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
    }
}