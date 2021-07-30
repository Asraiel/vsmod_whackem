namespace WhackEm.ModSystem.Entity.Behavior
{
    using System.Collections.Generic;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.GameContent;
#pragma warning disable IDE0005
    using System.Linq;
#pragma warning restore IDE0005

    public class BehaviorShepardsStick : EntityBehavior
    {

        public BehaviorShepardsStick(Entity entity) : base(entity)
        {
        }

        public void OnHitByShepardsStick(int toolMode)
        {
            var behEmo = this.entity.GetBehavior<EntityBehaviorEmotionStates>();
            switch (toolMode)
            {
                case 0: // anger
                    behEmo.TryTriggerState("aggressiveondamage", 0);
                    break;
                case 1: // scare
                    behEmo.TryTriggerState("fleeondamage", 0);
                    break;
                case 2: // calm
                    this.KillAllActiveStates(behEmo);
                    break;
                default:
                    break;
            }
        }

        private void KillAllActiveStates(EntityBehaviorEmotionStates behEmo)
        {
            for (var i = 0; i < 4; i++)
            {
                try
                {
                    var dur = behEmo.ActiveStatesById[i];
                    if (dur > 0)
                    {
                        behEmo.ActiveStatesById[i] = 0;
                    }
                }
                catch (KeyNotFoundException)
                {
                    // ignore
                }
            }
        }

        public override string PropertyName()
        {
            return "shepardsstick";
        }
    }
}
