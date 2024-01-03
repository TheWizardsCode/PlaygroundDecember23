using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(fileName = "Ammo Pickup Recipe", menuName = "Playground/Recipe/Ammo Pickup", order = 110)]
    public class AmmoPickupRecipe : ItemRecipe<Pickup>
    {
        [SerializeField, Tooltip("The ammo type this recipe creates.")]
        private SharedAmmoType ammo;

        private FpsInventorySwappable _inventory;
        private FpsInventorySwappable inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = FpsSoloCharacter.localPlayerCharacter.inventory as FpsInventorySwappable;
                }
                return _inventory;
            }
        }

        public override void Reset()
        {
            _inventory = null;
            base.Reset();
        }

        /// <summary>
        /// Test if the player has a given amount of ammo, expressed as a percentage of the maximum.
        /// </summary>
        /// <param name="requiredAmmoAmount">A value between 0 and 1 which is a % of the maxQuantity of ammo the player can hold.</param>
        /// <returns></returns>
        public bool HasAmount(float requiredAmmoAmount)
        {
            if (inventory.selected == null)
            {
                return false;
            }

            SharedPoolAmmo sharedPoolAmmo = inventory.selected.GetComponent<SharedPoolAmmo>();
            if (sharedPoolAmmo == null)
            {
                return false;
            }

            if (sharedPoolAmmo.ammoType.itemIdentifier == ammo.itemIdentifier)
            {
                return sharedPoolAmmo.currentAmmo >= ammo.maxQuantity * requiredAmmoAmount;
            }
            else
            {
                return true;
            }
        }
    }
}