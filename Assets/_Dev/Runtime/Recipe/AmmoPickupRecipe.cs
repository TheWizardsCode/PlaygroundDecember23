using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(fileName = "Ammo Pickup Recipe", menuName = "Playground/Ammo Pickup Recipe")]
    public class AmmoPickupRecipe : Recipe<Pickup>
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
        [NonSerialized] private InventoryItemPickup inventoryPickup;

        public override bool ShouldBuild
        {
            get
            {
                SharedPoolAmmo sharedPoolAmmo = inventory.selected.GetComponent<SharedPoolAmmo>();
                if (sharedPoolAmmo == null)
                {
                    return false;
                }
                return sharedPoolAmmo.ammoType.itemIdentifier == ammo.itemIdentifier;
            }
        }
    }
}