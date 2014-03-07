﻿using UnityEngine;
using System.Collections;

public class Gate : Accessible
{

    // TO BE CHANGED
    public Sprite gateOpen, gateClosed;
    //

    private bool locked;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        locked = true;
    }

    public override bool Enter()
    {
        if (locked)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void ToggleLock()
    {
        locked = !locked;

        spriteRenderer.sprite = locked ? gateClosed : gateOpen;
    }
}