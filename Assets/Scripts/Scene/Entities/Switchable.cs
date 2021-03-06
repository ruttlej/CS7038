﻿public abstract class Switchable : Entity
{
    protected Switchable()
    {
        ExplosionHandler = new ExplosionTask(this);
    }

    public abstract void Switch(bool byPlayer);

    public class ExplosionTask : EntityExplosionTask
    {
        public Switchable Switchable { get; private set; }

        public ExplosionTask(Switchable switchable)
        {
            Switchable = switchable;
            Delay = 0;
        }

        public override void Run()
        {
            var direction = Switchable.Position - ExplosionSource;
            direction.Normalize();
            
           Switchable.Switch(false);
        }

        public override bool Equals(Task other)
        {
            var explosionTask = other as ExplosionTask;
            return explosionTask != null && Switchable == explosionTask.Switchable;
        }
    }
}
