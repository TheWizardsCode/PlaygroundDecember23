using NeoFPS;
using UnityEngine;

namespace Playground
{
    public class EnemyWeapon : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField, Tooltip("The range of the weapon. If not within this range then it will not fire.")]
        float _Range = 50f;
        [SerializeField, Tooltip("How frequently this weapon can fire in seconds.")]
        private float _fireRate = 1f;
        [SerializeField, Tooltip("How quickly the weapon will move to the target position.")]
        private float _targetingSpeed = 5f;
        [SerializeField, Tooltip("The amount of damage this weapon will do to the player per second.")]
        private float damageAmount = 5f;
        [SerializeField, Tooltip("The accuracy of the weapon. This is a random offset applied to the target position. The actual offset will be +/- this amount on each axis.")]
        private float accuracy = 0.1f;

        [Header("Juice")]
        [SerializeField, Tooltip("The line renderer used to show the weapon firing.")]
        LineRenderer _lineRenderer;
        [SerializeField, Tooltip("The material to use for the laser when it is firing and doing damage on contact.")]
        Material _damagingLaser;
        [SerializeField, Tooltip("The material to use for the laser when it is targeting the player but not doing damage.")]
        Material _targetingLaser;
        [SerializeField, Tooltip("The amount of time in seconds that the weapon will be visible when firing.")]
        private float _fireDuration = 0.75f;

        private float _timeToNextFiring = 0f;

        BasicEnemyController controller;

        private Vector3 accuracyOffset;
        private float timeToNextRetargeting;
        private float _remainingShotTime;
        private Vector3 _targetedPos;

        enum State
        {
            Idle,
            LockingOn,
            LockedOn,
            Firing
        }

        private State _state = State.Idle;
        private State state
        {
            get => _state;
            set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                switch (value)
                {
                    case State.Idle:
                        HideLaser();
                        break;
                    case State.LockingOn:
                        _lineRenderer.material = _targetingLaser;
                        _remainingShotTime = _fireDuration;
                        break;
                    case State.LockedOn:
                        _lineRenderer.material = _targetingLaser;
                        break;
                    case State.Firing:
                        _lineRenderer.material = _damagingLaser;
                        _remainingShotTime = _fireDuration;
                        break;
                }
            }
        }

        /// <summary>
        /// Test if the current targeted position will result in a hit on the player.
        /// </summary>
        private IDamageHandler validTarget
        {
            get
            {
                Vector3 direction = (_lineRenderer.GetPosition(1) - transform.position).normalized;
                if (Physics.Raycast(transform.position, direction, out RaycastHit _hit))
                {
                    if (_hit.collider.CompareTag("Player"))
                    {
                        return _hit.transform.GetComponent<IDamageHandler>();
                    }
                }

                return null;
            }
        }

        private void Start()
        {
            controller = GetComponentInParent<BasicEnemyController>();
            HideLaser();
        }

        private void Update()
        {
            if (controller.CanSeeTarget == false) {
                state = State.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, controller.Target.position);
            float targetingRange = _Range * 1.5f;
            switch (state)
            {
                case State.Idle:
                    _timeToNextFiring -= Time.deltaTime;

                    if (distance <= targetingRange && _timeToNextFiring < 0)
                    {
                        state = State.LockingOn;
                    }
                    return;
                case State.LockingOn:
                    if (distance > targetingRange)
                    {
                        state = State.Idle;
                        return;
                    }

                    if (validTarget != null)
                    {
                        state = State.LockedOn;
                    }
                    
                    UpdateLaser();
                    break;
                case State.LockedOn:
                    if (distance > _Range) // can only fire if the player is still in range
                    {
                        state = State.Idle;
                        return;
                    }
                    
                    UpdateLaser();

                    if (controller.shouldAttack)
                    {
                        state = State.Firing;
                    } else
                    {
                        state = State.Idle;
                    }
                    break;
                case State.Firing:
                    _remainingShotTime -= Time.deltaTime;

                    UpdateLaser();

                    IDamageHandler damageHandler = validTarget;
                    if (damageHandler != null)
                    {
                        damageHandler.AddDamage(damageAmount * Time.deltaTime); ;
                    }

                    if (_remainingShotTime < 0)
                    {
                        _timeToNextFiring = _fireRate;
                        state = State.Idle;
                    }
                    break;
            }
        }

        void HideLaser()
        {
            _lineRenderer.enabled = false;
        }

        void UpdateLaser()
        {
            _timeToNextFiring -= Time.deltaTime;
            if (_timeToNextFiring > 0)
            {
                HideLaser();
                return;
            }
            
            if (timeToNextRetargeting < 0)
            {
                accuracyOffset = Vector3.zero;

                accuracyOffset.x += Random.Range(-accuracy, accuracy);
                accuracyOffset.z += Random.Range(-accuracy, accuracy);
                timeToNextRetargeting = _fireRate;
            } else
            {
                timeToNextRetargeting -= Time.deltaTime;
            }

            _targetedPos = controller.Target.position;
            _targetedPos += accuracyOffset;

            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, Vector3.Slerp(_lineRenderer.GetPosition(1), _targetedPos, _targetingSpeed * Time.deltaTime));
            _lineRenderer.enabled = true;
        }
    }
}