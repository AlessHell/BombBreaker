using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace BoomerShooter.Weapons
{
    public class KatanaWeaponController : WeaponController
    {
        [Header("Katana Melee Settings")]
        [Tooltip("Max reach of the katana slash in meters")]
        public float AttackRange = 3.2f;

        [Tooltip("Damage dealt per slash")]
        public float AttackDamage = 95f;

        [Tooltip("Half-angle of the slash attack cone in degrees")]
        public float AttackConeAngle = 55f;

        [Tooltip("Layers the slash can hit")]
        public LayerMask HitLayers = -1;

        [Header("Audio")]
        public AudioClip SwingSfx;
        public AudioClip HitFleshSfx;

        [Header("Visual Swing Motion")]
        [Tooltip("The blade visual root to animate procedurally")]
        public Transform BladeVisual;

        Vector3 m_InitialBladePos;
        Quaternion m_InitialBladeRot;
        bool m_IsSlashing;
        AudioSource m_AudioSource;

        void Awake()
        {
            InfiniteAmmo = true;
            WeaponName = "Runic Katana";
            ShootType = WeaponShootType.Manual;
            DelayBetweenShots = 0.32f;
            MaxAmmo = 1;
            m_CurrentAmmo = 1;

            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (BladeVisual != null)
            {
                m_InitialBladePos = BladeVisual.localPosition;
                m_InitialBladeRot = BladeVisual.localRotation;
            }
        }

        protected override void HandleShoot()
        {
            m_LastTimeShot = Time.time;

            // Play swing audio
            PlaySwingSound();

            // Perform slash raycast / spherecast
            PerformMeleeHit();

            // Animate procedural slash
            if (BladeVisual != null)
            {
                if (m_IsSlashing)
                {
                    StopAllCoroutines();
                }
                StartCoroutine(DoProceduralSlashAnimation());
            }

            OnShoot?.Invoke();
        }

        void PerformMeleeHit()
        {
            Camera cam = Camera.main;
            Vector3 origin = cam != null ? cam.transform.position : transform.position;
            Vector3 forward = cam != null ? cam.transform.forward : transform.forward;

            Collider[] hits = Physics.OverlapSphere(origin, AttackRange, HitLayers, QueryTriggerInteraction.Ignore);
            HashSet<Health> damagedEntities = new HashSet<Health>();
            bool hitSomething = false;

            foreach (var col in hits)
            {
                // Don't hit owner
                if (Owner != null && (col.gameObject == Owner || col.transform.IsChildOf(Owner.transform)))
                    continue;

                Vector3 targetDir = (col.bounds.center - origin).normalized;
                float angle = Vector3.Angle(forward, targetDir);

                if (angle <= AttackConeAngle)
                {
                    Damageable damageable = col.GetComponent<Damageable>();
                    if (damageable != null && damageable.Health != null && !damagedEntities.Contains(damageable.Health))
                    {
                        damagedEntities.Add(damageable.Health);
                        hitSomething = true;

                        // Inflict heavy slash damage
                        damageable.InflictDamage(AttackDamage, false, Owner != null ? Owner : gameObject);

                        // If killed, trigger full gib explosion!
                        if (damageable.Health.CurrentHealth <= 0f)
                        {
                            var gibSpawner = damageable.GetComponentInParent<BoomerShooter.Combat.GibSpawner>();
                            if (gibSpawner != null)
                            {
                                gibSpawner.TriggerGib(col.bounds.center);
                            }
                        }
                    }
                }
            }

            if (hitSomething)
            {
                PlayHitSound();
            }
        }

        void PlaySwingSound()
        {
            if (SwingSfx != null)
            {
                m_AudioSource.PlayOneShot(SwingSfx, 0.7f);
            }
            else
            {
                // Procedural whoosh
                AudioClip proceduralWhoosh = CreateWhooshClip();
                m_AudioSource.PlayOneShot(proceduralWhoosh, 0.6f);
            }
        }

        void PlayHitSound()
        {
            if (HitFleshSfx != null)
            {
                m_AudioSource.PlayOneShot(HitFleshSfx, 0.85f);
            }
            else
            {
                AudioClip proceduralSlice = CreateSliceClip();
                m_AudioSource.PlayOneShot(proceduralSlice, 0.8f);
            }
        }

        IEnumerator DoProceduralSlashAnimation()
        {
            m_IsSlashing = true;
            float duration = 0.22f;
            float elapsed = 0f;

            // Slash arc: Wind up slightly, slash diagonally downward across screen, return to guard
            Quaternion startRot = m_InitialBladeRot * Quaternion.Euler(-20f, 30f, -25f);
            Quaternion slashEndRot = m_InitialBladeRot * Quaternion.Euler(45f, -65f, 60f);

            Vector3 startPos = m_InitialBladePos + new Vector3(0.08f, 0.05f, -0.05f);
            Vector3 slashEndPos = m_InitialBladePos + new Vector3(-0.25f, -0.15f, 0.15f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (t < 0.45f)
                {
                    // Fast forward strike
                    float strikeT = t / 0.45f;
                    strikeT = Mathf.Sin(strikeT * Mathf.PI * 0.5f); // ease out
                    BladeVisual.localPosition = Vector3.Lerp(startPos, slashEndPos, strikeT);
                    BladeVisual.localRotation = Quaternion.Slerp(startRot, slashEndRot, strikeT);
                }
                else
                {
                    // Return to rest position
                    float returnT = (t - 0.45f) / 0.55f;
                    returnT = returnT * returnT; // ease in
                    BladeVisual.localPosition = Vector3.Lerp(slashEndPos, m_InitialBladePos, returnT);
                    BladeVisual.localRotation = Quaternion.Slerp(slashEndRot, m_InitialBladeRot, returnT);
                }

                yield return null;
            }

            BladeVisual.localPosition = m_InitialBladePos;
            BladeVisual.localRotation = m_InitialBladeRot;
            m_IsSlashing = false;
        }

        AudioClip CreateWhooshClip()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((t / duration) * Mathf.PI);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float freq = Mathf.Lerp(450f, 150f, t / duration);
                float tone = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.6f;
                samples[i] = (tone + noise) * env;
            }

            AudioClip clip = AudioClip.Create("KatanaWhoosh", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateSliceClip()
        {
            int sampleRate = 44100;
            float duration = 0.14f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 28f);
                float noise = (Random.value * 2f - 1f);
                samples[i] = noise * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("KatanaSlice", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
