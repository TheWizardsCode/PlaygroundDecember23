using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(fileName = "Health Pickup Recipe", menuName = "Playground/Health Pickup Recipe")]
    public class HealthPickupRecipe : Recipe<HealthPickup>
    {   
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