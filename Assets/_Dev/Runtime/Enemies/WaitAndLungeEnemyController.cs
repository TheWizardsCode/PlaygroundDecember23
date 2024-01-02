using NaughtyAttributes;
using NeoFPS.SinglePlayer;
using UnityEngine;

namespace Playground
{
    /// <summary>
    /// This kind of enemy will wait until the player is close enough, then lunge at them.
    /// They will move towards the player slowly, but will not attack until they are close enough.
    /// </summary>
    public class WaitAndLungeEnemyController : BasicEnemyController
    {
        [SerializeField, Tooltip("Minimum distance to attack from. If the player is further away than this the enemy will not attack. Note that if require line of sight is true and view distance is less than this value then this value will have not effect."), Foldout("Behaviour")]
        protected float minAttackDistance = 20f;
        [SerializeField, Tooltip("The multiplier for speed when attacking."), Foldout("Behaviour")]
        protected float attackSpeedMultiplier = 2f;

        protected override void Update()
        {
            if (FpsSoloCharacter.localPlayerCharacter == null)
                return;

            if (shouldAttack)
            {
                MoveTowards(Target.position, attackSpeedMultiplier);
            } else
            {
                base.Update();
            }
        }

        internal override bool shouldAttack {
            get
            {
                if (Vector3.Distance(transform.position, FpsSoloCharacter.localPlayerCharacter.localTransform.position) < minAttackDistance)
                {
                    return base.shouldAttack;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}