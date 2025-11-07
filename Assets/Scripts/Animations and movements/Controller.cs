using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Controller : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Attacking,
        Jumping
    }

    [Header("Spells")]
    public GameObject WizardFireball;
    public GameObject WizardIcespear;

    public GameObject WizardLightning;

    private PlayerState currentState = PlayerState.Idle;

    public SoundManager soundManager;
    [Header("Character Stats")]
    public CharacterStats.Wizard wizardStats;

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
    public float qAttackCost = 5;
    public float eAttackCost = 10;
    public float rAttackCost = 25;
    public float JumpCost = 25;

    [Header("Attack Colliders")]
    public GameObject attackQ;
    public GameObject attackE;
    public GameObject attackR;
    BoxCollider2D AttackCollider;

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
        wizardStats = new CharacterStats.Wizard();

        // Initialize components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
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
            soundManager.PlayWizardJump();
        }
    }


    private void HandleMovement()
    {
        if (currentState == PlayerState.Attacking) return;

        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Wizard Attack") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("Wizard Attack2") || this.animator.GetCurrentAnimatorStateInfo(0).IsName("Wizard Attack3"))
        {
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(horizontal, 0, 0) * speed * Time.deltaTime;
        transform.position += movement;

        // Flip the character based on movement direction
        if (horizontal != 0)
        {
            if (soundManager != null && !soundManager.audioSource.isPlaying && isGrounded)
            {
                soundManager.PlayWizardWalk();
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
            if (!CanPerformAttack(JumpCost))
            {
                Debug.Log("Not enough stamina or mana for jump!");
                return;
            }
            SpendStamina(JumpCost);
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
        if (currentState != PlayerState.Idle) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!CanPerformAttack(qAttackCost))
            {
                Debug.Log("Not enough stamina or mana for Q attack!");
                return;
            }
            SpendMana(qAttackCost);
            TriggerAttack("BasicAttack", "PerformBasicAttack");
            if (soundManager != null)
            {
                // soundManager.PlayWizardQ();
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (!CanPerformAttack(eAttackCost))
            {
                Debug.Log("Not enough stamina or mana for E attack!");
                return;
            }
            SpendStamina(eAttackCost);
            SpendMana(eAttackCost);
            TriggerAttack("SecondAttack", "PerformSecondAttack");
            if (soundManager != null)
            {
                // soundManager.PlayWizardQ();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (!CanPerformAttack(rAttackCost))
            {
                Debug.Log("Not enough stamina or mana for R attack!");
                return;
            }
            SpendStamina(rAttackCost);
            SpendMana(rAttackCost);
            TriggerAttack("ThirdAttack", "PerformThirdAttack");
            if (soundManager != null)
            {
                // soundManager.PlayWizardR();
            }
        }
    }


    private bool SpendStamina(float amount)
    {
        if (staminaSlider.value < amount) return false;
        staminaSlider.value -= (int)amount;
        return true;
    }

    private bool SpendMana(float amount)
    {
        if (manaSlider.value < amount) return false;
        manaSlider.value -= (int)amount;
        return true;
    }

    private bool CanPerformAttack(float attackCost)
    {
        return (manaSlider.value >= attackCost);
    }
    private bool CanPerformJump(float  JumpCost)
    {
        return (staminaSlider.value >= JumpCost);
    }


    private void RegenerateResources()
    {
        // Regenerate stamina if not moving and not attacking
        if (staminaSlider.value < staminaSlider.maxValue)
        {
            staminaSlider.value = Mathf.Min(wizardStats.Stamina + wizardStats.StaminaRS * Time.deltaTime, staminaSlider.maxValue);
        }

        if (manaSlider.value < manaSlider.maxValue)
        {
            manaSlider.value = Mathf.Min(wizardStats.Mana + wizardStats.ManaRS * Time.deltaTime, manaSlider.maxValue);
        }
    }

    private void InitializeSlider(Slider slider, float maxValue, float currentValue)
    {
        slider.maxValue = maxValue;
        slider.value = currentValue;
    }

    public void LevelUp()
    {
        wizardStats.LevelUp();
        Debug.Log("Wizard leveled up!");
        wizardStats.DisplayStats();

        // Update sliders with new stats
        InitializeSlider(healthSlider, wizardStats.Health, wizardStats.Health);
        InitializeSlider(staminaSlider, wizardStats.Stamina, wizardStats.Stamina);
        InitializeSlider(manaSlider, wizardStats.Mana, wizardStats.Mana);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
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
    
    private void TakeDamage(int damage)
    {
        healthSlider.value -= damage;
        if (soundManager != null)
        {
            soundManager.PlayWizardHurt();
        }

        if (healthSlider.value <= 0)
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
            soundManager.PlayWizardDead();
        }
        DeathScreen.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (!isGrounded) // Only play sound if the player was previously not grounded
            {
                if (soundManager != null)
                    soundManager.PlayWizardJump(); // Play landing sound
            }
            isGrounded = true;
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
        else if (collision.gameObject.CompareTag("Enemy"))
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
        CharacterStats enemyStats = enemy.GetComponent<CharacterStats>();
        if (enemyStats != null)
        {
            // Example: Using Q_dmg as the damage value
            int damage = enemyStats.Q_dmg;
            Debug.Log($"Collided with {enemyStats.Class}, taking {damage} damage.");
            TakeDamage(damage);
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
                instantiateAtFrame = 0.27f;
                animationEndsAtFrame = 0.53f;
                soundDelay = 0.25f;
                Invoke("PlayWizardQSound", soundDelay);
                break;
            case "PerformSecondAttack":
                instantiateAtFrame = 0.32f;
                animationEndsAtFrame = 0.43f;
                soundDelay = 0.4f;
                Invoke("PlayWizardESound", soundDelay);
                break;
            case "PerformThirdAttack":
                instantiateAtFrame = 0.18f;
                animationEndsAtFrame = 0.40f;
                soundDelay = 0.3f;
                Invoke("PlayWizardRSound", soundDelay);
                break;
        }
        Invoke(attackAction, instantiateAtFrame);
        Invoke("ResetAttackState", animationEndsAtFrame); // Adjust duration if needed    }
    }


    void InstantiateFireball()
    {
        //Spawn the fireball here
        Vector3 wizardPosition = gameObject.transform.position;
        Vector3 firePosition;

        if (transform.localScale.x < 0)
        {
            firePosition = wizardPosition + new Vector3(-3.5f, 1.9f, 0);

        }
        else
        {
            firePosition = wizardPosition + new Vector3(3.5f, 1.9f, 0);
        }

        GameObject instantiatedFire = Instantiate(WizardFireball, firePosition, Quaternion.identity);
        instantiatedFire.GetComponent<FireBallMovement>().isFacingLeft = (transform.localScale.x < 0);
    }

    void InstantiateIcespear()
    {
        //Spawn the icespear here
        Vector3 wizardPosition = gameObject.transform.position;
        Vector3 icePosition;

        if (transform.localScale.x < 0)
        {
            icePosition = wizardPosition + new Vector3(-3.5f, 2.5f, 0);

        }
        else
        {
            icePosition = wizardPosition + new Vector3(3.5f, 2.5f, 0);
        }

        GameObject instantiatedIce = Instantiate(WizardIcespear, icePosition, Quaternion.identity);
        instantiatedIce.GetComponent<IceMovement>().isFacingLeft = (transform.localScale.x < 0);
    }

    IEnumerator InstantiateLightning()
    {
        yield return new WaitForSeconds(0.3f);
        //Spawn the icespear here
        Vector3 wizardPosition = gameObject.transform.position;
        Vector3 firstLightningPosition;
        Vector3 secondLightningPosition;
        Vector3 thirdLightningPosition;

        if (transform.localScale.x < 0)
        {
            firstLightningPosition = wizardPosition + new Vector3(-7f, 1f, 0);
            secondLightningPosition = firstLightningPosition + new Vector3(-3.5f, 0f, 0);
            thirdLightningPosition = secondLightningPosition + new Vector3(-3.5f, 0f, 0);

        }
        else
        {
            firstLightningPosition = wizardPosition + new Vector3(7f, 1f, 0);
            secondLightningPosition = firstLightningPosition + new Vector3(3.5f, 0f, 0);
            thirdLightningPosition = secondLightningPosition + new Vector3(3.5f, 0f, 0);
        }

        GameObject firstLightning = Instantiate(WizardLightning, firstLightningPosition, Quaternion.identity);
        yield return new WaitForSeconds(0.3f);
        GameObject secondLightning = Instantiate(WizardLightning, secondLightningPosition, Quaternion.identity);
        yield return new WaitForSeconds(0.3f);
        GameObject thirdLightning = Instantiate(WizardLightning, thirdLightningPosition, Quaternion.identity);
    }

    void PerformBasicAttack()
    {
        Debug.Log("Wizard AttackQ");

        // Check if WizardFire prefab is assigned
        if (WizardFireball == null)
        {
            Debug.LogError("WizardFireball prefab is not assigned! Please assign it in the Inspector.");
            return;
        }
        Invoke("InstantiateFireball", 0.25f);
        //instantiatedFire.transform.parent = null;
        }


        void PerformSecondAttack()
        {
        Debug.Log("Wizard AttackE");

        // Check if WizardIce prefab is assigned
        if (WizardIcespear == null)
        {
            Debug.LogError("WizardIcespear prefab is not assigned! Please assign it in the Inspector.");
            return;
        }
        Invoke("InstantiateIcespear", 0.25f);
        //instantiatedIce.transform.parent = null;
        }


        void PerformThirdAttack()
        {
            Debug.Log("Wizard AttackR");

            // Check if WizardIce prefab is assigned
            if (WizardLightning == null)
            {
                Debug.LogError("WizardLightning prefab is not assigned! Please assign it in the Inspector.");
                return;
            }
            StartCoroutine("InstantiateLightning");
        }

        void PerformSecondAttack2(){
            Vector3 spawnPosition2 = transform.position + new Vector3(0f, 0f, 0);
            GameObject attackECollider2 = Instantiate(attackE, spawnPosition2, Quaternion.identity);
            attackECollider2.transform.parent = gameObject.transform;
            attackECollider2.transform.localScale = new Vector3(1f, 1f, 0);
            attackECollider2.SetActive(true);
            Destroy(attackECollider2, 0.35f);
            Debug.Log("Wizard AttackE2");
        }

        void PlayWizardQSound()
        {
            if (soundManager != null)
            {
                soundManager.PlayWizardQ();
            }
        }

        void PlayWizardESound()
        {
            if (soundManager != null)
            {
                soundManager.PlayWizardE();
            }
        }

        void PlayWizardRSound()
        {
            if (soundManager != null)
            {
                soundManager.PlayWizardR();
            }
        }


        void ResetAttackState()
        {
            currentState = PlayerState.Idle;
        }

    private void UpdateAnimatorStates()
    {
        animator.SetBool("IsIdle", currentState == PlayerState.Idle);
        animator.SetBool("IsWalking", currentState == PlayerState.Walking);
        animator.SetBool("IsAttacking", currentState == PlayerState.Attacking);
        animator.SetBool("IsJumping", currentState == PlayerState.Jumping);
    }
}
