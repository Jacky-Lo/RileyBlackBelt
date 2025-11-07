using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class RangerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Attacking,
        Jumping
    }

    private PlayerState currentState = PlayerState.Idle;
    private bool isAttackInProgress = false; // Flag to prevent attack overlap

    [Header("Arrow")]
    public GameObject RangerArrow;

    [Header("Sound Manager")]
    public SoundManager soundManager;

    [Header("Character Stats")]
    public CharacterStats.Ranger rangerStats;

    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider manaSlider;

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 700f;
    public bool isGrounded;

    [Header("Attack Settings")]
    // First one stamina, second one mana
    public (float staminaCost, float manaCost) qAttackCost = (10, 5);
    public (float staminaCost, float manaCost) eAttackCost = (20, 10);
    public (float staminaCost, float manaCost) rAttackCost = (10, 25);

    [Header("Attack Colliders")]
    public GameObject attackQ;
    public GameObject attackE;
    public GameObject attackR;
    BoxCollider2D AttackCollider;

    public GameObject ECollider; // E attack collider - assign manually in inspector

    private Animator animator;
    private Rigidbody2D rb;

    [Header("Raycast Settings")]
    public Vector2 raycastOriginOffset = new Vector2(0, 0);
    public Vector2 raycastDirection = Vector2.down;
    public float raycastDistance = 1f;
    public Color raycastColor = Color.red;
    public LayerMask groundLayerMask = 1; // Add layer mask for ground detection

    public GameObject DeathScreen;

    private void Start()
    {
        // Initialize character stats
        rangerStats = new CharacterStats.Ranger();

        // Initialize components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Initialize UI sliders
        InitializeSlider(healthSlider, rangerStats.Health, rangerStats.Health);
        InitializeSlider(staminaSlider, rangerStats.Stamina, rangerStats.Stamina);
        InitializeSlider(manaSlider, rangerStats.Mana, rangerStats.Mana);
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
            soundManager.PlayRangerJump();
        }
    }

    private void HandleMovement()
    {
        if (currentState == PlayerState.Attacking) return;

        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack1") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack2") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack3"))
        {
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");

        // Check if player has enough stamina to move
        if (Mathf.Abs(horizontal) > 0.01f && rangerStats.Stamina <= 0)
        {
            // Player is exhausted, can't move
            return;
        }

        Vector3 movement = new Vector3(horizontal, 0, 0) * speed * Time.deltaTime;
        transform.position += movement;

        // Flip the character based on movement direction
        if (horizontal != 0)
        {
            if (soundManager != null && !soundManager.audioSource.isPlaying && isGrounded)
            {
                soundManager.PlayRangerWalk();
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
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            currentState = PlayerState.Jumping;
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
        // Prevent attacks if already attacking or if attack is in progress
        if (currentState == PlayerState.Attacking || isAttackInProgress) return;

        // Additional check for attack animations that might still be playing
        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack1") ||
            this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack2") ||
            this.animator.GetCurrentAnimatorStateInfo(0).IsName("RangerAttack3"))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!CanPerformAttack(qAttackCost))
            {
                Debug.Log("Not enough stamina for Q attack!");
                return;
            }
            SpendStamina(qAttackCost.staminaCost);
            SpendMana(qAttackCost.manaCost);
            // Spend Mana here as well
            Debug.Log("Q key pressed - triggering basic attack");
            TriggerAttack("BasicAttack", "PerformBasicAttack");
            if (soundManager != null)
            {
                // soundManager.PlayRangerQ();
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (!CanPerformAttack(eAttackCost))
            {
                Debug.Log("Not enough stamina for E attack!");
                return;
            }
            SpendStamina(eAttackCost.staminaCost);
            SpendMana(eAttackCost.manaCost);
            TriggerAttack("SecondAttack", "PerformSecondAttack");
            if (soundManager != null)
            {
                // soundManager.PlayRangerQ();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (!CanPerformAttack(rAttackCost))
            {
                Debug.Log("Not enough stamina for R attack!");
                return;
            }
            SpendStamina(rAttackCost.staminaCost);
            SpendMana(rAttackCost.manaCost);
            TriggerAttack("ThirdAttack", "PerformThirdAttack");
            if (soundManager != null)
            {
                // soundManager.PlayRangerR();
            }
        }
    }

    private bool SpendStamina(float amount)
    {
        if (rangerStats.Stamina < amount) return false;

        rangerStats.Stamina -= (int)amount;
        staminaSlider.value = rangerStats.Stamina;
        return true;
    }

    private bool SpendMana(float amount)
    {
        if (rangerStats.Mana < amount) return false;

        rangerStats.Mana -= (int)amount;
        manaSlider.value = rangerStats.Mana;
        return true;
    }

    private bool CanPerformAttack((float staminaCost, float manaCost) attackCost)
    {
        return ((rangerStats.Stamina >= attackCost.staminaCost) && (rangerStats.Mana >= attackCost.manaCost));
    }

    private void RegenerateResources()
    {
        // Regenerate stamina if not moving and not attacking
        if (rangerStats.Stamina < staminaSlider.maxValue)
        {
            rangerStats.Stamina = Mathf.Min(rangerStats.Stamina + rangerStats.StaminaRS * Time.deltaTime, staminaSlider.maxValue);
            staminaSlider.value = rangerStats.Stamina;
        }

        if (rangerStats.Mana < manaSlider.maxValue)
        {
            rangerStats.Mana = Mathf.Min(rangerStats.Mana + rangerStats.ManaRS * Time.deltaTime, manaSlider.maxValue);
            manaSlider.value = rangerStats.Mana;
        }
    }

    private void InitializeSlider(Slider slider, float maxValue, float currentValue)
    {
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    public void LevelUp()
    {
        rangerStats.LevelUp();
        Debug.Log("Ranger leveled up!");
        rangerStats.DisplayStats();

        // Update sliders with new stats
        InitializeSlider(healthSlider, rangerStats.Health, rangerStats.Health);
        InitializeSlider(staminaSlider, rangerStats.Stamina, rangerStats.Stamina);
        InitializeSlider(manaSlider, rangerStats.Mana, rangerStats.Mana);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for MobAI attacks
        MobAI mobAI = other.gameObject.GetComponentInParent<MobAI>();
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
        BossAI bossAI = other.gameObject.GetComponentInParent<BossAI>();
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

    private void TakeDamage(int damage)
    {
        Debug.Log("I took: " + damage.ToString() + " damage");
        healthSlider.value -= damage;
        if (soundManager != null)
        {
            soundManager.PlayRangerHurt();
        }

        if (healthSlider.value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        gameObject.tag = "Untagged";
        animator.SetBool("IsDead", true);
        speed = 0f;
        jumpForce = 0f;
        gameObject.GetComponent<PolygonCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        if (soundManager != null)
        {
            soundManager.PlayRangerDead();
        }
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
        isAttackInProgress = true; // Set flag to prevent overlapping attacks
        animator.SetTrigger(animationTrigger);
        float instantiateAtFrame = 0f;
        float animationEndsAtFrame = 0f;
        float soundDelay = 0f;
        switch (attackAction)
        {
            case "PerformBasicAttack":
                instantiateAtFrame = 0.22f;
                animationEndsAtFrame = 0.75f;
                soundDelay = 0.25f; // Delay sound to match when bow is actually shot
                Invoke("PlayRangerQSound", soundDelay);

                break;
            case "PerformSecondAttack":
                instantiateAtFrame = 0.28f;
                animationEndsAtFrame = 0.57f;
                soundDelay = 0.30f; // Delay sound to match when bow is actually shot
                Invoke("PlayRangerESound", soundDelay);
                break;
            case "PerformThirdAttack":
                instantiateAtFrame = 0.18f;
                animationEndsAtFrame = 0.65f;
                soundDelay = 0.36f; // Delay sound to match when bow is actually shot
                Invoke("PlayRangerRSound", soundDelay);
                break;
        }
        Invoke(attackAction, instantiateAtFrame);
        Invoke("ResetAttackState", animationEndsAtFrame); // Adjust duration if needed
    }

    void InstantiateArrowQ()
    {
        //Spawn the arrow here
        Vector3 rangerPosition = gameObject.transform.position;
        Vector3 arrowPosition;

        if (transform.localScale.x < 0)
        {
            arrowPosition = rangerPosition + new Vector3(-3.5f, 1f, 0);

        }
        else
        {
            arrowPosition = rangerPosition + new Vector3(3.5f, 1f, 0);
        }

        GameObject instantiatedArrow = Instantiate(RangerArrow, arrowPosition, Quaternion.identity);
        instantiatedArrow.GetComponent<ArrowMovement>().isFacingLeft = (transform.localScale.x < 0);
        instantiatedArrow.GetComponent<ArrowMovement>().SpeedY = 0f;
    }

    void PerformBasicAttack()
    {
        Debug.Log("Ranger AttackQ");

        // Check if RangerArrow prefab is assigned
        if (RangerArrow == null)
        {
            Debug.LogError("RangerArrow prefab is not assigned! Please assign it in the Inspector.");
            return;
        }
        Invoke("InstantiateArrowQ", 0.25f);
        //instantiatedArrow.transform.parent = null;
    }

    void PerformSecondAttack()
    {
        Debug.Log("Ranger AttackE");
        Vector3 spawnPosition = transform.position + new Vector3(0f, 0f, 0);
        GameObject attackECollider = Instantiate(attackE, spawnPosition, Quaternion.identity);
        attackECollider.transform.parent = gameObject.transform;
        attackECollider.transform.localScale = new Vector3(1f, 1f, 0);
        attackECollider.SetActive(true);

        Destroy(attackECollider, 0.15f);

        Invoke("PerformSecondAttack2", 0.17f);
    }

    // void PerformSecondAttack2(){
    //    Vector3 spawnPosition2 = transform.position + new Vector3(0f, 0f, 0);
    //   GameObject attackECollider2 = Instantiate(attackE, spawnPosition2, Quaternion.identity);
    //   attackECollider2.transform.parent = gameObject.transform;
    //    attackECollider2.transform.localScale = new Vector3(1f, 1f, 0);
    //    attackECollider2.SetActive(true);/            Destroy(attackECollider2, 0.35f);
    //    Debug.Log("Ranger AttackE2");
    //}

    IEnumerator InstantiateArrowR()
    {
        float player_scale_x = transform.localScale.x;
        yield return new WaitForSeconds(0.55f);

        //Spawn the arrow here
        Vector3 rangerPosition = gameObject.transform.position;
        Vector3 firstarrowPosition;
        Vector3 secondarrowPosition;
        Vector3 thirdarrowPosition;

        if (player_scale_x < 0)
        {
            firstarrowPosition = rangerPosition + new Vector3(-4f, 7f, 0);
            secondarrowPosition = firstarrowPosition + new Vector3(-4f, 0f, 0);
            thirdarrowPosition = secondarrowPosition + new Vector3(-4f, 0f, 0);

        }
        else
        {
            firstarrowPosition = rangerPosition + new Vector3(4f, 7f, 0);
            secondarrowPosition = firstarrowPosition + new Vector3(4f, 0f, 0);
            thirdarrowPosition = secondarrowPosition + new Vector3(4f, 0f, 0);
        }

        GameObject firstArrow = Instantiate(RangerArrow, firstarrowPosition, Quaternion.identity);
        firstArrow.transform.Rotate(0f, 0f, -60f);
        firstArrow.GetComponent<ArrowMovement>().isFacingLeft = (player_scale_x < 0);
        firstArrow.GetComponent<ArrowMovement>().SpeedX = 250f;
        firstArrow.GetComponent<ArrowMovement>().SpeedY = -600f;
        yield return new WaitForSeconds(0.3f);
        GameObject secondArrow = Instantiate(RangerArrow, secondarrowPosition, Quaternion.identity);
        secondArrow.transform.Rotate(0f, 0f, -60f);
        secondArrow.GetComponent<ArrowMovement>().isFacingLeft = (player_scale_x < 0);
        secondArrow.GetComponent<ArrowMovement>().SpeedX = 250f;
        secondArrow.GetComponent<ArrowMovement>().SpeedY = -600f;
        yield return new WaitForSeconds(0.3f);
        GameObject thirdArrow = Instantiate(RangerArrow, thirdarrowPosition, Quaternion.identity);
        thirdArrow.transform.Rotate(0f, 0f, -60f);
        thirdArrow.GetComponent<ArrowMovement>().isFacingLeft = (player_scale_x < 0);
        thirdArrow.GetComponent<ArrowMovement>().SpeedX = 250f;
        thirdArrow.GetComponent<ArrowMovement>().SpeedY = -600f;
    }

    void PerformThirdAttack()
    {
        Debug.Log("Ranger AttackR");

        // Check if RangerArrow prefab is assigned
        if (RangerArrow == null)
        {
            Debug.LogError("RangerArrow prefab is not assigned! Please assign it in the Inspector.");
            return;
        }
        StartCoroutine("InstantiateArrowR");
    }

    void PlayRangerQSound()
    {
        if (soundManager != null)
        {
            soundManager.PlayRangerQ();
        }
    }

    void PlayRangerESound()
    {
        ECollider.SetActive(true); // Activate E attack collider
        if (soundManager != null)
        {
            soundManager.PlayRangerE();
        }
    }

    void PlayRangerRSound()
    {
        if (soundManager != null)
        {
            soundManager.PlayRangerR();
        }
    }

    void ResetAttackState()
    {
        currentState = PlayerState.Idle;
        isAttackInProgress = false; // Reset attack flag
        ECollider.SetActive(false); // Deactivate E attack collider
        // Reset all attack animation triggers
        animator.ResetTrigger("BasicAttack");
        animator.ResetTrigger("SecondAttack");
        animator.ResetTrigger("ThirdAttack");
        // Also reset the IsAttacking bool to ensure the animator state is properly reset
        animator.SetBool("IsAttacking", false);

        Debug.Log("Attack state reset - ready for new attacks");
    }

    private void UpdateAnimatorStates()
    {
        animator.SetBool("IsIdle", currentState == PlayerState.Idle);
        animator.SetBool("IsWalking", currentState == PlayerState.Walking);
        animator.SetBool("IsAttacking", currentState == PlayerState.Attacking);

        animator.SetBool("IsJumping", currentState == PlayerState.Jumping);
    }
    
    void ActivateDeathScreen()
    {
        DeathScreen.SetActive(true);
    }
}
