using NaughtyAttributes;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Playground
{
    /// <summary>
    /// Creates a recipe for an item. This is a generic class that can be used to create any item.
    /// </summary>
    /// <typeparam name="T">The type of item that will be created.</typeparam>
    /// <seealso cref="AmmoPickupRecipe"/>
    /// <seealso cref="WeaponPickupRecipe"/>
    /// <seealso cref="ToolPickupRecipe"/>
    public class ItemRecipe<T> : AbstractRecipe, IItemRecipe where T : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField, Tooltip("The pickup item this recipe creates.")]
        [FormerlySerializedAs("item")]
        internal T pickup;

        public Component Item => pickup;
    }
}