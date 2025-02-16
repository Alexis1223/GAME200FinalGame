using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlatformPlayerController : Observer
{
    private float jumpHeight;
    private float runningSpeed;
    private float inputRate;
    private float coyoteTime;
    private Vector2 groundCheckBoxSize;
    private GameObject fireObj;
    private float inputCacheTime;
    
    public float jumpGravityScale{ get; private set; }
    public float initGravityScale{ get; private set; }
    public float gravityFalling{ get; private set; }
    public float ascendingGravityStep{ get; private set; }
    public float runningAcceleration{ get; private set; }
    public int smallestJumpFrameCount{ get; private set; }
    public float runningBrakingAbility{ get; private set; }
    
    [Header("Player Configuration")] 
    public SO_PlayerConfig playerConfig;
    
    public Rigidbody2D rig { get; private set; }
    public Animator animator{ get; private set; }
    public GameObject spriteObj{ get; private set; }
    public GameObject walkingParticle{ get; private set; }
    public GameObject groundCheckPoint { get; private set; }
    public PlayerFSM stateMachine{ get; private set; }
    public bool isDie { get; private set; } = false;
    public bool isIgnited { get; private set; } = false;
    public SO_AudioConfig jumpAudio{ get; private set; }
    public SO_AudioConfig deathAudio{ get; private set; }

    private bool isJumpInput = false;

    [HideInInspector]
    public float groundSpeed;
    [HideInInspector]
    public bool isCoyote = false;
    
    //private bool isPulling = false;
    public GameObject blackhole;
    public Vector2 velo;

    protected void Awake()
    {
        jumpHeight = playerConfig.jumpHeight;
        runningSpeed = playerConfig.runningSpeed;
        inputRate = playerConfig.inputRate;
        coyoteTime = playerConfig.coyoteTime;
        initGravityScale = playerConfig.initGravityScale;
        //jumpAudio = playerConfig.jumpSoundAsset;
        //deathAudio = playerConfig.dieSoundAsset;
        groundCheckBoxSize = playerConfig.groundCheckBoxSize;
        jumpGravityScale = playerConfig.jumpGravityScale;
        inputCacheTime = playerConfig.inputCacheTime;
        gravityFalling = playerConfig.gravityFalling;
        ascendingGravityStep = playerConfig.ascendingGravityStep;
        runningAcceleration = playerConfig.runningAcceleration;
        smallestJumpFrameCount = playerConfig.smallestJumpFrameCount;
        runningBrakingAbility = playerConfig.runningBrakingAbility;
        
        rig = GetComponent<Rigidbody2D>();
        groundCheckPoint = transform.Find("GroundCheckPoint").gameObject;
        //walkingParticle = transform.Find("Walk Particle").gameObject;
        spriteObj = transform.Find("sprite").gameObject;
        //fireObj = transform.Find("Fire Particle").gameObject;
        //animator = transform.Find("sprite").GetComponent<Animator>();
        stateMachine = new PlayerFSM(this);
        rig.gravityScale = initGravityScale;
        

    }
    void Start()
    {
        //blackhole = Resources.Load("Prefabs/BlackHole.prefab", typeof(GameObject)) as GameObject;
    }
    
    void Update()
    {
        stateMachine.currentState.HandleUpdate();
        velo = rig.velocity;
    }

    private void FixedUpdate()
    {
        // HandleJump();
        stateMachine.currentState.HandleFixedUpdate();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        stateMachine.currentState.HandleCollide2D(col);
    }

    private void OnDestroy()
    {

        stateMachine.DestroySelf();
    }

    private float prevInputX;
    public void HorizontalMove()
    {
        float inputX = Input.GetAxis("Horizontal");
        float speed;
        if (inputX < 0)
        {
            speed = Mathf.Max(Input.GetAxis("Horizontal") * runningAcceleration, -1f) * runningSpeed;   
        }
        else
        {
            speed = Mathf.Min(Input.GetAxis("Horizontal") * runningAcceleration, 1f) * runningSpeed;
        }

        if (Mathf.Abs(inputX) - Mathf.Abs(prevInputX) < 0)
        {
            speed /= runningBrakingAbility;
        }
        rig.velocity = new Vector2(speed, rig.velocity.y);
        prevInputX = inputX;
    }

    public void HorizontalJumpingMove()
    {
        float speed = Input.GetAxis("Horizontal") * runningSpeed * inputRate + groundSpeed * (1f - inputRate);
        rig.velocity = new Vector2(speed, rig.velocity.y);
    }



    public void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isJumpInput)
        {
            isJumpInput = true;
            StartCoroutine(ClearInput());
        }
        IEnumerator ClearInput()
        {
            yield return new WaitForSeconds(inputCacheTime);
            isJumpInput = false;
        }
    }


    public void HandleBlackHoleInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (GameObject.FindGameObjectsWithTag("BlackHole").Length == 0)
            {
                Instantiate(blackhole, GameManager.instance.mousePos3, Quaternion.identity);
            }
            else if (GameObject.FindGameObjectsWithTag("BlackHole").Length > 0)
            {
                GameObject.FindGameObjectsWithTag("BlackHole")[0].transform.position = GameManager.instance.mousePos3;
            }
            
        }
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            EventManager.SendNotification(EventName.BlackHolePull);
        }
    }

    public IEnumerator RecoverCoyote()
    {
        yield return new WaitForSeconds(coyoteTime);
        isCoyote = false;
    }
    
    public bool IsOnGround()
    {
        bool isGrounded = false;
        Collider2D[] cols = Physics2D.OverlapBoxAll(groundCheckPoint.transform.position, groundCheckBoxSize, 0);
        foreach (var item in cols)
        {
            if (item.CompareTag("Floor"))
            {
                isGrounded = true;
                break;
            }
        }
        return isGrounded;
    }

    public bool HandleJump()
    {
        if (isJumpInput && (IsOnGround() || isCoyote))
        {
            isCoyote = false;
            isJumpInput = false;
            rig.velocity = new Vector2(rig.velocity.x, jumpHeight);
            return true;
        }
        return false;
    }
}
