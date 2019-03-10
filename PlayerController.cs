using UnityEngine;

/*
 * A class for the player which determines how they control and react to the environment
 */
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour {

    [Header("Player Controls")]
    [SerializeField] [Tooltip("Mouse click number (0 = left, 1 = right, 2 = middle, etc)")]
    private int mouseButtonClickId = 0;
    [SerializeField] [Tooltip("Time between two clicks to count as a double click")]
    private float doubleClickTime = 0.2f;

    [Header("Movement")]
    [SerializeField] [Tooltip("How quickly the player accelerates")]
    private float accelerationIntensity = 30f;
    [SerializeField]
    private float maxVelocity = 30f;
    [SerializeField]
    private float maxDashVelocity = 50f;
    [SerializeField] [Tooltip("Speed at which the player will slow down to 0 velocity")]
    private float dragSpeed = 0.5f;
    [SerializeField] [Tooltip("If facing opposite direction of velocity, use this a accel boost (should have x values in a range of -1 to 1 with y values greater than 1)")]
    private AnimationCurve oppsiteAccelerationBoost = null;
    [SerializeField] [Tooltip("Speed the player to rotates towards the mouse at")]
    private float rotateSpeed = 10f;
    [SerializeField] [Tooltip("Speed dampen after bouncing off a wall")]
    private float bounceSpeedDampen = 0.5f;
    [SerializeField] [Tooltip("Time to disable the player from moving when hitting a wall")]
    private float disableMoveMaxTime = 0.3f;
    [SerializeField]
    private Vector3 outOfWaterGravity = new Vector3(0f, -30f, 0f);
    [SerializeField] [Tooltip("Water level which the player will begin using gravity after this point")]
    private float topWaterY = 15f;


    [Header("Ranking")]
    [SerializeField] [Tooltip("How the current combo rank influences the players speed/ accel")]
    private float maxVelocityRankMultiplier = 3f;
    [SerializeField] [Tooltip("How much acceleration increases based on the players current rank")]
    private float accelerationRankMultiplier = 0f;


    [Header("Other")]
    [SerializeField]
    private float dashEffectSpawnAwayDistance = 6f;



    private Animator animator;

    private Vector3 direction = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;
    private float currentMaxVelocity = 0f;

    private float disableMoveTimer = 0f; // Current time that the player is disabled
    private bool outOfWater = false;

    private Vector2 clickTimeHistory = Vector2.zero; // Past two click times
    private bool isClicking = false;
    private bool isDoubleClicking = false;


    /*
     * Gets the current players current velocity
     * 
     * @return Vector3 - player velocity
     */
    public Vector3 GetVelocity()
    {
        return velocity;
    }


    /*
     * Executed on start of scene containing gameobject with this PlayerController Component
     * Used to intialize the variables
     */
    private void Start () {
        animator = GetComponent<Animator> ();

        currentMaxVelocity = maxVelocity;
    }
    

    /*
     * Executed on every update of the game loop
     * Calls relevant methods to handle player control and other relevant details
     */
    private void Update () {

        // If the game is paused don't allow the player to do anything
        if (Time.timeScale == 0f) {
            return;
        }

        outOfWater = transform.position.y > topWaterY;

        direction = GameManager.Instance().GetCameraController().GetMouseTarget() - transform.position;
        direction.Normalize();

        HandleInput();

        HandleReflectonOnCollision();
        HandleRotation();

        SetAcceleration();
        SetVelocity();

        HandleDash();

        transform.position += velocity * Time.deltaTime;

        FaceSpriteInDirection();
        
        if (animator.enabled)
        {
            SetAnimatorState();
        }

    }


    /*
     * Handle user input, specifically clicking the mouse and setting state variables about whether or not they're clicking
     */
    private void HandleInput ()
    {
        isClicking = Input.GetMouseButton(mouseButtonClickId);
        isDoubleClicking = false;

        // If click down, update the past click timestamp queue
        if (Input.GetMouseButtonDown(mouseButtonClickId))
        {
            clickTimeHistory.y = clickTimeHistory.x;
            clickTimeHistory.x = Time.time;
        }

        if (clickTimeHistory.x > 0f && clickTimeHistory.y > 0f && clickTimeHistory.x - clickTimeHistory.y < doubleClickTime)
        {
            isDoubleClicking = true;
        }
    }


    /*
     * Handle a double click in the proper case to cause the player to perform a dash
     */
    private void HandleDash ()
    {
        if(disableMoveTimer <= 0f && !outOfWater && isDoubleClicking)
        {
            // Create Dash Effect
            GameObject dashEffectInstance = EffectManager.Instance().SpawnDashEffect();
            dashEffectInstance.transform.position = transform.position + dashEffectSpawnAwayDistance * direction.normalized;
            dashEffectInstance.transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z + 90f);

            // Screenshake
            CameraShaker.Instance().AddCameraShake(direction);

            // Reset the double click history
            clickTimeHistory = Vector2.zero;

            // Increase velocity cap to allow for more velocity only temporarily
            currentMaxVelocity = maxDashVelocity + (maxVelocityRankMultiplier * GameManager.Instance().GetCurrentRank());
            
            velocity = direction.normalized * currentMaxVelocity;
        }
    }


    /*
     * Set the players acceleration based on the relevant state variables
     */
    private void SetAcceleration ()
    {
        acceleration = Vector3.zero;

        if(disableMoveTimer <= 0f && !outOfWater && isClicking)
        {
            // If the player is changing direction, increase acceleration by multiplier
            float accelerationBoost = 1f + oppsiteAccelerationBoost.Evaluate( Vector3.Dot(velocity.normalized, direction.normalized) );
            
            acceleration = direction * accelerationBoost * accelerationIntensity;
        }
        else
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * dragSpeed);
        }

    }


    /*
     * Set the players velocity based on the relevant state variables
     */
    private void SetVelocity ()
    {
        float maxVelocityRestoreSpeed = 2f;


        // Handle moving in vs out of the water
        if (outOfWater)
        {
            velocity += Time.deltaTime * outOfWaterGravity;
            currentMaxVelocity = Mathf.Lerp(currentMaxVelocity, maxDashVelocity, Time.deltaTime * maxVelocityRestoreSpeed); // Set currentMaxVelocity back to default
        }
        else
        {
            // Velocity calculated from acceleration
            velocity += acceleration * Time.deltaTime;

            // Rank Based velocity boost
            velocity += acceleration.normalized * (accelerationRankMultiplier * GameManager.Instance().GetCurrentRank()) * Time.deltaTime;
            
            // Set currentMaxVelocity back to default
            currentMaxVelocity = Mathf.Lerp(
                currentMaxVelocity,
                maxVelocity + (maxVelocityRankMultiplier * GameManager.Instance().GetCurrentRank()),
                Time.deltaTime * maxVelocityRestoreSpeed
            );
        }


        if (velocity.magnitude > currentMaxVelocity)
        {
            velocity = velocity.normalized * currentMaxVelocity;
        }

        velocity.z = 0f;
    }


    /*
     * Sets the animator state based on whether or not the player is sitting still, swimming, or dashing
     */
    private void SetAnimatorState ()
    {
        animator.SetBool ("Swimming", velocity != Vector3.zero);
        animator.SetBool ("Dashing", velocity.magnitude > (maxVelocity + (maxVelocityRankMultiplier * GameManager.Instance().GetCurrentRank())));
    }


    /*
     * Handles player rotation based on the direction the player is looking
     */
    private void HandleRotation ()
    {
        transform.eulerAngles = new Vector3(
            0f,
            0f,
            Mathf.LerpAngle(
                transform.eulerAngles.z,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                Time.deltaTime * rotateSpeed
            )    
        );
    }


    /*
     * Handles player collisions with walls and reflects them in the opposite direction
     */
    private void HandleReflectonOnCollision ()
    {
        RaycastHit2D hit = Physics2D.Raycast(this.transform.position, velocity, velocity.magnitude * Time.deltaTime, 1 << 8);

        if(hit.collider != null) {
            velocity = bounceSpeedDampen * (velocity - (2 * Vector3.Dot(velocity, (Vector3)hit.normal) * (Vector3)hit.normal));
            disableMoveTimer = disableMoveMaxTime;
        }
    }


    /*
     * Faces the player sprite in the correct direction by flipping localscale y
     */
    private void FaceSpriteInDirection ()
    {
        if ((0 < velocity.x && 0 > transform.localScale.y) || (0 > velocity.x && 0 < transform.localScale.y))
            transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
    }

}
