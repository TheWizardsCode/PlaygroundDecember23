using NeoFPS;
using UnityEngine;
using NeoFPS.SinglePlayer;
using System;

namespace Playground
{
    [CreateAssetMenu(fileName = "Weapon Pickup Recipe", menuName = "Playground/Recipe/Weapon Pickup", order = 108)]
    public class WeaponPickupRecipe : ItemRecipe<InteractivePickup>
    {
        [Header("Weapon")]
        [SerializeField, Tooltip("The Ammo recipe for this weapon. When the weapon is built the player should get this recipe too.")]
        internal AmmoPickupRecipe ammoRecipe;

        [NonSerialized]
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

        public override void BuildFinished()
        {
            if (ammoRecipe != null)
            {
                FpsSoloCharacter.localPlayerCharacter.GetComponent<NanobotManager>().Add(ammoRecipe);
            }
            RogueLiteManager.runData.AddToLoadout(pickup.GetItemPrefab() as FpsInventoryItemBase);

            base.BuildFinished();
        }

        public override bool ShouldBuild
        {
            get
            {
                IInventoryItem[] ownedItems = inventory.GetItems();
                for (int i = 0; i < ownedItems.Length; i++)
                {
                    if (ownedItems[i].itemIdentifier == pickup.GetItemPrefab().itemIdentifier)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
