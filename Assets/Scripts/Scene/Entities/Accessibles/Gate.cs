﻿using UnityEngine;

public class Gate : Accessible
{
    public LeverGateType LeverGateType;
    public LeverGateManager Manager;

	PlayerController drHandrew;

    private bool open;
    public virtual bool Open
    {
        get { return open; }
        set
        {
            open = value;
            collider2D.enabled = !open;
            spriteRenderer.sprite = open ? GateOpen : GateClosed;
        }
    }

    // TO BE CHANGED
    public Sprite GateOpen;
    public Sprite GateClosed;
    //


    // Use this for initialization
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
		spriteRenderer.sortingOrder = LevelLoader.PlaceDepth(transform.position.y) - LevelLoader.UsableOffset;
		if (drHandrew == null) {
			drHandrew = GameObject.FindObjectOfType<PlayerController>();
		} else {
			if (!Open && (transform.position - drHandrew.transform.position).magnitude < 0.1f) {
				Debug.Log("Squashed!");
				drHandrew.Die(GameWorld.LevelOverReason.Squashed);

			}
		}
    }

    public override bool Enter()
    {
        return Open;
    }
		
    public void SwitchState()
    {
        Open = !Open;
    }
}
