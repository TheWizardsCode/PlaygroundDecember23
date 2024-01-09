using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Playground
{
    [Serializable]
    public class RogueLitePersistentData
    {
        // Movement stat upgrades
        // If you want to modify these during gameplay (in combat scene) use the values
        // on the character's MovementUpgradeManager instead
        public float moveSpeedMultiplier = 1f;
        public float moveSpeedPreAdd = 0f;
        public float moveSpeedPostAdd = 0f;
        public float airbourneSpeedMultiplier = 1f;
        public float airbourneSpeedPreAdd = 0f;
        public float airbourneSpeedPostAdd = 0f;
        public float accelerationMultiplier = 1f;
        public float accelerationdPreAdd = 0f;
        public float accelerationPostAdd = 0f;
        public float maxJumpHeightMultiplier = 1f;
        public float maxJumpHeightPreAdd = 0f;
        public float maxJumpHeightPostAdd = 0f;
        public float jetpackForceMultiplier = 1f;
        public float jetpackForcePreAdd = 0f;
        public float jetpackForcePostAdd = 0f;
        public float dashSpeedMultiplier = 1f;
        public float dashSpeedPreAdd = 0f;
        public float dashSpeedPostAdd = 0f;

        // Add additional persistent data here
        // Can serialize value types, serializable structs and arrays/lists
        // Can NOT(!) serialize UnityEngine.Object references or non-serializable types like dictionary
        // Remember to set isDirty = true when changing a value

        public int runNumber = 0;

        public int m_CurrentResources = 0; // The current resources of the player, the player gains resources by destroying enemies and loses resources by dying
        public int currentResources
        {
            get { return m_CurrentResources; }
            set
            {
                m_CurrentResources = value;
                isDirty = true;
            }
        }

        public List<string> recipeGuids = new List<string>();
        public List<string> RecipeIds { get { return recipeGuids; } }

        public RogueLitePersistentData()
        {
            if (runNumber == 0) // this will be the players first run
            {
                currentResources = 150;
            }

#if UNITY_EDITOR
            //currentResources = 100000;
            //Debug.Log("RogueLiteRunData: currentResources set to 100000 as we are running in the editor in debug mode.");
#endif
        }

        /// <summary>
        /// The recipes that will be available to the player in their NanobotManager when they start a level in a run.
        /// This will be reset on death.
        /// </summary>
        /// <returns>True if the recipe is added, false if not added because already present.</returns> 
        public bool Add(IRecipe recipe)
        {
            if (recipeGuids.Contains(recipe.UniqueID))
            {
                return false;
            }

            recipeGuids.Add(recipe.UniqueID);
            isDirty = true;
            return true;
        }

        public bool isDirty { get; set; } // TODO: Need to wrap values above to automate setting this on change
    }
}