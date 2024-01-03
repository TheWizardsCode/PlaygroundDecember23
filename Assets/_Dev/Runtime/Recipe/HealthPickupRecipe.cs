using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(fileName = "Health Pickup Recipe", menuName = "Playground/Recipe/Health Pickup", order = 115)]
    public class HealthPickupRecipe : ItemRecipe<HealthPickup>
    {
        [NonSerialized]
        private IHealthManager _healthManager;
        private IHealthManager healthManager
        {
            get
            {
                if (_healthManager == null)
                {
                    _healthManager = FpsSoloCharacter.localPlayerCharacter.GetComponent<IHealthManager>();
                }
                return _healthManager;
            }
        }

        public override void Reset()
        {
            _healthManager = null;
            base.Reset();
        }

        public override bool ShouldBuild
        {
            get
            {
                float missingHealth = healthManager.healthMax - healthManager.health;
                return missingHealth > 0;
            }
        }
    }
}