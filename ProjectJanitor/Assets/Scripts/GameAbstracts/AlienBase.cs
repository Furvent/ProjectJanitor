/*
    Optional DIRECTIVES : (not required to work well)  
       - Commented as SOUND for every line calling an optional audiosource.
       - Commented as UI or GUI for every line about UI/GUI
       - Commented as ANIM for animator variable trigger/param update.
    
    REQUIERED 
       - onDamageSound has to loop.
*/
using System;
using GalacticJanitor.Engine;
using MonoPersistency;
using UnityEngine;

namespace GalacticJanitor.Game
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AlienBase : LivingEntity
    {

        #region fields
        protected NavMeshAgent pathfinder;
        EnemyState state = EnemyState.IDLE;

        public Transform target { get; private set; }
        public bool targetLocked;

        [Header("Passed params")]
        public Transform sensor;
        public Animator rigging;
        float m_idleOffset;
        
        [Header("Behavior", order = 0)]
        public LayerMask lineOfSightMask;
        public float maxAttackRange;
        [HideInInspector]public float maxDistanceFromSpawn;
        float baseSpeed;

        [Header("Attack behavior", order = 2)]
        public float attackPerSecond;
        public bool enraged;
        public int remainingHealthToEnrage;
        public int enrageModifier;

        [Header("Alien Sounds", order = 3)]
        public AudioClip sndOnAttack;
        public AudioClip sndOnMove;
        public AudioClip sndOnAggro;
        public AudioClip sndEnrage;

        private Vector3 spawn;
        #endregion

        void Awake()
        {
            pathfinder = GetComponent<NavMeshAgent>();
        }

        protected override void Start()
        {
            base.Start();
            baseSpeed = pathfinder.speed;
            spawn = transform.position;
            if (rigging)
            {
                rigging.SetFloat("offset", UnityEngine.Random.Range(0f, 1f));
            }
        }

        protected virtual void Update()
        {
            if (rigging)
            {
                rigging.SetBool("enraged", enraged);
                rigging.SetFloat("magnitude", pathfinder.velocity.magnitude);
            }
            
            //Enrage state check.
            if (m_entity.health <= remainingHealthToEnrage)
            {
                Enrage();
            }
            
            
            //sensor traking if target exists
            if (sensor!= null && target) sensor.LookAt(target);

            //Check EnemyState
            UpdateState();
        }

        void FixedUpdate()
        {
            if (target)
                GameController.Player.UpdateCombat();

            CalculateBehavior();
        }

        /// <summary>
        /// Calculat the right action to do according to the object state.
        /// Physics dependant. -> FixedUpdate
        /// </summary>
        void CalculateBehavior()
        {
            if (state == EnemyState.FOLLOW)
            {
                pathfinder.SetDestination(target.position);
            }
            else if (state == EnemyState.RETURN)
            {
                pathfinder.SetDestination(spawn);
            }
            else if (state == EnemyState.ATTACK)
            {
                // Reduce the NavMeshAgent stopping range until target is reachable
                // or restore the value if target is close enought to be in sight
                if (TargetInSight())
                {
                    pathfinder.stoppingDistance = maxAttackRange;
                    SlerpToTarget();
                    Attack();
                }
                else
                {
                    pathfinder.stoppingDistance = pathfinder.stoppingDistance -= 1;
                    if (target != null) pathfinder.SetDestination(target.position);
                }
            }
        }

        /// <summary>
        /// Smoothly rotate the object in the direction of his target 
        /// </summary>
        void SlerpToTarget()
        {
            if (target != null)
            {
                Vector3 fixTargetPos = new Vector3(target.position.x, transform.position.y, target.position.z);
                Quaternion quat = Quaternion.LookRotation(fixTargetPos - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, quat, 3f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Determine if the target is in sight.
        /// </summary>
        /// <returns>True if nothing stand between this object and his target</returns>
        bool TargetInSight()
        {
            if (target == null) return false;

            Ray ray = new Ray(transform.position, target.position - transform.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxAttackRange, lineOfSightMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.transform == target)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Update the tacking state of this enemy.
        /// </summary>
        void UpdateState()
        {
            if (target != null)
            {
                if (GetDistanceFromTarget() < maxAttackRange)
                {
                    state = EnemyState.ATTACK;
                }
                else
                {
                    state = EnemyState.FOLLOW;
                }
            }
            else
            {
                state = EnemyState.IDLE;
            }
        }

        /*
        /// <summary>
        /// Update the tacking state of this enemy.
        /// </summary>
        void UpdateState()
        {
            //if (Vector3.Distance(transform.position, spawn) > maxDistanceFromSpawn || (target == null && Vector3.Distance(transform.position, spawn) > 2))
            if (target == null)
            {
                if (Vector3.Distance(transform.position, spawn) > 2)
                    state = EnemyState.RETURN;
                //target = null;
            }
            else
            {
                if (target != null)
                {
                    if (GetDistanceFromTarget() < maxAttackRange)
                    {
                        state = EnemyState.ATTACK;
                    }
                    else
                    {
                        state = EnemyState.FOLLOW;
                    }
                }
                else
                {
                    state = EnemyState.IDLE;
                }
            }
        }
        */
        public float GetDistanceFromTarget()
        {
            return Vector3.Distance(transform.position, target.position);
        }

        /// <summary>
        /// If not enraged yet, berserk it.
        /// </summary>
        public void Enrage()
        {
            if (!enraged)
            {
                if (sndEnrage) listener.PlayOneShot(sndEnrage);
                //if (onMoveSound) onMoveSound.volume = onMoveSound.volume * enrageModifier;
                //if (onAttackSound) onAttackSound.volume = onAttackSound.volume * enrageModifier;

                enraged = true;
                pathfinder.speed = baseSpeed * enrageModifier;
            }
        }

        /// <summary>
        /// Calm down... Good alien, good... 
        /// </summary>
        public void Traquilize()
        {
            if (enraged)
            {
                //if (onMoveSound) onMoveSound.volume = onMoveSound.volume / enrageModifier;
                //if (onAttackSound) onAttackSound.volume = onAttackSound.volume / enrageModifier;

                enraged = false;
                pathfinder.speed = baseSpeed;
            }
        }

        public virtual void SetTarget(Transform target)
        {
            if (!targetLocked)
                this.target = target;
        }

        public void LockTarget()
        {
            targetLocked = true;
        }

        public void LockTarget(Transform target)
        {
            UnlockTarget();
            SetTarget(target);
            LockTarget();
        }

        public void UnlockTarget()
        {
            targetLocked = false;
        }

        /// <summary>
        /// 01100100 01101001 01100101 00100000 01100010 01101001 01110100 01100011 01101000 00100000 00100001 
        /// </summary>
        protected virtual void Attack(){}

        public override void CollectData(DataContainer container)
        {
            container.RegisterLocation(transform);
            container.m_spawnable = m_entity.alive;
            container.Addvalue("ent", m_entity);
        }

        public override void LoadData(DataContainer container)
        {
            container.RestoreLocation(transform);
            m_entity = container.GetValue<EntityBook>("ent");
        }
    } 

    public enum EnemyState { ATTACK, FOLLOW, IDLE, RETURN }

}
