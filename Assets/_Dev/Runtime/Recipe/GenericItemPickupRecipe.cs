using NeoFPS;
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
    [CreateAssetMenu(fileName = "Item Pickup Recipe", menuName = "Playground/Recipe/Generic Item Pickup", order = 100)]
    public class GenericItemPickupRecipe : ItemRecipe<Pickup>
    {
    }
}