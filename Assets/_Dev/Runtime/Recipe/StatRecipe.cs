using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.SinglePlayer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Playground
{
    /// <summary>
    /// A stat recipe will upgrade one or more of the player's stats.
    /// The player need not doing anything once the upgrade has been built, it will be applied automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "Stat Recipe", menuName = "Playground/Recipe/Stat", order = 150)]
    public class StatRecipe : AbstractRecipe
    {
        public enum StatType
        {
            /*

            Parameters TODO:

            SWITCH -
            - canAimHover
            - canGrapple
            - canJetpack
            - canWallRun
            - canWallRunUp

            INT -
            - maxAirJumpCount

            FLOAT -
            - minDashInterval
            - minAirJumpInterval

            VECTOR -
            - crouchDash

            */
            MotionGraphMovementCanDash,
            MotionGraphMovementCanGrapple
        }
        [SerializeField, Tooltip("The type of stat to upgrade.")]
        StatType statType = StatType.MotionGraphMovementCanDash;

        public override void BuildFinished()
        {
            Apply();

            base.BuildFinished();
        }

        internal void Apply()
        {
            switch (statType)
            {
                case StatType.MotionGraphMovementCanDash:
                    
                    FpsSoloCharacter.localPlayerCharacter.GetComponent<MovementUpgradeManager>().canDash = true;
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = Guid.NewGuid().ToString();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
#endif
    }
}