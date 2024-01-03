using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    [RequireComponent(typeof(MotionController))]
    public class MovementUpgradeManager : MonoBehaviour, IMotionGraphDataOverride
    {
        #region MOTION DATA MODIFIERS

        /*
        
        FLOAT -
        - moveSpeedWalking
        - moveSpeedWalkAiming
        - moveSpeedSprinting
        - moveSpeedSprintAiming
        - moveSpeedCrouching
        - moveSpeedAirWalk
        - moveSpeedAirSprint
        - moveSpeedAirCrouch
        - acceleration
        - accelerationAirborne
        - deceleration
        - maxJumpHeight
        - jetpackForce
        - dashSpeed

        */

        public FloatValueModifier moveSpeed { get; private set; }
        public FloatValueModifier moveSpeedAirborne { get; private set; }
        public FloatValueModifier acceleration { get; private set; }
        public FloatValueModifier maxJumpHeight { get; private set; }
        public FloatValueModifier jetpackForce { get; private set; }
        public FloatValueModifier dashSpeed { get; private set; }

        public Func<bool, bool> GetBoolOverride(BoolData data) { return null; }
        public Func<int, int> GetIntOverride(IntData data) { return null; }

        public Func<float, float> GetFloatOverride(FloatData data)
        {
            switch (data.name)
            {
                // Grounded move speed
                case "moveSpeedWalking":
                    return moveSpeed.GetModifiedValue;
                case "moveSpeedWalkAiming":
                    return moveSpeed.GetModifiedValue;
                case "moveSpeedSprinting":
                    return moveSpeed.GetModifiedValue;
                case "moveSpeedSprintAiming":
                    return moveSpeed.GetModifiedValue;
                case "moveSpeedCrouching":
                    return moveSpeed.GetModifiedValue;

                // Airborne move speed
                case "moveSpeedAirWalk":
                    return moveSpeedAirborne.GetModifiedValue;
                case "moveSpeedAirSprint":
                    return moveSpeedAirborne.GetModifiedValue;
                case "moveSpeedAirCrouch":
                    return moveSpeedAirborne.GetModifiedValue;

                // Acceleration
                case "acceleration":
                    return acceleration.GetModifiedValue;
                case "accelerationAirborne":
                    return acceleration.GetModifiedValue;
                case "deceleration":
                    return acceleration.GetModifiedValue;

                // Other
                case "maxJumpHeight":
                    return maxJumpHeight.GetModifiedValue;
                case "jetpackForce":
                    return jetpackForce.GetModifiedValue;
                case "dashSpeed":
                    return dashSpeed.GetModifiedValue;

                default:
                    return null;
            }
        }

        void InitialiseDataModifiers()
        {
            var persistent = RogueLiteManager.persistentData;
            moveSpeed = new FloatValueModifier(persistent.moveSpeedMultiplier, persistent.moveSpeedPreAdd, persistent.moveSpeedPostAdd); 
            // Debug.Log("Move speed multiplier: " + persistent.moveSpeedMultiplier);
            moveSpeedAirborne = new FloatValueModifier(persistent.airbourneSpeedMultiplier, persistent.airbourneSpeedPreAdd, persistent.airbourneSpeedPostAdd);
            acceleration = new FloatValueModifier(persistent.accelerationMultiplier, persistent.accelerationdPreAdd, persistent.accelerationPostAdd);
            maxJumpHeight = new FloatValueModifier(persistent.maxJumpHeightMultiplier, persistent.maxJumpHeightPreAdd, persistent.maxJumpHeightPostAdd);
            jetpackForce = new FloatValueModifier(persistent.jetpackForceMultiplier, persistent.jetpackForcePreAdd, persistent.jetpackForcePostAdd);
            dashSpeed = new FloatValueModifier(persistent.dashSpeedMultiplier, persistent.dashSpeedPreAdd, persistent.dashSpeedPostAdd);
        }

        #endregion

        #region PARAMETERS

        /*

        SWITCH -
        - canAimHover
        - canDash
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

        private SwitchParameter m_CanAimHover = null;
        public bool canAimHover
        {
            get { return m_CanAimHover.on; }
            set { m_CanAimHover.on = value; }
        }

        private SwitchParameter m_CanDash = null;
        public bool canDash
        {
            get { return m_CanDash.on; }
            set { m_CanDash.on = value; }
        }

        private SwitchParameter m_CanGrapple = null;
        public bool canGrapple
        {
            get { return m_CanGrapple.on; }
            set { m_CanGrapple.on = value; }
        }

        private SwitchParameter m_CanJetpack = null;
        public bool canJetpack
        {
            get { return m_CanJetpack.on; }
            set { m_CanJetpack.on = value; }
        }

        private SwitchParameter m_CanWallRun = null;
        public bool canWallRun
        {
            get { return m_CanWallRun.on; }
            set { m_CanWallRun.on = value; }
        }

        private SwitchParameter m_CanWallRunUp = null;
        public bool canWallRunUp
        {
            get { return m_CanWallRunUp.on; }
            set { m_CanWallRunUp.on = value; }
        }

        private IntParameter m_MaxAirJumpCount = null;
        public int maxAirJumpCount
        {
            get { return m_MaxAirJumpCount.value; }
            set { m_MaxAirJumpCount.value = value; }
        }

        private FloatParameter m_MinDashInterval = null;
        public float minDashInterval
        {
            get { return m_MinDashInterval.value; }
            set { m_MinDashInterval.value = value; }
        }

        private FloatParameter m_MinAirJumpInterval = null;
        public float minAirJumpInterval
        {
            get { return m_MinAirJumpInterval.value; }
            set { m_MinAirJumpInterval.value = value; }
        }

        private VectorParameter m_CrouchDash = null;
        public float crouchDashSpeedBoost
        {
            get { return m_CrouchDash.value.z; }
            set { m_CrouchDash.value = new Vector3(0f, 0f, value); }
        }

        void InitialiseParameters()
        {
            m_CanAimHover = m_MotionGraph.GetSwitchProperty("canAimHover");
            m_CanDash = m_MotionGraph.GetSwitchProperty("canDash");
            m_CanGrapple = m_MotionGraph.GetSwitchProperty("canGrapple");
            m_CanJetpack = m_MotionGraph.GetSwitchProperty("canJetpack");
            m_CanWallRun = m_MotionGraph.GetSwitchProperty("canWallRun");
            m_CanWallRunUp = m_MotionGraph.GetSwitchProperty("canWallRunUp");
            m_MaxAirJumpCount = m_MotionGraph.GetIntProperty("maxAirJumpCount");
            m_MinDashInterval = m_MotionGraph.GetFloatProperty("minDashInterval");
            m_MinAirJumpInterval = m_MotionGraph.GetFloatProperty("minAirJumpInterval");
            m_CrouchDash = m_MotionGraph.GetVectorProperty("crouchDashSpeedBoost");
        }

        #endregion

        private MotionGraphContainer m_MotionGraph = null;

        void Awake()
        {
            // Get the persistent player data & motion graph
            m_MotionGraph = GetComponent<MotionController>().motionGraph;

            // Get the required parameters
            InitialiseParameters();

            // Set up value modifiers for motion graph data
            InitialiseDataModifiers();

            // Connect up the various data overrides
            m_MotionGraph.AddDataOverrides(this);
        }
    }
}