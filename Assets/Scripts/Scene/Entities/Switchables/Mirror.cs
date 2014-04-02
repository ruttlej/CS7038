﻿using System;
using UnityEngine;

public class Mirror : Switchable
{
    /// <summary>
    /// Indicates the direction of the mirror. If true, laser coming from top is reflected to right.
    /// </summary>
    private bool forward;
    public bool Forward
    {
        get { return forward; }
        set
        {
            forward = value;
            spriteRenderer.sprite = forward ? MirrorForward : MirrorInverse;
        }
    }

	public Sprite MirrorForward;
	public Sprite MirrorInverse;

    public Mirror()
    {
        forward = true;
    }

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
    }

    public Direction Reflect(Direction incoming)
    {
        switch (incoming)
        {
            case Direction.Up:
                return Forward ? Direction.Left : Direction.Right;
            case Direction.Down:
                return Forward ? Direction.Right : Direction.Left;
            case Direction.Left:
                return Forward ? Direction.Up : Direction.Down;
            case Direction.Right:
                return Forward ? Direction.Down : Direction.Up;
            default:
                throw new Exception("Impossible");
        }
    }

    protected override void Update()
    {
        base.Update();
    }

	public override void Switch()
	{
        audioManager.PlaySFX("Mirror");

		Forward = !Forward;

        playerHand.SpoilHand(-0.5f, GetInstanceID());
	}
}