﻿using UnityEngine;
using System.Collections;

public class Sanitizer : Collectible
{

	// Use this for initialization
	protected override void Start()
	{
		base.Start();
	}

	public override void Collect()
	{
        AudioManager.PlaySFX("Collect");
		Execute(Trigger.ActionType.Handy);

        //playerHand.SpoilHand(1.0f, GetInstanceID());

		base.Collect();
	}
}
