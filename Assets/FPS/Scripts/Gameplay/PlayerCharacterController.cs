using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")] [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;

        [Tooltip("Audio source for footsteps, jump, etc...")]
        public AudioSource AudioSource;

        [Header("General")] [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")] [Tooltip("Max movement speed when grounded (when not sprinting)")]
        public float MaxSpeedOnGround = 10f;

        [Tooltip(
            "Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Max movement speed when crouching")] [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        [Tooltip("Max movement speed when not grounded")]
        public float MaxSpeedInAir = 10f;

        [Tooltip("Acceleration speed when in the air")]
        public float AccelerationSpeedInAir = 25f;

        [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
        public float SprintSpeedModifier = 2f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")] [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Range(0.1f, 1f)] [Tooltip("Rotation speed multiplier when aiming")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")] [Tooltip("Force applied upward when jumping")]
        public float JumpForce = 9f;

        [Header("Stance")] [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.9f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 1.8f;

        [Tooltip("Height of character when crouching")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("Speed of crouching transitions")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")] [Tooltip("Amount of footstep sounds played when moving one meter")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("Sound played for footsteps")]
        public AudioClip FootstepSfx;

        [Tooltip("Sound played when jumping")] public AudioClip JumpSfx;
        [Tooltip("Sound played when landing")] public AudioClip LandSfx;

        [Tooltip("Sound played when taking damage froma fall")]
        public AudioClip FallDamageSfx;

        [Header("Fall Damage")]
        [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
        public bool RecievesFallDamage;

        [Tooltip("Minimun fall speed for recieving fall damage")]
        public float MinSpeedForFallDamage = 10f;

        [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
        public float MaxSpeedForFallDamage = 30f;

        [Tooltip("Damage recieved when falling at the mimimum speed")]
        public float FallDamageAtMinSpeed = 10f;

        [Tooltip("Damage recieved when falling at the maximum speed")]
        public float FallDamageAtMaxSpeed = 50f;

        [Header("Quake / Boomer Shooter Movement")]
        [Tooltip("Enable authentic Quake CPM air-strafing, bunnyhopping, and rocket jumps")]
        public bool UseQuakeMovement = true;

        [Tooltip("Base movement speed on ground")]
        public float QuakeGroundMaxSpeed = 13f;

        [Tooltip("Ground acceleration")]
        public float QuakeGroundAccelerate = 12f;

        [Tooltip("Ground friction")]
        public float QuakeFriction = 6f;

        [Tooltip("Friction stop threshold")]
        public float QuakeStopSpeed = 2f;

        [Tooltip("Air acceleration multiplier for CPM style strafing")]
        public float QuakeAirAccelerate = 18f;

        [Tooltip("Air wishspeed cap for strafe jumping")]
        public float QuakeAirWishSpeed = 3.2f;

        [Tooltip("Auto-bunnyhop when holding jump")]
        public bool AutoBhop = true;

        [Header("Quake Camera Roll & Dynamic FOV")]
        [Tooltip("Maximum camera roll angle when strafing")]
        public float CameraRollAngle = 2.5f;

        [Tooltip("Camera roll transition speed")]
        public float CameraRollSharpness = 8f;

        [Tooltip("Dynamic FOV based on player velocity")]
        public bool EnableDynamicFov = true;

        public float DynamicFovMin = 85f;
        public float DynamicFovMax = 110f;
        public float DynamicFovSpeedThreshold = 13f;
        public float DynamicFovMaxSpeed = 26f;

        public UnityAction<bool> OnStanceChanged;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        Health m_Health;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        PlayerWeaponsManager m_WeaponsManager;
        Actor m_Actor;
        Vector3 m_GroundNormal;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_CurrentCameraRoll = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake()
        {
            ActorsManager actorsManager = FindAnyObjectByType<ActorsManager>();
            if (actorsManager != null)
                actorsManager.SetPlayer(gameObject);
        }

        void Start()
        {
            // fetch components on the same gameObject
            m_Controller = GetComponent<CharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller,
                this, gameObject);

            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler,
                this, gameObject);

            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(
                m_WeaponsManager, this, gameObject);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

            m_Controller.enableOverlapRecovery = true;

            m_Health.OnDie += OnDie;

            // force the crouch state to false when starting
            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);
        }

        void Update()
        {
            // check for Y kill
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            GroundCheck();

            // landing
            if (IsGrounded && !wasGrounded)
            {
                // Fall damage
                float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                       (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    m_Health.TakeDamage(dmgFromFall, null);

                    // fall damage SFX
                    AudioSource.PlayOneShot(FallDamageSfx);
                }
                else
                {
                    // land SFX
                    AudioSource.PlayOneShot(LandSfx);
                }
            }

            // crouching
            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie()
        {
            IsDead = true;

            // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
            m_WeaponsManager.SwitchToWeaponIndex(-1, true);

            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck()
        {
            // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
            float chosenGroundCheckDistance =
                IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // reset values before the ground check
            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    // storing the upward direction for the surface found
                    m_GroundNormal = hit.normal;

                    // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                    // and if the slope angle is lower than the character controller's limit
                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;

                        // handle snapping to the ground
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }

        public void AddImpulse(Vector3 impulse)
        {
            CharacterVelocity += impulse;
            m_LastTimeJumped = Time.time;
            IsGrounded = false;
            m_GroundNormal = Vector3.up;
        }

        public void AddExplosionForce(Vector3 explosionOrigin, float force, float radius, float upwardModifier = 0.8f)
        {
            Vector3 diff = transform.position + Vector3.up * (m_Controller.height * 0.5f) - explosionOrigin;
            float dist = diff.magnitude;
            if (dist < radius)
            {
                float factor = 1f - Mathf.Clamp01(dist / radius);
                Vector3 dir = dist > 0.001f ? (diff / dist) : Vector3.up;
                dir.y = Mathf.Max(dir.y, upwardModifier);
                dir.Normalize();
                AddImpulse(dir * force * factor);
            }
        }

        void HandleCharacterMovement()
        {
            // horizontal character rotation
            {
                // rotate the transform with the input speed around its local Y axis
                transform.Rotate(
                    new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier),
                        0f), Space.Self);
            }

            // vertical camera rotation
            {
                // add vertical inputs to the camera's vertical angle
                m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;

                // limit the camera's vertical angle to min/max
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

                // calculate camera roll when strafing (Quake style camera banking)
                float targetRoll = 0f;
                if (UseQuakeMovement)
                {
                    targetRoll = -m_InputHandler.GetMoveInput().x * CameraRollAngle;
                }
                m_CurrentCameraRoll = Mathf.Lerp(m_CurrentCameraRoll, targetRoll, CameraRollSharpness * Time.deltaTime);

                // apply the vertical angle as a local rotation to the camera transform along its right axis, and roll along Z
                PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, m_CurrentCameraRoll);
            }

            // Dynamic FOV based on speed
            if (EnableDynamicFov && m_WeaponsManager != null)
            {
                float horizSpeed = new Vector2(CharacterVelocity.x, CharacterVelocity.z).magnitude;
                float fovFactor = Mathf.InverseLerp(DynamicFovSpeedThreshold, DynamicFovMaxSpeed, horizSpeed);
                float targetFov = Mathf.Lerp(DynamicFovMin, DynamicFovMax, fovFactor);
                m_WeaponsManager.SetFov(Mathf.Lerp(PlayerCamera.fieldOfView, targetFov, 6f * Time.deltaTime));
            }

            if (UseQuakeMovement)
            {
                HandleQuakeMovement();
            }
            else
            {
                HandleDefaultMovement();
            }

            // apply the final calculated velocity value as a character movement
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            // detect obstructions to adjust velocity accordingly
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                // We remember the last impact speed because the fall damage logic might need it
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        void HandleQuakeMovement()
        {
            Vector3 moveInput = m_InputHandler.GetMoveInput();
            Vector3 wishDir = transform.TransformDirection(moveInput);
            wishDir.y = 0f;
            float wishSpeed = wishDir.magnitude * QuakeGroundMaxSpeed;
            if (wishDir.sqrMagnitude > 0.0001f)
            {
                wishDir.Normalize();
            }

            bool wantsToJump = m_InputHandler.GetJumpInputDown() || (AutoBhop && m_InputHandler.GetJumpInputHeld());

            if (IsGrounded)
            {
                if (wantsToJump)
                {
                    // Immediate jump - skips ground friction to preserve bunnyhopping momentum
                    CharacterVelocity = new Vector3(CharacterVelocity.x, JumpForce, CharacterVelocity.z);
                    AudioSource.PlayOneShot(JumpSfx);
                    m_LastTimeJumped = Time.time;
                    HasJumpedThisFrame = true;
                    IsGrounded = false;
                    m_GroundNormal = Vector3.up;
                }
                else
                {
                    // Apply Quake ground friction
                    float hSpeed = Mathf.Sqrt(CharacterVelocity.x * CharacterVelocity.x + CharacterVelocity.z * CharacterVelocity.z);
                    if (hSpeed > 0.001f)
                    {
                        float control = hSpeed < QuakeStopSpeed ? QuakeStopSpeed : hSpeed;
                        float drop = control * QuakeFriction * Time.deltaTime;
                        float newSpeed = Mathf.Max(0f, hSpeed - drop) / hSpeed;
                        CharacterVelocity = new Vector3(CharacterVelocity.x * newSpeed, CharacterVelocity.y, CharacterVelocity.z * newSpeed);
                    }

                    // Apply Quake ground acceleration
                    float currentSpeed = Vector3.Dot(CharacterVelocity, wishDir);
                    float addSpeed = wishSpeed - currentSpeed;
                    if (addSpeed > 0f)
                    {
                        float accelSpeed = QuakeGroundAccelerate * wishSpeed * Time.deltaTime;
                        if (accelSpeed > addSpeed)
                            accelSpeed = addSpeed;
                        CharacterVelocity += wishDir * accelSpeed;
                    }

                    // Reorient on slope normal
                    if (CharacterVelocity.sqrMagnitude > 0.0001f)
                    {
                        CharacterVelocity = GetDirectionReorientedOnSlope(CharacterVelocity.normalized, m_GroundNormal) *
                                           CharacterVelocity.magnitude;
                    }

                    // Footsteps
                    if (m_FootstepDistanceCounter >= 1f / FootstepSfxFrequency)
                    {
                        m_FootstepDistanceCounter = 0f;
                        AudioSource.PlayOneShot(FootstepSfx);
                    }
                    m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
                }
            }
            else
            {
                // Air Accelerate (Quake CPM strafe-jumping math)
                float wishSpd = Mathf.Min(wishSpeed, QuakeAirWishSpeed);
                float currentSpeed = Vector3.Dot(CharacterVelocity, wishDir);
                float addSpeed = wishSpd - currentSpeed;
                if (addSpeed > 0f)
                {
                    float accelSpeed = QuakeAirAccelerate * wishSpeed * Time.deltaTime;
                    if (accelSpeed > addSpeed)
                        accelSpeed = addSpeed;
                    CharacterVelocity += wishDir * accelSpeed;
                }

                // Apply gravity
                CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
            }
        }

        void HandleDefaultMovement()
        {
            bool isSprinting = m_InputHandler.GetSprintInputHeld();
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = isSprinting ? SprintSpeedModifier : 1f;
            Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

            if (IsGrounded)
            {
                Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                if (IsCrouching)
                    targetVelocity *= MaxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                 targetVelocity.magnitude;

                CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                    MovementSharpnessOnGround * Time.deltaTime);

                if (IsGrounded && m_InputHandler.GetJumpInputDown())
                {
                    if (SetCrouchingState(false, false))
                    {
                        CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                        CharacterVelocity += Vector3.up * JumpForce;
                        AudioSource.PlayOneShot(JumpSfx);
                        m_LastTimeJumped = Time.time;
                        HasJumpedThisFrame = true;
                        IsGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }
                }

                float chosenFootstepSfxFrequency =
                    (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    AudioSource.PlayOneShot(FootstepSfx);
                }

                m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
            }
            else
            {
                CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;
                float verticalVelocity = CharacterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
                CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
            }
        }

        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        // Gets the center point of the bottom hemisphere of the character controller capsule    
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }

        // Gets the center point of the top hemisphere of the character controller capsule    
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        // Gets a reoriented direction that is tangent to a given slope
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // Update height instantly
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
            // Update smooth height
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                // resize the capsule and adjust camera position
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight,
                    CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                    Vector3.up * m_TargetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
        }

        // returns false if there was an obstruction
        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // set appropriate heights
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // Detect obstructions
                if (!ignoreObstructions)
                {
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        if (c != m_Controller)
                        {
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            if (OnStanceChanged != null)
            {
                OnStanceChanged.Invoke(crouched);
            }

            IsCrouching = crouched;
            return true;
        }
    }
}