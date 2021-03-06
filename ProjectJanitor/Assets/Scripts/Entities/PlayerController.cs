using UnityEngine;
using System.Collections;
using GalacticJanitor.UI;
using GalacticJanitor.Engine;
using GalacticJanitor.Game;
using MonoPersistency;
//using System;

namespace GalacticJanitor.Game
{

    /*

        For the player, the rigidbody's Interpolation has to be set to "Interpolate".

        Unity Doc : Interpolation allows you to smooth out the effect of running physics at a fixed frame rate.

        By default interpolation is turned off. 
        Commonly rigidbody interpolation is used on the player's character. 
      * Physics is running at discrete timesteps, while graphics is renderered at variable frame rates. 
        This can lead to jittery looking objects, because physics and graphics are not completely in sync. 
        The effect is subtle but often visible on the player character, especially if a camera follows the main character. 
      * It is recommended to turn on interpolation for the main character but disable it for everything else.

    */

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerAmmo))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : LivingEntity
    {

        [Header("Player Sounds", order = 2)]
        public AudioClip sndOnLowHp;
        [HideInInspector]
        public bool isPlayingLowHpsound;
        public AudioClip[] sndSteps;

        [Header("Player Behavior")]
        public MarinesType marinesType;
        public PlayerAmmo playerAmmo;
        public float speed = 10;
        public bool freeze;
        public bool freezed;

        Rigidbody body;
        public PlayerRotation rotate; // Ref to the gameObject that must rotate, with the script PlayerRotation

        public Animator anim;
        [HideInInspector]
        public bool justHaveShoot = false;
        [HideInInspector]
        public float timerActiveJustHaveShoote = 0; // public, need to be accessible in weapon's script, but must be hide in inspector
        [Tooltip("Timer handle fire animation, time before switch to still or move animations. 0.5f to Hartman, 0.1f to Carter")]
        public float timerJustHaveShoot = 0.5f; // 0.5f to Hartman, 0.1f to Carter

        [Header("GUI")]
        public PlayerStateDisplay playerDisplay;
        public PlayerHUD m_HUD;

        [HideInInspector]public WeaponControllerCarter weapCCarter;
        [HideInInspector]public WeaponControllerHartman weapCHartman;

        //SCORE
        [System.Serializable]
        [SerializeField]
        public struct PlayerScore
        {
            public int enemyKilled;
        }
        public PlayerScore score;
        

        private float _movementCooldown = 0;
        public float MovementCooldown
        {
            get
            {
                return _movementCooldown;
            }
            set
            {
                freeze = value > 0;
                if (freeze) FreezeVelocity();

                _movementCooldown = value;
            }
        }

        protected override void Start()
        {
            base.Start();
            isPlayingLowHpsound = false;
            if (IsLowHp()) StartCoroutine("CoRoutLowLife");
        }

        void Awake()
        {
            if (playerDisplay)
            {
                entityDisplay = playerDisplay;
            }

            anim = gameObject.GetComponent<Animator>();
            playerAmmo = gameObject.GetComponent<PlayerAmmo>();
            body = gameObject.GetComponent<Rigidbody>();

            if (marinesType == MarinesType.MajCarter)
            { weapCCarter = GetComponent<WeaponControllerCarter>(); }
            else
            { weapCHartman = GetComponent<WeaponControllerHartman>(); }

            GameController.Player = this;
            GameController.TopDownCamera.SetTarget(transform);
            
        }

        public void RefreshAmmoHUD()
        {
            Debug.Log("func - RefreshAmmoHUD from PlayerController");
            if (isCarter())
                weapCCarter.RefreshAmmoHUD();
            else
                weapCHartman.RefreshAmmoHUD();
        }

        public new void TakeDirectDamage(int damage, bool ignoreArmor)
        {
            base.TakeDirectDamage(damage, ignoreArmor);
            if (IsLowHp() && !isPlayingLowHpsound)
            {
                StartCoroutine("CoRoutLowLife");
            }

        }

        private bool IsLowHp()
        {
            if (m_entity.health < m_entity.maxHealth / 4) return true;
            else return false;
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            GameController.TimeController.GameOver(true);
        }

        void Update()
        {
            rotate.ForceLookAt();
            UpdateCooldown();
            UpdateTimerPlayerAnimShoot();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!freeze && !freezed)
            {
                Movement(); 
            }
            else
            {
                anim.SetBool("playerMove", false);
                if (marinesType == MarinesType.MajCarter) anim.SetBool("bobbing", false);
            }
        }

        void Movement()
        {
            
            Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            body.AddRelativeForce(move * speed, ForceMode.VelocityChange); // For Forcemode see 2
            MovementAnim();
        }

        void FreezeVelocity()
        {
            body.velocity = Vector3.zero;
        }

        void UpdateCooldown()
        {
            if (_movementCooldown > 0)
            {
                _movementCooldown -= Time.deltaTime;
            }
            freeze = _movementCooldown > 0;
        }


        #region Sounds
        public void PlayStepSound()
        {
            listener.PlayOneShot(sndSteps[Random.Range(0, sndSteps.Length)], 0.04f);
        }

        private IEnumerator CoRoutLowLife()
        {
            isPlayingLowHpsound = true;

            while (IsLowHp() && m_entity.alive)
            {
                listener.PlayOneShot(sndOnLowHp);
                yield return new WaitForSeconds(sndOnLowHp.length);
            }

            isPlayingLowHpsound = false;
        }
        #endregion

        /// <summary>
        /// When the flag justHaveShoot is active, play the fire animation.
        /// </summary>
        #region Animation
        void UpdateTimerPlayerAnimShoot()
        {
            if (justHaveShoot)
            {
                timerActiveJustHaveShoote += Time.deltaTime;
                anim.SetBool("playerShoot", true);
                if (timerActiveJustHaveShoote >= timerJustHaveShoot)
                {
                    timerActiveJustHaveShoote = 0f;
                    justHaveShoot = false;
                    anim.SetBool("playerShoot", false);
                }
            }
        }

        void MovementAnim()
        {
            {
                if (marinesType == MarinesType.MajCarter)
                {
                    if (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f)
                    {
                        anim.SetBool("bobbing", true);

                        if (GetComponent<WeaponControllerCarter>().IndexActiveWeapon == 1)  // If Carter is equiped with flamethrower
                        {
                            anim.SetBool("playerMove", true);
                        }
                        else
                        {
                            anim.SetBool("playerMove", false);
                        }

                    }

                    else
                    {
                        anim.SetBool("playerMove", false);
                        anim.SetBool("bobbing", false);
                    }
                }

                else // Hartman
                {
                    if (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f)
                        anim.SetBool("playerMove", true);

                    else
                        anim.SetBool("playerMove", false);
                } 
            }
        } 
        #endregion

        #region DisplayInfo
        public void DisplayInfoWeapon1(int ammoCarried, int ammoInMagazine)
        {
            if (playerDisplay) playerDisplay.DisplayInfoWeapon1(ammoCarried, ammoInMagazine);
        }

        public void DisplayInfoWeapon2(int ammoCarried, int ammoInMagazine)
        {
            if (playerDisplay) playerDisplay.DisplayInfoWeapon2(ammoCarried, ammoInMagazine);
        }

        /// <summary>
        /// Display info about which weapon MajCarter is equiped.
        /// Send false if player is SgtHartman.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public void DisplayInfoIndexWeapon(int index)
        {
            if (marinesType == MarinesType.MajCarter)
            {
                if (playerDisplay) playerDisplay.DisplayInfoIndexWeapon(index, true);
            }

            else playerDisplay.DisplayInfoIndexWeapon(0, false);
        } 
        #endregion

        #region PERSISTENCY
		
        public override void CollectData(DataContainer container)
        {

            container.m_spawnable = true;

            container.Addvalue("marine", marinesType);
            container.Addvalue("ent", m_entity);

            container.Addvalue("stockAmmo0", playerAmmo.ammoCarriedType0);
            container.Addvalue("stockAmmo1", playerAmmo.ammoCarriedType1);

            if (marinesType == MarinesType.MajCarter)
            {
                container.Addvalue("ammo0", weapCCarter.doubleGuns.magazine);
                container.Addvalue("ammo1", weapCCarter.flamethrower.magazine);
                container.Addvalue("weapIndex", weapCCarter.IndexActiveWeapon);
            }
            else
            {
                container.Addvalue("ammo0", weapCHartman.assaultRifle.magazineBullet);
                container.Addvalue("ammo1", weapCHartman.assaultRifle.magazineGrenade);
            }


            SaveSystem.GetActiveSceneData().StageData.OverridePlayerLocation(transform.position, transform.rotation);
        }

        public override void LoadData(DataContainer container)
        {
            if (container != null)
            {
                if (SaveSystem.Registery.m_firstRegistering)
                {
                    CollectData(container);
                    SaveSystem.Registery.m_firstRegistering = false;
                }

                Debug.Log("[SaveSystem] PlayerData --Loading");
                //entity restore
                m_entity = container.GetValue<EntityBook>("ent");

                //weapon restore
                playerAmmo.ammoCarriedType0 = container.GetValue<int>("stockAmmo0");
                playerAmmo.ammoCarriedType1 = container.GetValue<int>("stockAmmo1");

                if (marinesType == MarinesType.MajCarter)
                {
                    weapCCarter.doubleGuns.magazine = container.GetValue<int>("ammo0");
                    weapCCarter.flamethrower.magazine = container.GetValue<int>("ammo1");
                    if (weapCCarter.IndexActiveWeapon != container.GetValue<int>("weapIndex"))
                        weapCCarter.SwitchIndexWeapon();
                }
                else
                {
                    weapCHartman.assaultRifle.magazineBullet = container.GetValue<int>("ammo0");
                    weapCHartman.assaultRifle.magazineGrenade = container.GetValue<int>("ammo1");
                }

                //location restore;
                SaveSystem.GetActiveSceneData().StageData.RestorePlayerLocation(transform);
            }

        }

        public override void Save()
        {
            if (m_data == null)
                m_data = new DataContainer("##");

            CollectData(m_data);
            Debug.Log("[SaveSystem] PlayerData --Saving");
            SaveSystem.RegisterPlayer(m_data);
        } 
        
        #endregion

        #region COMBAT

        public bool IsFighting { get; private set; }
        public bool m_stillCombat;

        [Header("Combat")]
        [Range(3, 10)]
        public float m_combatThreshold = 3;

        public void UpdateCombat()
        {
            m_stillCombat = true;

            if (!IsFighting)
            {
                StartCoroutine(CombatRoutine());
            }
        }

        void StartCombatState()
        {
            IsFighting = true;
            SaveSystem.BlockedSave = true;

            /*
            GameController.NotifyPlayer("Fight !", Color.red, 1);
            if (playerDisplay)
                playerDisplay.ShowCombatVisual(true);
            */
        }

        public void StopCombatState()
        {
            IsFighting = false;
            SaveSystem.BlockedSave = false;

            /*
            GameController.NotifyPlayer("End fight !", Color.red, 2);
            if (playerDisplay)
                playerDisplay.ShowCombatVisual(false);
            */
        }

        IEnumerator CombatRoutine()
        {
            //Init combat mode
            StartCombatState();

            //Loop until no Combat Update is called before the end of the combat threshold time.
            while (m_stillCombat)
            {
                m_stillCombat = false;
                yield return new WaitForSeconds(m_combatThreshold);
            }

            //Restore non-combat mode
            StopCombatState();
        }

        #endregion

        #region ENTRAVE

        [Header("Entrave Debuff")]
        public bool immuneEntrave;
        public float m_entraveSpeedReduction;
        public int m_maxEntraveTime;

        bool m_entraved;
        int m_entraveTime;


        public void Entrave(int sec)
        {
            if (!immuneEntrave)
            {
                m_entraveTime += sec;
                if (m_entraveTime > m_maxEntraveTime)
                    m_entraveTime = m_maxEntraveTime;

                if (!m_entraved)
                {
                    StartCoroutine(EntraveRoutine());
                }
            }
        }

        void EnterEntrave()
        {
            playerDisplay.DisplayEntrave(true);
            playerDisplay.UpdateEntrave(m_entraveTime, m_maxEntraveTime);
            m_entraved = true;
            speed -= m_entraveSpeedReduction;
        }

        void ExitEntrave()
        {
            playerDisplay.DisplayEntrave(false);
            speed += m_entraveSpeedReduction;
            m_entraveTime = 0;
            m_entraved = false;
        }

        IEnumerator EntraveRoutine()
        {
            EnterEntrave();

            while (m_entraveTime > 0)
            {
                yield return new WaitForSeconds(1);
                m_entraveTime--;
                playerDisplay.UpdateEntrave(m_entraveTime, m_maxEntraveTime);
            }

            ExitEntrave();
        }
        #endregion

        public bool isCarter()
        {
            return marinesType == MarinesType.MajCarter;
        }

    }

}

public enum MarinesType
{
    MajCarter, SgtHartman
}
