using Unity.FPS.Game;
using UnityEngine;

namespace BoomerShooter.Weapons
{
    public class CosmicRifleWeaponController : WeaponController
    {
        [Header("Cosmic Rifle Custom Parameters")]
        [Tooltip("Rapid fire delay (e.g. 0.09s = ~660 RPM)")]
        public float FireRateDelay = 0.09f;

        [Tooltip("Gunfire audio clip")]
        public AudioClip CustomRifleSfx;

        AudioSource m_RifleAudioSource;

        void Awake()
        {
            WeaponName = "Cosmic Assault Rifle";
            ShootType = WeaponShootType.Automatic;
            DelayBetweenShots = FireRateDelay;
            BulletSpreadAngle = 0.6f;
            BulletsPerShot = 1;
            RecoilForce = 0.35f;

            m_RifleAudioSource = GetComponent<AudioSource>();
            if (m_RifleAudioSource == null)
            {
                m_RifleAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (CustomRifleSfx == null)
            {
                CustomRifleSfx = CreateProceduralRifleSound();
            }
        }

        protected override void HandleShoot()
        {
            base.HandleShoot();

            if (m_RifleAudioSource != null && CustomRifleSfx != null)
            {
                m_RifleAudioSource.pitch = Random.Range(0.95f, 1.05f);
                m_RifleAudioSource.PlayOneShot(CustomRifleSfx, 0.7f);
            }
        }

        AudioClip CreateProceduralRifleSound()
        {
            int sampleRate = 44100;
            float duration = 0.10f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 40f);
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float punch = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(220f, 60f, t / duration) * t) * 0.5f;
                samples[i] = (noise + punch) * env;
            }

            AudioClip clip = AudioClip.Create("ProceduralRifleShot", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
