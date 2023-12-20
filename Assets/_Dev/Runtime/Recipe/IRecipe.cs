using System;
using UnityEngine;

namespace Playground
{
    internal interface IRecipe
    {
        public string DisplayName { get; }
        public Component Item { get; }
        public Vector3 SpawnOffset{ get; }
        public int Cost { get; }
        public float TimeToBuild { get; }
        public AudioClip BuildCompleteClip { get; }

        /// <summary>
        /// Indicates whether this recipe should be built if enough resources are available.
        /// This will test to see if the item would be useful to the player at this point in time.
        /// For example, if the player already has full health, a health pickup would not be useful.
        /// </summary>
        public bool ShouldBuild { get; }
    }
}