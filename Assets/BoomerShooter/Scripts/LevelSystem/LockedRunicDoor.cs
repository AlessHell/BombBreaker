using System.Collections;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace BoomerShooter.LevelSystem
{
    public class LockedRunicDoor : MonoBehaviour
    {
        [Tooltip("The required key to open this door")]
        public RuneKeyType RequiredKey = RuneKeyType.Silver;

        [Tooltip("The door mesh transform that will move when opened. If null, this transform is used.")]
        public Transform DoorTransform;

        [Tooltip("Distance and direction the door moves when opening")]
        public Vector3 OpenOffset = new Vector3(0f, 5f, 0f);

        [Tooltip("Speed of opening animation")]
        public float OpenSpeed = 3f;

        [Tooltip("Audio played when locked")]
        public AudioClip LockedSfx;

        [Tooltip("Audio played when opening")]
        public AudioClip OpenSfx;

        Vector3 m_ClosedPos;
        Vector3 m_TargetOpenPos;
        bool m_IsOpen;
        bool m_IsOpening;
        float m_LastPromptTime;

        void Awake()
        {
            if (DoorTransform == null) DoorTransform = transform;
            m_ClosedPos = DoorTransform.position;
            m_TargetOpenPos = m_ClosedPos + OpenOffset;
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_IsOpen || m_IsOpening) return;

            var player = other.GetComponentInParent<PlayerCharacterController>();
            if (player != null)
            {
                string keyId = RequiredKey.ToString();
                bool hasKey = PlayerCampaignPersistence.Instance != null && PlayerCampaignPersistence.Instance.HasKey(keyId);

                if (hasKey)
                {
                    OpenDoor();
                }
                else
                {
                    if (Time.time > m_LastPromptTime + 2f)
                    {
                        m_LastPromptTime = Time.time;
                        PlayLockedSound();

                        DisplayMessageEvent msg = Events.DisplayMessageEvent;
                        msg.Message = $"Requires {RequiredKey} Rune Key to open!";
                        msg.DelayBeforeDisplay = 0f;
                        EventManager.Broadcast(msg);
                    }
                }
            }
        }

        public void OpenDoor()
        {
            if (m_IsOpen || m_IsOpening) return;
            m_IsOpening = true;

            PlayOpenSound();

            DisplayMessageEvent msg = Events.DisplayMessageEvent;
            msg.Message = $"Unlocked with {RequiredKey} Rune Key!";
            msg.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(msg);

            StartCoroutine(DoOpenAnimation());
        }

        IEnumerator DoOpenAnimation()
        {
            while (Vector3.Distance(DoorTransform.position, m_TargetOpenPos) > 0.05f)
            {
                DoorTransform.position = Vector3.MoveTowards(DoorTransform.position, m_TargetOpenPos, OpenSpeed * Time.deltaTime);
                yield return null;
            }

            DoorTransform.position = m_TargetOpenPos;
            m_IsOpen = true;
            m_IsOpening = false;
        }

        void PlayLockedSound()
        {
            if (LockedSfx != null)
            {
                AudioSource.PlayClipAtPoint(LockedSfx, transform.position, 1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(CreateLockedClip(), transform.position, 0.8f);
            }
        }

        void PlayOpenSound()
        {
            if (OpenSfx != null)
            {
                AudioSource.PlayClipAtPoint(OpenSfx, transform.position, 1f);
            }
            else
            {
                AudioSource.PlayClipAtPoint(CreateOpenClip(), transform.position, 0.9f);
            }
        }

        AudioClip CreateLockedClip()
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Low heavy stone thud
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 25f);
                float wave = Mathf.Sin(2 * Mathf.PI * 90f * t);
                samples[i] = wave * env * 0.8f;
            }

            AudioClip clip = AudioClip.Create("DoorLockedSound", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateOpenClip()
        {
            int sampleRate = 44100;
            float duration = 0.8f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Deep rumbling grind
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((t / duration) * Mathf.PI);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                float rumble = Mathf.Sin(2 * Mathf.PI * 65f * t) * 0.5f;
                samples[i] = (rumble + noise) * env;
            }

            AudioClip clip = AudioClip.Create("DoorOpenSound", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
