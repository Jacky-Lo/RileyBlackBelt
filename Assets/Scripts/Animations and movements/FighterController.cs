using UnityEngine;
using UnityEngine.UI;

public class FighterController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Attacking,
        Jumping
    }

    private PlayerState currentState = PlayerState.Idle;

    public SoundManager soundManager;
    public GameObject Deathscreen;
    [Header("Character Stats")]
    public CharacterStats.Fighter fighterStats;

    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider manaSlider;

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 700f;
    public float JumpCost = 25f;
    public bool isGrounded;

    [Header("Attack Settings")]
    public float qAttackCost = 5f;
    public float eAttackCost = 15f;
    public float rAttackCost = 30f;

    [Header("Attack Colliders")]
    public GameObject attackQ;
    public GameObject attackE;
    public GameObject attackR;
    BoxCollider2D AttackCollider;

    public GameObject ECollider;
    [Header("Ground Detection")]
    public BoxCollider2D groundDetectionCollider; // Assign this manually in the inspector

    [Header("Raycast Settings")]
    public Vector2 raycastOriginOffset = new Vector2(0, 0);
    public Vector2 raycastDirection = Vector2.down;
    public float raycastDistance = 1f;
    public Color raycastColor = Color.red;
    public LayerMask groundLayerMask = 1; // Add layer mask for ground detection

    private Animator animator;
    private Rigidbody2D rb;

    public GameObject DeathScreen;

    private void Start()
    {
        // Initialize character stats
        fighterStats = new CharacterStats.Fighter();

        // Initialize components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Validate ground detection collider setup
        ValidateGroundDetectionCollider();

        // Initialize UI sliders
        InitializeSlider(healthSlider, fighterStats.Health, fighterStats.Health);
        InitializeSlider(staminaSlider, fighterStats.Stamina, fighterStats.Stamina);
        InitializeSlider(manaSlider, fighterStats.Mana, fighterStats.Mana);
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAttacks();
        UpdateAnimatorStates();
        RegenerateResources();

        // Use raycast for ground detection (more reliable than collision detection)
        bool wasGrounded = isGrounded;
        isGrounded = RaycastHitsGround((Vector2)transform.position + raycastOriginOffset, raycastDirection, raycastDistance, raycastColor);

        // Play landing sound when transitioning from not grounded to grounded
        if (!wasGrounded && isGrounded && soundManager != null)
        {
            soundManager.PlayFighterJump();
        }
    }

    private void HandleMovement()
    {
        if (currentState == PlayerState.Attacking || animator.GetBool("IsDead")) return;

        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Fighter Attack New") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("Fighter Attack2") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("Fighter Attack3"))
        {
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");

        // Check if player has enough stamina to move
        if (Mathf.Abs(horizontal) > 0.01f && staminaSlider.value <= 0)
        {
            // Player is exhausted, can't move
            return;
        }

        Vector3 movement = new Vector3(horizontal, 0, 0) * speed * Time.deltaTime;
        transform.position += movement;

        // Consume stamina while moving
        if (Mathf.Abs(horizontal) > 0.01f)
        {

            if (soundManager != null && !soundManager.audioSource.isPlaying && isGrounded)
            {
                soundManager.PlayFighterWalk();
            }
            // If moving right, ensure normal scale
            if (horizontal > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            // If moving left, flip scale
            else
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        // Only update state if not attacking or jumping
        if (currentState != PlayerState.Attacking && currentState != PlayerState.Jumping)
        {
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                currentState = PlayerState.Walking;
            }
            else
            {
                currentState = PlayerState.Idle;
            }
        }
    }

    private void HandleJump()
    {
        // Check for jump input and if grounded
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            if (!CanPerformJump(JumpCost))
            {
                Debug.Log("Not enough stamina for Jump!");
                return;
            }
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            currentState = PlayerState.Jumping;
            SpendStamina(JumpCost);

        }

        // Simple state management: if grounded, not jumping; if not grounded, jumping
        if (isGrounded)
        {
            // Only change state if we're currently jumping
            if (currentState == PlayerState.Jumping)
            {
                float horizontal = Input.GetAxis("Horizontal");
                if (Mathf.Abs(horizontal) > 0.01f)
                {
                    currentState = PlayerState.Walking;
                }
                else
                {
                    currentState = PlayerState.Idle;
                }
            }
        }
        else
        {
            // If not grounded, we're jumping (unless attacking)
            if (currentState != PlayerState.Attacking)
            {
                currentState = PlayerState.Jumping;
            }
        }
    }



    private void HandleAttacks()
    {
        if (currentState != PlayerState.Idle || animator.GetBool("IsDead")) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!CanPerformAttack(qAttackCost))
            {
                Debug.Log("Not enough stamina for Q attack!");
                return;
            }

            // Only set the main polygon collider as trigger, not the ground detection collider
            PolygonCollider2D mainCollider = GetComponent<PolygonCollider2D>();
            if (mainCollider != null)
            {
                mainCollider.isTrigger = true;
            }

            SpendStamina(qAttackCost);
            TriggerAttack("BasicAttack", "PerformBasicAttack");
            if (soundManager != null)
            {
                // soundManager.PlayFighterQ();
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (!CanPerformAttack(eAttackCost))
            {
                Debug.Log("Not enough stamina for E attack!");
                return;
            }

            // Only set the main polygon collider as trigger, not the ground detection collider
            PolygonCollider2D mainCollider = GetComponent<PolygonCollider2D>();
            if (mainCollider != null)
            {
                mainCollider.isTrigger = true;
            }

            SpendStamina(eAttackCost);
            TriggerAttack("SecondAttack", "PerformSecondAttack");

            if (soundManager != null)
            {
                // PlayFighterE();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (!CanPerformAttack(rAttackCost))
            {
                Debug.Log("Not enough stamina for R attack!");
                return;
            }

            // Only set the main polygon collider as trigger, not the ground detection collider
            PolygonCollider2D mainCollider = GetComponent<PolygonCollider2D>();
            if (mainCollider != null)
            {
                mainCollider.isTrigger = true;
            }

            SpendStamina(rAttackCost);
            TriggerAttack("ThirdAttack", "PerformThirdAttack");
            if (soundManager != null)
            {
                // soundManager.PlayFighterR();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision detected: " + other.gameObject.name);

        // Ignore ground collisions
        if (other.gameObject.name == "Ground")
        {
            return;
        }

        // Check for MobAI attacks
        MobAI mobAI = other.gameObject.transform.parent != null ? other.gameObject.transform.parent.GetComponent<MobAI>() : null;
        if (mobAI == null)
        {
            mobAI = other.gameObject.GetComponent<MobAI>();
        }
        
        if (mobAI != null)
        {
            // Only take damage if the enemy's attack trigger is active and this is the attack trigger collider
            if (mobAI.attackTrigger != null && other == mobAI.attackTrigger && mobAI.attackTrigger.enabled)
            {
                Debug.Log("I took damage from enemy attack.");
                TakeDamage(mobAI.Damage);
            }
            return; // Exit early to avoid checking BossAI if we already found MobAI
        }

        // Check for BossAI attacks
        BossAI bossAI = other.gameObject.transform.parent != null ? other.gameObject.transform.parent.GetComponent<BossAI>() : null;
        if (bossAI == null)
        {
            bossAI = other.gameObject.GetComponent<BossAI>();
        }
        
        if (bossAI != null)
        {
            // Only take damage if the boss's attack trigger is active and this is the attack trigger collider
            if (bossAI.attackTrigger != null && other == bossAI.attackTrigger && bossAI.attackTrigger.enabled)
            {
                Debug.Log("I took damage from boss attack.");
                TakeDamage(bossAI.Damage);
            }
        }
    }

    private bool SpendStamina(float amount)
    {
        if (staminaSlider.value < amount) return false;

        staminaSlider.value -= amount;
        return true;
    }

    private bool CanPerformAttack(float staminaCost)
    {
        return staminaSlider.value >= staminaCost;
    }

    private bool CanPerformJump(float staminaCost)
    {
        return staminaSlider.value >= staminaCost;
    }

    private void HandleSpecialAbilities()
    {

    }

    private void RegenerateResources()
    {
        // Regenerate stamina if not moving and not attacking
        if (staminaSlider.value < staminaSlider.maxValue)
        {
            staminaSlider.value = Mathf.Min(staminaSlider.value + fighterStats.StaminaRS * Time.deltaTime, staminaSlider.maxValue);
        }
    }

    private void InitializeSlider(Slider slider, float maxValue, float currentValue)
    {
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    private void ValidateGroundDetectionCollider()
    {
        if (groundDetectionCollider == null)
        {
            Debug.LogWarning("Ground Detection Collider is not assigned! Please assign a BoxCollider2D to the groundDetectionCollider field in the inspector.");
            return;
        }

        // Ensure the ground detection collider is not a trigger
        if (groundDetectionCollider.isTrigger)
        {
            Debug.LogWarning("Ground Detection Collider should not be a trigger! Setting isTrigger to false.");
            groundDetectionCollider.isTrigger = false;
        }
    }

    private void TakeDamage(int damage)
    {
        healthSlider.value -= damage;
        if (soundManager != null)
        {
            soundManager.PlayFighterHurt();
        }

        if (healthSlider.value <= 0 && !animator.GetBool("IsDead"))
        {
            Die();
        }
    }

    private void Die()
    {
        animator.SetBool("IsDead", true);
        speed = 0f;
        jumpForce = 0f;
        gameObject.GetComponent<PolygonCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        if (soundManager != null)
        {
            soundManager.PlayFighterDead();
        }
        DeathScreen.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision.gameObject);
        }
    }

    private bool GetIsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 1.5f, LayerMask.GetMask("Ground"));
    }
    /// <summary>
    /// Casts a ray from the given origin in the given direction and distance, and returns true if it hits an object with the tag "Ground". Draws the ray for visualization.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray.</param>
    /// <param name="distance">The length of the ray.</param>
    /// <param name="color">The color to draw the ray in the Scene view.</param>
    /// <returns>True if the ray hits an object with the tag "Ground"; otherwise, false.</returns>
    public bool RaycastHitsGround(Vector2 origin, Vector2 direction, float distance, Color color)
    {
        Debug.DrawRay(origin, direction.normalized * distance, color);
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, LayerMask.GetMask("Ground"));
        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            return true;
        }
        return false;
    }

    // Overload for backward compatibility (default color: red)
    public bool RaycastHitsGround(Vector2 origin, Vector2 direction, float distance)
    {
        return RaycastHitsGround(origin, direction, distance, Color.red);
    }

    /// <summary>
    /// Debug method to visualize raycast information in the console
    /// </summary>
    public void DebugRaycastInfo()
    {
        Vector2 origin = (Vector2)transform.position + raycastOriginOffset;
        Debug.Log($"Raycast Origin: {origin}");
        Debug.Log($"Raycast Direction: {raycastDirection}");
        Debug.Log($"Raycast Distance: {raycastDistance}");
        Debug.Log($"Ground Layer Mask: {groundLayerMask.value}");
        Debug.Log($"Is Grounded: {isGrounded}");

        RaycastHit2D hit = Physics2D.Raycast(origin, raycastDirection, raycastDistance, groundLayerMask);
        if (hit.collider != null)
        {
            Debug.Log($"Hit Object: {hit.collider.gameObject.name}, Tag: {hit.collider.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        }
        else
        {
            Debug.Log("No hit detected");
        }
    }

    private void HandleEnemyCollision(GameObject enemy)
    {
        MobAI enemyStats = enemy.GetComponent<MobAI>();
        if (enemyStats != null)
        {
            // Example: Using Q_dmg as the damage value
            int damage = enemyStats.Damage;
        }
    }
    private void TriggerAttack(string animationTrigger, string attackAction)
    {
        currentState = PlayerState.Attacking;
        animator.SetTrigger(animationTrigger);
        float instantiateAtFrame = 0f;
        float animationEndsAtFrame = 0f;
        float soundDelay = 0f;
        switch (attackAction)
        {
            case "PerformBasicAttack":
                instantiateAtFrame = 0.22f;
                animationEndsAtFrame = 0.53f;
                soundDelay = 0.25f;
                Invoke("PlayFighterQSound", soundDelay);
                break;
            case "PerformSecondAttack":
                instantiateAtFrame = 0.28f;
                animationEndsAtFrame = 0.59f;
                soundManager.PlayFighterE();
                ECollider.SetActive(true);
                break;
            case "PerformThirdAttack":
                instantiateAtFrame = 0.18f;
                animationEndsAtFrame = 0.6f;
                soundDelay = 0.3f;
                Invoke("PlayFighterRSound", soundDelay);
                break;
        }
        Invoke(attackAction, instantiateAtFrame);
        Invoke("ResetAttackState", animationEndsAtFrame); // Adjust duration if needed    }
    }

    void PerformBasicAttack()
    {
        Debug.Log("Fighter AttackQ");
        Vector3 spawnPosition = transform.position + new Vector3(0f, 0f, 0);
        GameObject attackQCollider = Instantiate(attackQ, spawnPosition, Quaternion.identity);
        attackQCollider.transform.parent = gameObject.transform;
        attackQCollider.transform.localScale = new Vector3(1f, 1f, 0);
        attackQCollider.SetActive(true);
        Destroy(attackQCollider, 0.53f);

        gameObject.tag = "FighterQ";
    }
    void PerformSecondAttack()
    {
        Debug.Log("Fighter AttackE");
        // Vector3 spawnPosition = transform.position + new Vector3(0f, 0f, 0);
        // GameObject attackECollider = Instantiate(attackE, spawnPosition, Quaternion.identity);
        // attackECollider.transform.parent = gameObject.transform;
        // attackECollider.transform.localScale = new Vector3(1f, 1f, 0);
        // attackECollider.SetActive(true);
        //

        // Destroy(attackECollider, 0.15f);

        // gameObject.tag = "FighterE";
    }

    void PerformThirdAttack()
    {
        Vector3 spawnPosition = transform.position + new Vector3(0f, 0f, 0);
        GameObject attackRCollider = Instantiate(attackR, spawnPosition, Quaternion.identity);
        attackRCollider.transform.parent = gameObject.transform;
        attackRCollider.transform.localScale = new Vector3(1f, 1f, 0);
        attackRCollider.SetActive(true);
        Destroy(attackRCollider, 1f);

        gameObject.tag = "FighterR";
    }

    void PlayFighterQSound()
    {
        if (soundManager != null)
        {
            soundManager.PlayFighterQ();
        }
    }

    void PlayFighterRSound()
    {
        if (soundManager != null)
        {
            soundManager.PlayFighterR();
        }
    }


    void ResetAttackState()
    {
        currentState = PlayerState.Idle;
        ECollider.SetActive(false);
    }

    private void UpdateAnimatorStates()
    {
        animator.SetBool("IsIdle", currentState == PlayerState.Idle);
        animator.SetBool("IsWalking", currentState == PlayerState.Walking);
        animator.SetBool("IsAttacking", currentState == PlayerState.Attacking);
        animator.SetBool("IsJumping", currentState == PlayerState.Jumping);

        if (currentState == PlayerState.Idle)
        {
            if (!animator.GetBool("IsDead"))
            {
                gameObject.tag = "Player";
            }

            // Only reset the main polygon collider, not the ground detection collider
            PolygonCollider2D mainCollider = GetComponent<PolygonCollider2D>();
            if (mainCollider != null)
            {

                mainCollider.isTrigger = false;
            }
        }
    }

    void SetMainColliderTriggerOff()
    {
        GetComponent<PolygonCollider2D>().isTrigger = false;
    }

    void ActivateDeathScreen()
    {
        Deathscreen.SetActive(true);
    }
}
