﻿using UnityEngine;
using HandyGestures;
using System;
using Grouping;

public class PlayerController : MonoBehaviour, IPan
{
    private Transform player;
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private GameObject ashes;
	private Ashes _ashController;
    private Animator animator;

    private JoystickController joystick;

    public enum PlayerAnimState
    {
        Idle = 0,
        Walk = 1,
        Push = 2,
        Action = 3,
        Wash = 4,
        Treat = 5

    }

    private PlayerAnimState animState;

    public PlayerAnimState AnimState { get { return animState; } set { animState = value; } }

    private Timer actionTimer;
    private const float PLAYER_SPEED = 0.4f;
    private bool canSwitch;
    private bool canMove;
    private Vector2 lastSwitchDirection;
    private HandController hands;
    private Vector2 previousPosition;
    public Vector2 movement;
    private Vector2 nextMovement;

    public Vector2 NextDirection { get { return movement; } }

    public bool playerMoving;
    private Transform objectPushing;
    private Vector2 previousPushablePosition;
    private Timer timer;

    private bool Moving { get { return timer.running; } }

    public bool IsAlive { get; set; }

    void Awake()
    {
        player = transform;

        spriteRenderer = GetComponent<SpriteRenderer>();

        hands = GetComponent<HandController>();

        animator = GetComponent<Animator>();

        joystick = FindObjectOfType<JoystickController>();
    }

    void Start()
    {
        timer = new Timer(PLAYER_SPEED, CompleteMoving);
        timer.repeating = true;

        actionTimer = new Timer(PLAYER_SPEED, CompleteAction);

        var detector = FindObjectOfType<HandyDetector>();
        if (detector != null)
        {
			detector.defaultObject = transform;
        }

        previousPosition = player.position;

        animState = PlayerAnimState.Idle;

        canSwitch = true;
        canMove = true;

        GroupManager.main.group["Running"].Add(this);
		GroupManager.main.group["Running"].Add(animator);
		//GroupManager.main.group["Running"].Add(this, new GroupDelegator(null, ()=>{canMove = true;}, null));
        GroupManager.main.group["To Level Over"].Add(this, new GroupDelegator(null, GoBackToIdle, null));
		GroupManager.main.group["To Level Over"].Add(animator);

        // not sure which one is better.
        GroupManager.main.group["Dialogue"].Add(this, new GroupDelegator(null, GoBackToIdle, null));
		GroupManager.main.group["Dialogue"].Add(animator);

        KeyboardController.Instance.KeyboardEventHandler = this;

		ashes = Entity.Spawn(gameObject, ashes);
		_ashController = ashes.GetComponent<Ashes>();
		ashes.SetActive(false);

        IsAlive = true;
    }

    #region Gestures

    public void OnGesturePan(PanArgs args)
    {
        switch (args.state)
        {
            case PanArgs.State.Down:
                joystick.TargetPosition = new Vector2(args.position.x, Screen.height - args.position.y);
                joystick.IsHeld = true;
                break;

            case PanArgs.State.Move:
                float x = args.delta.x;
                float y = args.delta.y;

                joystick.TargetPosition = new Vector2(args.position.x, Screen.height - args.position.y);
                if (Math.Abs(x - y) >= 3.5f)
                {
                    nextMovement = Math.Abs(x) > Math.Abs(y) ? new Vector2(x < 0 ? 1 : -1, 0) : new Vector2(0, y < 0 ? 1 : -1);
                    joystick.CurrentDirection = nextMovement; 

                    playerMoving = true;
                }
                
                if (nextMovement != Vector2.zero && (canMove || nextMovement != movement) && !Moving && CanMove())
                {
                    timer.Reset();
                    canSwitch = true;
                    canMove = true;

                    playerMoving = true;
                }

                break;
            case PanArgs.State.Hold:
                break;
            case PanArgs.State.Interrupt:
            case PanArgs.State.Up:
                canSwitch = true;
                canMove = true;
                playerMoving = false;
                joystick.IsHeld = false;
                nextMovement = Vector3.zero;
                break;
            default:
                playerMoving = false;
                joystick.IsHeld = false;
                nextMovement = Vector3.zero;
                break;
        }
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sortingOrder = LevelLoader.PlaceDepth(player.position.y) + 1;//-Mathf.RoundToInt(4 * player.position.y) + 1;

		switch (LevelManager.instance.settings.GetFloor(player.position.x, player.position.y)) {
			case LevelSettings.FloorType.HeatPad:
				Die(GameWorld.LevelOverReason.ExplosionKilledPlayer);
				break;
		}


        if (hands.isInfected)
        {
            animator.SetTrigger("Infect");

            AudioManager.StopSFX("Heartbeat");
            AudioManager.PlaySFX("Player Infected");

            IsAlive = false;

            GameWorld.levelOverReason = GameWorld.LevelOverReason.PlayerInfected;

            return;
        }

        if (IsAlive)
        {
            timer.Update();
            actionTimer.Update();

            if (Moving)
            {
                var newPosition = previousPosition + timer.progress * movement;
                if (objectPushing != null)
                {
                    objectPushing.position += newPosition.xy0() - player.position;
                    animState = PlayerAnimState.Push;
                }
                else
                {
                    animState = PlayerAnimState.Walk;
                }

                player.position = newPosition;
            }
            else
            {
                if ((int)animState < 3)
                {
                    animState = PlayerAnimState.Idle;
                }
            }

            SetAnimationState(Moving ? movement : nextMovement);
            animator.SetInteger("State", (int)animState);

            if (lastSwitchDirection != nextMovement)
            {
                canSwitch = true;
            }
        }
    }

