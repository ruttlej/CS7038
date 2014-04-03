﻿using UnityEngine;

public class Plant : Crate
{
	public GameObject fire;
	public GameObject ashes;
	private Timer clock;

    public Plant()
    {
        ExplosionHandler = new ExplosionTask(this);
    }

	 void Start() {
		base.Start();
		clock = new Timer(1, Break);
		clock.Stop(); 
	}

	 void Update() {
		clock.Update();
	}

	public void Break() {
		var obj = Object.Instantiate(
			ashes, transform.position, transform.rotation) as GameObject;
		obj.GetComponent<SpriteRenderer>().sortingOrder = this.spriteRenderer.sortingOrder;
		obj.transform.parent = transform.parent;
		Destroy(this.gameObject);
		clock.Stop();
	}

	public void Burn()
	{
		Break();
		//clock.Resume();
		//Object.Instantiate(fire);
		//Destroy(this.gameObject, 1);
	}

    public new class ExplosionTask : EntityExplosionTask
    {
        public Plant Plant { get; private set; }

        public ExplosionTask(Plant plant)
        {
            Plant = plant;
            Delay = 0;
        }

        public override void Run()
        {
            var direction = Plant.Position - ExplosionSource;
            direction.Normalize();
            Plant.Break();
        }

        public override bool Equals(Task other)
        {
            var explosionTask = other as ExplosionTask;
            return explosionTask != null && Plant == explosionTask.Plant;
        }
    }
}
