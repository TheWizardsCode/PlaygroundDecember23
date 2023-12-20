using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(fileName = "Generic Recipe", menuName = "Playground/Generic Recipe")]
    public class Recipe<T> : ScriptableObject, IRecipe where T : MonoBehaviour
    {
        [SerializeField, Tooltip("The name of this recipe.")]
        public string displayName = "TBD";
        [SerializeField, Tooltip("The pickup item this recipe creates.")]
        public T item;
        [SerializeField, Tooltip("The offset from the NanobotManager to spawn the item.")]
        public Vector3 spawnOffset = new Vector3(0, 0, -2);
        [SerializeField, Tooltip("The resources required to build this ammo type.")]
        public int cost = 10;
        [SerializeField, Tooltip("The time it takes to build this recipe.")]
        public float timeToBuild = 5;
        [SerializeField, Tooltip("The sound to play when the recipe is complete.")]
        public AudioClip buildCompleteClip;

        public virtual bool ShouldBuild
        {
            get
            {
                return true;
            }
        }

        public string DisplayName => displayName;

        public Vector3 SpawnOffset => spawnOffset;

        public int Cost => cost;

        public float TimeToBuild => timeToBuild;

        public AudioClip BuildCompleteClip => buildCompleteClip;

        public Component Item => item;
    }
}