    private void SetAnimationState(Vector2 directionVector)
    {
        string direction = null;

        if (directionVector == new Vector2(0, -1))
        {
            direction = "Down";
        }
        else if (directionVector == new Vector2(0, 1))
        {
            direction = "Up";
        }
        else if (directionVector == new Vector2(1, 0))
        {
            direction = "Right";
        }
        else if (directionVector == new Vector2(-1, 0))
        {
            direction = "Left";
        }

        if (direction != null)
        {
            animator.CrossFade((animState != PlayerAnimState.Treat ? animState.ToString() : "Wash") + " " + direction, 0.0f);
        }

    }

    private bool CanMove()
    {
        objectPushing = null;

        movement = nextMovement;

        // Get the next position
        var nextPosition = previousPosition + nextMovement;

        // Check collisions
        var hit = Physics2D.Raycast(nextPosition, nextMovement, 0.0f);

        if (hit.collider == null)
            return true;

        //Debug.Log("Collided with " + hit.collider.name + " [" + hit.collider.tag + "].");

        switch (hit.collider.tag)
        {
            case "Wall":
                return false;

            case "Pushable":
                var pushable = hit.collider.GetComponent<Pushable>();
                var canPush = pushable.Push(nextMovement);

                if (canPush && hit.transform.name.StartsWith("Trolley"))
                {
                    StartAction();
                }

                canMove = canPush && pushable.MovingWithPlayer;
                if (canMove)
                {
                    objectPushing = pushable.transform;
                    previousPushablePosition = objectPushing.position;
                }

                return canMove;

            case "Collectible":
                var collectible = hit.collider.GetComponent<Collectible>();
                collectible.Collect();

                return true;

            case "Accessible":
                var accessible = hit.collider.GetComponent<Accessible>();

                return accessible.Enter();

            case "Switchable":
                if (canSwitch)
                {
                    Switchable switchable = hit.collider.GetComponent<Switchable>();
                    switchable.Switch(true);
					Debug.Log("Switchy!");

                    canSwitch = false;
                    lastSwitchDirection = nextMovement;

                    if (!switchable.name.StartsWith("Patient"))
                    {
                        StartAction();
                    }
                }
                return false;

            default:
                return true;
        }
    }

    private void CompleteMoving()
    {
        previousPosition += movement;
        player.position = previousPosition;

        if (objectPushing != null)
        {
            previousPushablePosition += movement;
            objectPushing.position = previousPushablePosition;
        }

        if (playerMoving && CanMove())
        {
            movement = nextMovement;
        }
        else
        {
            timer.Stop();

            objectPushing = null;
        }
    }

    private void StartAction()
    {
        animState = PlayerAnimState.Action;
        actionTimer.Reset();
    }

    private void CompleteAction()
    {
        if (animState == PlayerAnimState.Action)
        {
            animState = PlayerAnimState.Idle;
        }
    }

    private void GoBackToIdle()
    {
		//if (this == null) {
			//So, it has come to this...
	//		return;
	//	}
        if (IsAlive)
        {
            animState = PlayerAnimState.Idle;

            SetAnimationState(nextMovement);
            animator.SetInteger("State", (int)animState);
        }
        IsAlive = true;
        this.GetComponent<Collider2D>().enabled = true;
    }

    public void Die(GameWorld.LevelOverReason reason = GameWorld.LevelOverReason.LaserKilledPlayer)
    {
        IsAlive = false;
        this.GetComponent<Collider2D>().enabled = false;

        if (reason == GameWorld.LevelOverReason.Squashed)
        {
            GetComponent<Renderer>().enabled = false;
            animator.enabled = false;
            transform.localScale = new Vector3(
                transform.localScale.x * 0.1f,
                transform.localScale.y,
                transform.localScale.z);
        }
        else
        {
            animator.SetTrigger("Die");
			//ashes.SetActive(true);
			_ashController.Trigger(transform.position);
			Destroy(this.gameObject);
			//Entity.Replace(this.gameObject, ashes);

        }
        GameWorld.levelOverReason = reason;
    }
}
