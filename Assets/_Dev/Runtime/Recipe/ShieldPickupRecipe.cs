using NeoFPS;
using NeoFPS.SinglePlayer;
using UnityEngine;

namespace Playground
{
    /// <summary>
    /// This will add a shield manager to the player if they don't have one, and if they do, it will recharge a depleted shield.
    /// </summary>
    [CreateAssetMenu(fileName = "Shield Pickup Recipe", menuName = "Playground/Recipe/Shield Pickup", order = 110)]
    public class ShieldPickupRecipe : ItemRecipe<ShieldPickup>
    {
        private ShieldManager _shieldMgr;

        private ShieldManager shieldManager
        {
            get
            {
                if (_shieldMgr == null)
                {
                    _shieldMgr = FpsSoloCharacter.localPlayerCharacter.GetComponent<ShieldManager>();
                }
                return _shieldMgr;
            }
        }

        public override void Reset()
        {
            _shieldMgr = null;
            base.Reset();
        }

        public override bool ShouldBuild
        {
            get
            {
                if (shieldManager == null)
                {
                    return true;
                }

                return shieldManager.shieldState != ShieldState.Recharging
                    && shieldManager.shield < (shieldManager.shieldStepCount * shieldManager.shieldStepCapacity)
                    && base.ShouldBuild;
            }
        }
    }
}