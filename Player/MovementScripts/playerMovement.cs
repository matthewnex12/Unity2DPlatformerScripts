using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public float temporal;
    Rigidbody2D rb;
    Animator animator;
    [SerializeField] Collider2D standingCollider;
    [SerializeField] Transform groundCheckCollider;
    [SerializeField] Transform overheadCheckCollider;
    [SerializeField] LayerMask groundLayer;
    public ParticleSystem dust;
    public Transform wallCheck;


    const float groundCheckRadius = 0.1f;
    const float overheadCheckRadius = 0.2f;
    [Space]
    [Header("Stats")]
    [SerializeField] float speed = 2;
    [SerializeField] float jumpPower = 500;
    [SerializeField] int totalJumps;
    [SerializeField] float wallSlideSpeed;
    [SerializeField] float variableJumpHeightMulitplier = 0.5f;
    int availableJumps;
    float horizontalValue;
    float runSpeedModifier = 2f;
    [SerializeField]float crouchSpeedModifier = 0.5f;
    [Space]
    [Header("Wall Stuff")]

    [SerializeField] float wallCheckDistance;

    [Space]
    [Header("Booleans")]
    [SerializeField] bool isGrounded;
    bool isRunning;
    bool facingRight = true;
    bool crouchPressed;
    bool multipleJump;
    bool coyoteJump;
    bool isTouchingWall;
    bool isWallSliding;

    void Awake()
    {
        availableJumps = totalJumps;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        CheckInput();
        CheckIfWallSliding();
    }

    private void CheckInput()
    {
        //Store the horizontal value
        horizontalValue = Input.GetAxisRaw("Horizontal");

        //If LShift is clicked enable isRunning
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Joystick1Button4))
            isRunning = true;
        //If LShift is released disable isRunning
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.Joystick1Button4))
            isRunning = false;

        //If we press Jump button enable jump 
        if (Input.GetButtonDown("Jump"))
            Jump();

        if (Input.GetButtonUp("Jump"))
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeightMulitplier);
        }

        //If we press Crouch button enable crouch 
        if (Input.GetButtonDown("Crouch"))
            crouchPressed = true;


        //Otherwise disable it
        else if (Input.GetButtonUp("Crouch"))
            crouchPressed = false;

        //Set the yVelocity Value
        animator.SetFloat("yVelocity", rb.velocity.y);

    }

    void FixedUpdate()
    {
        
        Move(horizontalValue, crouchPressed);
        CheckSurroundings();
    }

    private void CheckIfWallSliding()
    {
        if(isTouchingWall && !isGrounded && rb.velocity.y <0)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }
    private void CheckSurroundings()
    {
        GroundCheck();
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, groundLayer);
    }

    void GroundCheck()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;
        //Check if the GroundCheckObject is colliding with other
        //2D Colliders that are in the "Ground" Layer
        //If yes (isGrounded true) else (isGrounded false)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckCollider.position, groundCheckRadius, groundLayer);
        if (colliders.Length > 0)
        {
            isGrounded = true;
            if (!wasGrounded)
            {
                availableJumps = totalJumps;
                multipleJump = false;
            }
        }
        else
        {
            if (wasGrounded)
                StartCoroutine(CoyoteJumpDelay());
        }

        //As long as we are grounded the "Jump" bool
        //in the animator is disabled
        animator.SetBool("Jump", !isGrounded);
    }

    IEnumerator CoyoteJumpDelay()
    {
        coyoteJump = true;
        yield return new WaitForSeconds(0.2f);
        coyoteJump = false;
    }


    void Jump()
    {
        if (isGrounded)
        {
            multipleJump = true;
            availableJumps--;

            rb.velocity = Vector2.up * jumpPower;
            animator.SetBool("Jump", true);
            CreateDust();

        }
        //Wall hop
        else if(isWallSliding && !isGrounded)
        {
            rb.velocity = new Vector2((-transform.localScale.x)* jumpPower, jumpPower);
            animator.SetBool("Jump", true);
            CreateDust();
            isWallSliding = false;
        }
        else
        {
            if (coyoteJump)
            {
                multipleJump = true;
                availableJumps--;

                rb.velocity = Vector2.up * jumpPower;
                animator.SetBool("Jump", true);
                CreateDust();
            }

            if (multipleJump && availableJumps > 0)
            {
                availableJumps--;

                rb.velocity = Vector2.up * jumpPower;
                animator.SetBool("Jump", true);
                CreateDust();
            }
        }
    }

    void CreateDust()
    {
        dust.Play();
    }

    void Move(float dir, bool crouchFlag)
    {
        #region Crouch

        //If we are crouching and disabled crouching
        //Check overhead for collision with Ground items
        //If there are any, remain crouched, otherwise un-crouch
        if (!crouchFlag)
        {
           if (Physics2D.OverlapCircle(overheadCheckCollider.position, overheadCheckRadius, groundLayer))
                crouchFlag = true;
        }

        animator.SetBool("Crouch", crouchFlag);
        standingCollider.enabled = !crouchFlag;
        #endregion

        #region Move & Run
        //Set value of x using dir and speed
        float xVal = dir * speed * 100 * Time.fixedDeltaTime;
        //If we are running mulitply with the running modifier (higher)
        if (isRunning)
            xVal *= runSpeedModifier;
        //If we are running mulitply with the running modifier (higher)
        if (crouchFlag)
           xVal *= crouchSpeedModifier;
        //Create Vec2 for the velocity
        Vector2 targetVelocity = new Vector2(xVal, rb.velocity.y);
        //Set the player's velocity
        rb.velocity = targetVelocity;

        //If looking right and clicked left (flip to the left)
        if (facingRight && dir < 0)
        {
            if(isGrounded)
            {
                CreateDust();
            }
            if (!isWallSliding)
            {
                transform.Rotate(0f, 180f, 0f); ;
                facingRight = false;
            }

        }
        //If looking left and clicked right (flip to rhe right)
        else if (!facingRight && dir > 0)
        {
            if (isGrounded)
            {
                CreateDust();
            }
            if(!isWallSliding)
            {
                transform.Rotate(0f, 180f, 0f);
                facingRight = true;
            }

        }

        //(0 idle , 4 walking , 8 running)
        //Set the float xVelocity according to the x value 
        //of the RigidBody2D velocity 
        animator.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));

        //Wall Sliding
        if(isWallSliding)
        {
            if(rb.velocity.y < -wallSlideSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }
        animator.SetBool("isWallSliding", isWallSliding);
        #endregion
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y, wallCheck.position.z));
    }
}