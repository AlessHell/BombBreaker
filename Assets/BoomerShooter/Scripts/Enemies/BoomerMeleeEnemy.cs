using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace BoomerShooter.Enemies
{
    [RequireComponent(typeof(EnemyController))]
    public class BoomerMeleeEnemy : MonoBehaviour
    {
        public enum MeleeState
        {
            Patrol,
            Chase,
            Windup,
            Recover,
        }

        [Header("Visual & Animation")]
        public Animator Animator;

        [Header("Movement & Pursuit")]
        public float PatrolSpeed = 4.0f;
        public float ChaseSpeed = 9.5f;

        [Header("Melee Combat")]
        public float MeleeTriggerRange = 2.7f;
        public float MeleeHitRange = 3.1f;
        public float MeleeDamage = 18f;
        public float WindupDuration = 0.24f;
        public float AttackCooldown = 0.95f;
        public float LungeSpeed = 10f;
        public float KnockbackForce = 7.5f;
        public bool IsAreaSlam = false;

        [Header("Effects & Audio")]
        public ParticleSystem[] RandomHitSparks;
        public AudioClip OnDetectSfx;
        public AudioClip AttackWindupSfx;
        public AudioClip AttackImpactSfx;

        public MeleeState CurrentState { get; private set; }

        EnemyController m_EnemyController;
        AudioSource m_AudioSource;
        AudioClip m_FallbackGrowlClip;
        AudioClip m_FallbackImpactClip;
        float m_StateTimer;
        float m_NextAttackTime;
        bool m_HasStruck;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        void Start()
        {
            m_EnemyController = GetComponent<EnemyController>();
            DebugUtility.HandleErrorIfNullGetComponent<EnemyController, BoomerMeleeEnemy>(m_EnemyController, this, gameObject);

            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.spatialBlend = 1f;
                m_AudioSource.minDistance = 3f;
                m_AudioSource.maxDistance = 35f;
            }

            m_FallbackGrowlClip = CreateServoGrowlClip();
            m_FallbackImpactClip = CreateHeavyImpactClip();

            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;
            m_EnemyController.onDamaged += OnDamaged;
            m_EnemyController.SetPathDestinationToClosestNode();

            NavMeshAgent agent = m_EnemyController.NavMeshAgent;
            if (agent != null)
            {
                agent.stoppingDistance = Mathf.Max(1.3f, MeleeTriggerRange * 0.55f);
            }

            CurrentState = MeleeState.Patrol;
        }

        void OnDestroy()
        {
            if (m_EnemyController != null)
            {
                m_EnemyController.onDetectedTarget -= OnDetectedTarget;
                m_EnemyController.onLostTarget -= OnLostTarget;
                m_EnemyController.onDamaged -= OnDamaged;
            }
        }

        void Update()
        {
            if (m_EnemyController == null) return;

            NavMeshAgent agent = m_EnemyController.NavMeshAgent;
            if (Animator != null && agent != null)
            {
                Animator.SetFloat(k_AnimMoveSpeedParameter, agent.velocity.magnitude);
            }

            switch (CurrentState)
            {
                case MeleeState.Patrol:
                    UpdatePatrol(agent);
                    break;
                case MeleeState.Chase:
                    UpdateChase(agent);
                    break;
                case MeleeState.Windup:
                    UpdateWindup(agent);
                    break;
                case MeleeState.Recover:
                    UpdateRecover();
                    break;
            }
        }

        void UpdatePatrol(NavMeshAgent agent)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = PatrolSpeed;
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
            }

            if (m_EnemyController.KnownDetectedTarget != null)
            {
                CurrentState = MeleeState.Chase;
            }
        }

        void UpdateChase(NavMeshAgent agent)
        {
            GameObject target = m_EnemyController.KnownDetectedTarget;
            if (target == null)
            {
                CurrentState = MeleeState.Patrol;
                return;
            }

            Vector3 targetPos = target.transform.position;
            m_EnemyController.OrientTowards(targetPos);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = ChaseSpeed;
                m_EnemyController.SetNavDestination(targetPos);
            }

            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            float verticalGap = Mathf.Abs(targetPos.y - transform.position.y);

            if (delta.magnitude <= MeleeTriggerRange && verticalGap <= 3.2f && Time.time >= m_NextAttackTime)
            {
                EnterWindup(agent);
            }
        }

        void EnterWindup(NavMeshAgent agent)
        {
            CurrentState = MeleeState.Windup;
            m_StateTimer = WindupDuration;
            m_HasStruck = false;

            if (agent != null && agent.isOnNavMesh)
            {
                m_EnemyController.SetNavDestination(transform.position);
            }

            if (Animator != null)
            {
                Animator.SetTrigger(k_AnimAttackParameter);
            }

            PlayClip(AttackWindupSfx != null ? AttackWindupSfx : m_FallbackGrowlClip, 0.85f);
        }

        void UpdateWindup(NavMeshAgent agent)
        {
            GameObject target = m_EnemyController.KnownDetectedTarget;
            if (target != null)
            {
                m_EnemyController.OrientTowards(target.transform.position);
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Move(transform.forward * (LungeSpeed * 0.35f * Time.deltaTime));
            }

            m_StateTimer -= Time.deltaTime;
            if (m_StateTimer <= 0f && !m_HasStruck)
            {
                m_HasStruck = true;
                PerformMeleeStrike();
                CurrentState = MeleeState.Recover;
                m_StateTimer = 0.25f;
                m_NextAttackTime = Time.time + AttackCooldown;
            }
        }

        void UpdateRecover()
        {
            GameObject target = m_EnemyController.KnownDetectedTarget;
            if (target != null)
            {
                m_EnemyController.OrientTowards(target.transform.position);
            }

            m_StateTimer -= Time.deltaTime;
            if (m_StateTimer <= 0f)
            {
                CurrentState = target != null ? MeleeState.Chase : MeleeState.Patrol;
            }
        }

        void PerformMeleeStrike()
        {
            GameObject target = m_EnemyController.KnownDetectedTarget;
            if (target == null) return;

            Vector3 targetPos = target.transform.position;
            Vector3 toTarget = targetPos - transform.position;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toTarget.magnitude;
            float flatDist = flatToTarget.magnitude;

            if (dist > MeleeHitRange && flatDist > MeleeHitRange) return;

            if (!IsAreaSlam && flatToTarget.sqrMagnitude > 0.001f)
            {
                float dot = Vector3.Dot(transform.forward, flatToTarget.normalized);
                if (dot <= 0.15f) return;
            }

            Damageable damageable = target.GetComponentInParent<Damageable>();
            PlayerCharacterController player = target.GetComponentInParent<PlayerCharacterController>();
            if (damageable == null && player != null)
            {
                damageable = player.GetComponentInChildren<Damageable>();
            }

            if (damageable != null)
            {
                damageable.InflictDamage(MeleeDamage, IsAreaSlam, gameObject);
            }

            if (player != null)
            {
                Vector3 pushDir = player.transform.position - transform.position;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = transform.forward;
                Vector3 impulseDir = (pushDir.normalized + Vector3.up * (IsAreaSlam ? 0.45f : 0.25f)).normalized;
                player.AddImpulse(impulseDir * KnockbackForce);
            }

            AudioClip impact = AttackImpactSfx != null ? AttackImpactSfx : m_FallbackImpactClip;
            if (impact != null)
            {
                AudioSource.PlayClipAtPoint(impact, targetPos, 0.95f);
            }
        }

        void OnDetectedTarget()
        {
            if (CurrentState == MeleeState.Patrol)
            {
                CurrentState = MeleeState.Chase;
            }

            if (Animator != null)
            {
                Animator.SetBool(k_AnimAlertedParameter, true);
            }

            PlayClip(OnDetectSfx != null ? OnDetectSfx : m_FallbackGrowlClip, 0.7f);
        }

        void OnLostTarget()
        {
            if (CurrentState == MeleeState.Chase || CurrentState == MeleeState.Recover)
            {
                CurrentState = MeleeState.Patrol;
            }

            if (Animator != null)
            {
                Animator.SetBool(k_AnimAlertedParameter, false);
            }
        }

        void OnDamaged()
        {
            if (RandomHitSparks != null && RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length);
                if (RandomHitSparks[n] != null) RandomHitSparks[n].Play();
            }

            if (Animator != null)
            {
                Animator.SetTrigger(k_AnimOnDamagedParameter);
            }
        }

        void PlayClip(AudioClip clip, float volume)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip, volume);
            }
        }

        AudioClip CreateServoGrowlClip()
        {
            int sampleRate = 22050;
            float duration = 0.22f;
            int total = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((t / duration) * Mathf.PI);
                float freq = Mathf.Lerp(145f, 240f, t / duration);
                float mod = Mathf.Sin(2f * Mathf.PI * 38f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t + mod * 0.6f) * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create("MechMeleeGrowl", total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateHeavyImpactClip()
        {
            int sampleRate = 22050;
            float duration = 0.26f;
            int total = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 18f);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 45f, t / duration) * t);
                float crunch = (Random.value * 2f - 1f) * 0.45f * Mathf.Exp(-t * 28f);
                samples[i] = (thud * 0.85f + crunch) * env;
            }
            AudioClip clip = AudioClip.Create("MechMeleeImpact", total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
