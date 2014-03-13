using UnityEngine;
using System.Collections;

public class Fountain : Accessible
{
	
	// Use this for initialization
	protected override void Start()
	{
		base.Start();
	}
	
	public override bool Enter()
	{
		audioManager.PlaySFX("Fountain");

		var player = GameObject.FindObjectOfType<HandController>();
		player.value = HandController.MaxValue;

		return true;
	}
}