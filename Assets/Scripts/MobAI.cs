using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobAI : MonoBehaviour
{

    public string EnemyType;

    // Animator boo
    [Header("Animator Parameters")]
    public string isRunningBool = "IsRunning";
    public string isHurtBool = "IsHurt";
    public string isAttackingBool = "IsAttacking";
    public string isIdleBool = "IsIdle";
    public string isDeadBool = "IsDead";

    // Patrol boundaries
    [Header("Patrol Settings")]
    public float minXPosition;
    public float maxXPosition;
    public float patrolSpeed = 2f;

    // Chase settings
    [Header("Chase Settings")]
    public float chaseSpeed = 4f;
    public float attackRange = 1f; // Horizontal distance at which the enemy stops and attacks
    public float attackVerticalTolerance = 1.5f; // Maximum vertical distance difference to allow attack (if player is higher than this, don't attack)
    public float attackDelay = 1f; // Delay before attacking

    // Hurt settings
    [Header("Hurt Settings")]

    public int health;
    public float hurtDuration = 0.5f; // Duration of the hurt state
    private bool isHurt = false; // Tracks if the boss is currently hurt
    private float hurtTimer = 0f; // Timer for hurt duration

    // Attack trigger
    [Header("Attack Settings")]
    public Collider2D attackTrigger; // Drag your attack trigger collider here
    
    [Header("Detection Settings")]
    [Tooltip("The enemy needs a trigger collider (separate from attackTrigger) to detect the player's 'Chase' tagged object (PlayerAttraction). This detection collider should be larger than the attack trigger.")]
    // Note: The detection collider is set up in Unity Inspector - it should be a trigger collider on the enemy GameObject or a child object

    // Private variables
    private Animator animator;
    private Rigidbody2D rb;
    private Transform playerTransform;
    public bool isChasing = false;
    public bool movingRight = true; // Tracks the direction of patrol movement
    public bool isAttacking = false; // Tracks if the boss is currently attacking
    private float attackTimer = 0f; // Timer for attack delay

    public int Damage;

    void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Disable attack trigger initially
        if (attackTrigger != null)
        {
            attackTrigger.enabled = false;
        }

        // Set initial animation state
        UpdateAnimationState();
    }

    void Update()
    {
        // Handle hurt state
        if (isHurt)
        {
            hurtTimer += Time.deltaTime;
            if (health <= 0)
            {
                Die();
            }
        }

        if (hurtTimer >= hurtDuration)
        {
            isHurt = false;
            hurtTimer = 0f;

            // Reset animation after hurt
            animator.SetBool(isHurtBool, false);
            UpdateAnimationState();
        }

        // Ensure the enemy doesn't move while attacking
        if (isAttacking)
        {
            rb.velocity = Vector2.zero; // Stop all movement
            
            // Face the player while attacking
            if (playerTransform != null)
            {
                float direction = (playerTransform.position.x - transform.position.x);
                if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
                {
                    Flip();
                }
            }
            
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackDelay)
            {
                AttackPlayer();
                attackTimer = 0f;
                isAttacking = false;
            }
        }
        else if (isChasing && !isHurt)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("[MobAI] Update: isChasing is true but playerTransform is null! Resetting chase state.");
                isChasing = false;
            }
            else
            {
                ChasePlayer();
            }
        }
        else if (!isHurt)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // Check if the enemy has reached the boundaries (using world position)
        if (movingRight && transform.position.x >= maxXPosition)
        {
            Flip();
        }
        else if (!movingRight && transform.position.x <= minXPosition)
        {
            Flip();
        }

        // Move the enemy in the current direction
        float horizontalMovement = movingRight ? patrolSpeed : -patrolSpeed;
        rb.velocity = new Vector2(horizontalMovement, rb.velocity.y);

        // Update animation state
        animator.SetBool(isRunningBool, true);
        animator.SetBool(isIdleBool, false);
    }

    void ChasePlayer()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[MobAI] ChasePlayer called but playerTransform is null!");
            isChasing = false;
            rb.velocity = Vector2.zero;
            UpdateAnimationState();
            return;
        }

        // Calculate distances to the player
        float horizontalDistance = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float verticalDistance = playerTransform.position.y - transform.position.y;

        // If player is too high above the enemy AND within attack range, don't attack but keep chasing
        // The enemy will continue tracking but won't attack until player comes down
        if (verticalDistance > attackVerticalTolerance && horizontalDistance <= attackRange)
        {
            // Player is above and within attack range, don't attack but continue tracking
            // Face the player
            float direction = (playerTransform.position.x - transform.position.x);
            if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
            {
                Flip();
            }
            
            rb.velocity = Vector2.zero; // Stop moving since we're in attack range
            animator.SetBool(isRunningBool, false);
            animator.SetBool(isIdleBool, true);
            return; // Don't attack, but keep isChasing = true so we continue tracking
        }

        // If within attack range and on same level (or slightly below), stop and prepare to attack
        // Allow attack if player is at same level OR below (negative vertical distance is always OK)
        // Only prevent attack if player is significantly above (more than tolerance)
        if (horizontalDistance <= attackRange && (verticalDistance <= attackVerticalTolerance || verticalDistance < 0))
        {
            Debug.Log($"[MobAI] Attack conditions met! Horizontal: {horizontalDistance}, Vertical: {verticalDistance}, AttackRange: {attackRange}, Tolerance: {attackVerticalTolerance}");
            
            // Face the player before attacking
            float direction = (playerTransform.position.x - transform.position.x);
            if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
            {
                Flip();
            }

            rb.velocity = Vector2.zero; // Stop moving
            animator.SetBool(isRunningBool, false);
            animator.SetBool(isIdleBool, true);

            // Trigger attack after a delay
            if (!isAttacking)
            {
                Debug.Log("[MobAI] Setting isAttacking to true");
                isAttacking = true;
                attackTimer = 0f; // Reset timer when starting attack
            }
        }
        else
        {
            // Debug why attack isn't happening (only log occasionally to avoid spam)
            if (horizontalDistance <= attackRange && Time.frameCount % 60 == 0) // Log every 60 frames
            {
                Debug.Log($"[MobAI] Within horizontal range ({horizontalDistance} <= {attackRange}) but vertical check failed: {verticalDistance} > {attackVerticalTolerance}");
            }
            
            // Otherwise, continue chasing
            float direction = (playerTransform.position.x - transform.position.x);
            float horizontalMovement = direction > 0 ? chaseSpeed : -chaseSpeed;

            // Move towards the player
            rb.velocity = new Vector2(horizontalMovement, rb.velocity.y);

            // Flip the sprite based on direction
            if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
            {
                Flip();
            }

            // Update animation state
            animator.SetBool(isRunningBool, true);
            animator.SetBool(isIdleBool, false);
        }
    }

    void Flip()
    {
        // Flip the sprite by scaling the x-axis
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void UpdateAnimationState()
    {
        // Reset all animation states
        animator.SetBool(isRunningBool, false);
        animator.SetBool(isHurtBool, false);
        animator.SetBool(isAttackingBool, false);
        animator.SetBool(isIdleBool, true);
        animator.SetBool(isDeadBool, false);
    }

    void AttackPlayer()
    {
        Debug.Log("[MobAI] AttackPlayer() called!");
        
        // Ensure enemy is facing the player before attacking
        if (playerTransform != null)
        {
            float direction = (playerTransform.position.x - transform.position.x);
            if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
            {
                Flip();
            }
        }

        // Trigger the attack animation
        animator.SetBool(isAttackingBool, true);
        animator.SetBool(isIdleBool, false);
        
        Debug.Log("[MobAI] Attack animation triggered! isAttackingBool set to true");

        // Optionally, you can add logic here to deal damage to the player
        // Debug.Log("Enemy is attacking the player!");

        // Reset the attack animation after it finishes
        Invoke("ResetAttackAnimation", 1f); // Adjust this delay to match your attack animation length
    }

    void ResetAttackAnimation()
    {
        // Reset the attack animation state
        animator.SetBool(isAttackingBool, false);
        animator.SetBool(isIdleBool, true);
        
        // Ensure attack trigger is disabled
        if (attackTrigger != null)
        {
            attackTrigger.enabled = false;
        }
    }

    // Animation Event Methods - Called by Animation Events
    public void ActivateAttackTrigger()
    {
        if (attackTrigger != null)
        {
            attackTrigger.enabled = true;
        }
    }

    public void DeactivateAttackTrigger()
    {
        if (attackTrigger != null)
        {
            attackTrigger.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Enemy" || other.gameObject.tag == "Player") {
            gameObject.GetComponent<PolygonCollider2D>().enabled = false;
            other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
        }  

        // Check if the colliding object is the Chase trigger (PlayerAttraction)
        if (other.CompareTag("Chase"))
        {
            Debug.Log($"[MobAI] OnTriggerEnter2D: Chase tag detected! GameObject: {other.gameObject.name}");
            // Get the player transform (parent of the Chase object)
            Transform player = other.transform.parent;
            if (player != null)
            {
                Debug.Log($"[MobAI] Player parent found: {player.name}, setting up chase. isChasing will be set to true");
                playerTransform = player;
                isChasing = true;
                
                // Face the player immediately when starting to chase
                float direction = (player.position.x - transform.position.x);
                if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
                {
                    Flip();
                }
                
                animator.SetBool(isRunningBool, true);
                animator.SetBool(isIdleBool, false);
                
                PolygonCollider2D polyCollider = gameObject.GetComponent<PolygonCollider2D>();
                if (polyCollider != null)
                {
                    polyCollider.enabled = true;
                }
            }
            else
            {
                Debug.LogWarning($"[MobAI] Chase object detected but player parent is null! Chase object: {other.gameObject.name}, Parent: {other.transform.parent}");
            }
            return; // Exit early to avoid checking other tags
        }

        // Legacy support: Check if the colliding object is the player directly
        if (other.CompareTag("Player") && other.gameObject.GetComponent<BoxCollider2D>() != null && other.gameObject.GetComponent<BoxCollider2D>().enabled)
        {
            playerTransform = other.transform;
            isChasing = true;
            animator.SetBool(isRunningBool, true);
            animator.SetBool(isIdleBool, false);
        }
        if ((other.gameObject.CompareTag("Lightning") || other.gameObject.CompareTag("FireBall") || other.gameObject.CompareTag("IceSpear") || other.gameObject.CompareTag("FighterQ") || other.gameObject.CompareTag("FighterR") || other.gameObject.CompareTag("Ranger Arrow") || other.gameObject.CompareTag("RangerE") || other.gameObject.CompareTag("FighterE")) && !isHurt)
        {
            if (other.gameObject.CompareTag("Lightning"))
            {
                health -= 45;
            }
            else if (other.gameObject.CompareTag("FireBall"))
            {
                health -= 25;
            }
            else if (other.gameObject.CompareTag("IceSpear"))
            {
                health -= 20;
            }
            else if (other.gameObject.CompareTag("FighterQ"))
            {
                health -= 25;   
            }
            else if (other.gameObject.CompareTag("FighterE"))
            {
                health -= 20;
            }
            else if (other.gameObject.CompareTag("FighterR"))
            {
                health -= 40;
            }
            else if (other.gameObject.CompareTag("Ranger Arrow"))
            {
                health -= 20;
            }
            else if (other.gameObject.CompareTag("RangerE"))
            {
                health -= 15;
            }

            GetHurt();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if the colliding object is the Chase trigger (PlayerAttraction)
        if (other.CompareTag("Chase"))
        {
            // Get the player transform (parent of the Chase object)
            Transform player = other.transform.parent;
            if (player != null)
            {
                // Continuously update player position and ensure we're still chasing
                playerTransform = player;
                isChasing = true;
                
                // Face the player while chasing
                float direction = (player.position.x - transform.position.x);
                if ((direction > 0 && !movingRight) || (direction < 0 && movingRight))
                {
                    Flip();
                }
                
                // Only update animation if not attacking
                if (!isAttacking)
                {
                    animator.SetBool(isRunningBool, true);
                    animator.SetBool(isIdleBool, false);
                }
                
                PolygonCollider2D polyCollider = gameObject.GetComponent<PolygonCollider2D>();
                if (polyCollider != null)
                {
                    polyCollider.enabled = true;
                }
            }
            return; // Exit early to avoid checking other tags
        }
        
        if (other.gameObject.tag == "Enemy")
        {
            gameObject.GetComponent<PolygonCollider2D>().enabled = false;
            other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Check if the colliding object is a weapon (Melee or Range)

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object is the Chase trigger (PlayerAttraction)
        if (other.CompareTag("Chase"))
        {
            playerTransform = null;
            isChasing = false;
            animator.SetBool(isRunningBool, false);
            animator.SetBool(isIdleBool, true);
            return; // Exit early to avoid checking other tags
        }
        
        // Legacy support: Check if the exiting object is the player directly
        if (other.CompareTag("Player"))
        {
            playerTransform = null;
            isChasing = false;
            animator.SetBool(isRunningBool, false);
            animator.SetBool(isIdleBool, true);
        }
        
        if (other.gameObject.tag == "Enemy")
        {
            gameObject.GetComponent<PolygonCollider2D>().enabled = true;
            other.gameObject.GetComponent<PolygonCollider2D>().enabled = true;
        }
    }

    void GetHurt()
    {
        // Stop movement and play the hurt animation
        rb.velocity = Vector2.zero;
        isHurt = true;

        // Play the hurt animation only once
        animator.SetBool(isHurtBool, true);
        animator.SetBool(isRunningBool, false);
        animator.SetBool(isIdleBool, false);

        // Optionally, you can add logic here to reduce the boss's health
        //Debug.Log("Boss is hurt!");
    }

    void OnDrawGizmos()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol boundaries
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(minXPosition, transform.position.y, 0), new Vector3(maxXPosition, transform.position.y, 0));
        Gizmos.DrawSphere(new Vector3(minXPosition, transform.position.y, 0), 0.2f);
        Gizmos.DrawSphere(new Vector3(maxXPosition, transform.position.y, 0), 0.2f);
    }

    void Die()
    {
        animator.SetBool(isDeadBool, true);
        Destroy(gameObject);
    }
}