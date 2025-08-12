using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem; // important

public class PlayerInputActionHandler : MonoBehaviour
{
    
    [Header("Vital Components")]
    //Inputs
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform player;
    
    [Header("Stats")]
    //Movement
    [SerializeField] float groundDecel = 10f; //Ground Slide amount
    [SerializeField] float landingInertia = 0.85f; // 0..1, how much air direction to keep on landing
    private bool wasGrounded;
    
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float airStrafeSpeed = 2f;
    [SerializeField] private float speed = 5f;
    
    [Header("Jump Components")]
    //Jump / Falling
    [SerializeField] float gravity = -20f;          // base gravity 
    [SerializeField] float jumpHeight = 2.5f;       // max height when fully held
    [SerializeField] float fallMultiplier = 2.2f;   // faster falls feel snappier
    [SerializeField] float lowJumpMultiplier = 4f;  // extra gravity when released early
    [SerializeField] float coyoteTime = 0.1f;       // grace time after leaving ground
    [SerializeField] float jumpBufferTime = 0.1f;   // buffer presses just before landing

    float jumpSpeed;             
    float coyoteTimer;
    float jumpBufferTimer;
    bool jumpHeld;
    
    //Velocities
    private Vector2 moveInput;

    private Vector2 storedInput;

    private Vector3 currentMoveDirection;
    
    [Header("Input Checks")]
    private Vector3 velocity;
    [SerializeField] private bool isRunning  = false; 
    [SerializeField] private bool isGrounded = false;
    [SerializeField] private Vector2 lookDirection; //x left/right -1/1   //y up/down -1/1 
    
    
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _playerMovement = new PlayerMovement();
        controller = GetComponent<CharacterController>();
        player = this.gameObject.transform;
        
        Cursor.lockState = CursorLockMode.Locked;
        
        /*
        //Movement
        _playerMovement._2DPlayerMovement.Move.performed += ctx => OnMove(ctx.ReadValue<Vector2>());
        
        _playerMovement._2DPlayerMovement.Jump.started += ctx => OnJump(ctx.ReadValueAsButton());
        _playerMovement._2DPlayerMovement.Jump.canceled += ctx => OnJump(ctx.ReadValueAsButton());
        
        _playerMovement._2DPlayerMovement.Run.performed += ctx => OnRun(ctx.ReadValueAsButton());
        _playerMovement._2DPlayerMovement.Run.canceled += ctx => OnRun(ctx.ReadValueAsButton());
        
        //Attacking
        _playerMovement._2DPlayerMovement.SimpleAttack.performed += ctx => OnSimpleAttack(ctx.ReadValueAsButton());
        //Interaction
        _playerMovement._2DPlayerMovement.Interact.performed += ctx => OnInteract(ctx.ReadValueAsButton());*/
        
        jumpSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
        
    }

    private void Update()
    {   
        
        //Handle Sprinting
        if(controller.isGrounded)
        {
            float lerpSpeed = 4f;
            if(isRunning)
            {
                this.speed = Mathf.Lerp(this.speed, sprintSpeed, lerpSpeed * Time.deltaTime);
            }
            else
            {
                this.speed = Mathf.Lerp(this.speed, walkSpeed, lerpSpeed * Time.deltaTime);
            }
            
        }
        
        //Move Player
        HandleMove();
    }
    
    
    //Variable jump height, depending on how long the button is pressed
    public void OnSimpleAttack(InputValue value)
    {
        if (value.isPressed)
            Debug.Log("Attacking");
    }

    public void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        moveInput = dir; 
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;   // start buffer only on press
        }
        else
        {
            jumpHeld = false;
            if (isGrounded) jumpBufferTimer = 0f; // prevent post-landing mini-jump
        }
    }

    public void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
            Debug.Log("Interacting");
    }

    private void HandleUpDown(float input)
    {
        Debug.Log(input);
        if (input > 0.7)
        {
            Debug.Log("Up");
        } else if (input < -0.7)
        {
            Debug.Log("Down");
        } else
        {
            Debug.Log("Neutral");
        }
    }
    
    private void HandleMove()
    {
        bool prevGrounded = wasGrounded;

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;

            // ease-out on ground
            storedInput.x = Mathf.Lerp(storedInput.x, moveInput.x, groundDecel * Time.deltaTime);
            if (Mathf.Abs(storedInput.x) < 0.01f) storedInput.x = 0f;

            currentMoveDirection = player.right * storedInput.x;
        }
        else
        {
            // in air: steer with air control
            Vector3 desiredMoveDirection = (player.right * moveInput.x).normalized;
            currentMoveDirection = Vector3.Lerp(currentMoveDirection, desiredMoveDirection, airStrafeSpeed * Time.deltaTime);
        }

        // horizontal move (don’t normalize on ground)
        Vector3 horizDir = isGrounded ? (player.right * storedInput.x) : currentMoveDirection;
        Vector3 horizMove = horizDir * speed * Time.deltaTime;
        CollisionFlags hFlags = controller.Move(horizMove);

        // anti wall-stick
        if ((hFlags & CollisionFlags.Sides) != 0)
        {
            if (Mathf.Sign(horizMove.x) == Mathf.Sign(moveInput.x) && Mathf.Abs(moveInput.x) > 0.01f)
            {
                currentMoveDirection.x = 0f;
                storedInput.x = 0f;
            }
        }

        // vertical
        HandleJump();
        CollisionFlags vFlags = controller.Move(velocity * Time.deltaTime);

        // Ceiling bonk / ground bias
        if ((vFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
        if ((vFlags & CollisionFlags.Below) != 0 && velocity.y < 0f) velocity.y = -2f;

        // --- landing inertia: if we just landed, seed storedInput from air direction ---
        bool justLanded = !prevGrounded && (vFlags & CollisionFlags.Below) != 0;
        if (justLanded)
        {
            // project current air move onto player-right to get a 1D scalar
            float airScalar = Vector3.Dot(currentMoveDirection, player.right);
            storedInput.x = Mathf.Lerp(storedInput.x, airScalar, landingInertia);
            currentMoveDirection = player.right * storedInput.x; // sync
        }

        // update prev for next frame
        wasGrounded = isGrounded;
        
        HandleUpDown(moveInput.y);
        
        lookDirection.x = moveInput.x;
        lookDirection.y = moveInput.y;
    }

    private void HandleJump()
    {
        // Coyote time
        if (isGrounded && velocity.y <= 0f) coyoteTimer = coyoteTime;
        else                                coyoteTimer -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

        // Consume buffered jump if still in coyote window
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpSpeed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // Gravity with variable-height behavior
        if (velocity.y < 0f)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else if (!jumpHeld)
            velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;
    }
    
}
