using NeoFPS.SinglePlayer;
using NeoFPS;
using UnityEngine;
using System;

namespace Playground
{
    [CreateAssetMenu(fileName = "Tool Pickup Recipe", menuName = "Playground/Recipe/Tool Pickup", order = 105)]
    public class ToolPickupRecipe : ItemRecipe<InventoryItemPickup>
    {
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