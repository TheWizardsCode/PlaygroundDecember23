using NeoFPS.SinglePlayer;
using NeoFPS;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Playground
{
    public static class InteractivePickupExtension
    {
        public static FpsInventoryItemBase GetItemPrefab(this InteractivePickup instance)
        {
            var fieldInfo = typeof(InteractivePickup).GetField("m_Item", BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(instance) as FpsInventoryItemBase;
        }
    }
    public static class InventoryItemPickupExtension
    {
        public static FpsInventoryItemBase GetItemPrefab(this InventoryItemPickup instance)
        {
            var fieldInfo = typeof(InventoryItemPickup).GetField("m_ItemPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(instance) as FpsInventoryItemBase;
        }
    }

    public static class  HealthPickupExtension
    {
        public static float GetHealAmount(this HealthPickup instance)
        {
            var fieldInfo = typeof(HealthPickup).GetField("m_HealAmount", BindingFlags.NonPublic | BindingFlags.Instance);
            return (float)fieldInfo.GetValue(instance);
        }
    }

    public static class LoadoutBuilderSlotExtensions
    {
        public static void AddOption(this LoadoutBuilderSlot slot, FpsInventoryItemBase option)
        {
            var field = typeof(LoadoutBuilderSlot).GetField("m_Options", BindingFlags.NonPublic | BindingFlags.Instance);
            var options = (FpsInventoryItemBase[])field.GetValue(slot);
            var optionsList = new List<FpsInventoryItemBase>(options) { option };
            field.SetValue(slot, optionsList.ToArray());
        }
    }
}