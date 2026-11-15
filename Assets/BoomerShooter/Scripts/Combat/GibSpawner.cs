using Unity.FPS.Game;
using UnityEngine;

namespace BoomerShooter.Combat
{
    [RequireComponent(typeof(Health))]
    public class GibSpawner : MonoBehaviour
    {
        [Header("Gib Parameters")]
        [Tooltip("Minimum damage on death to trigger full gib explosion")]
        public float OverkillGibThreshold = 15f;

        [Tooltip("Number of gib pieces to spawn")]
        public int GibCount = 8;

        [Tooltip("Explosion force applied to gib chunks")]
        public float GibExplosionForce = 8f;

        [Tooltip("Material used for gib chunks if procedural")]
        public Material GibMaterial;

        [Tooltip("Audio played when exploding into gibs")]
        public AudioClip GibExplosionSfx;

        Health m_Health;
        bool m_HasGibbed;

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_Health.OnDie += CheckGibOnDie;
        }

        void OnDestroy()
        {
            if (m_Health != null)
            {
                m_Health.OnDie -= CheckGibOnDie;
            }
        }

        public void TriggerGib(Vector3 hitPoint)
        {
            if (m_HasGibbed) return;
            m_HasGibbed = true;

            SpawnGibs(hitPoint);
        }

        void CheckGibOnDie()
        {
            if (m_HasGibbed) return;

            // In Quake, high damage or explosive deaths trigger gibs
            TriggerGib(transform.position + Vector3.up * 0.5f);
        }

        void SpawnGibs(Vector3 origin)
        {
            // Play gore sound
            if (GibExplosionSfx != null)
            {
                AudioSource.PlayClipAtPoint(GibExplosionSfx, origin, 1f);
            }

            // Create material if needed
            if (GibMaterial == null)
            {
                GibMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                GibMaterial.color = new Color(0.6f, 0.05f, 0.05f); // Dark visceral blood red
                GibMaterial.SetFloat("_Smoothness", 0.8f); // Wet look
            }

            for (int i = 0; i < GibCount; i++)
            {
                GameObject gib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gib.name = "Gib_Chunk_" + i;

                // Random size
                float s = Random.Range(0.12f, 0.28f);
                gib.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1.4f), s);
                gib.transform.position = origin + Random.insideUnitSphere * 0.3f;
                gib.transform.rotation = Random.rotation;

                // Material
                var renderer = gib.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.material = GibMaterial;

                // Physics
                var rb = gib.GetComponent<Rigidbody>();
                if (rb == null) rb = gib.AddComponent<Rigidbody>();
                rb.mass = 0.5f;

                Vector3 randomDir = (Random.insideUnitSphere + Vector3.up * 0.7f).normalized;
                Vector3 launchVel = randomDir * Random.Range(GibExplosionForce * 0.5f, GibExplosionForce * 1.3f);
                Vector3 angularVel = Random.insideUnitSphere * 15f;

                var gibPiece = gib.AddComponent<GibPiece>();
                gibPiece.Lifetime = Random.Range(4f, 7f);
                gibPiece.Initialize(launchVel, angularVel);
            }

            // Hide main renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            // Disable main colliders so player doesn't bump into invisible corpse
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }
    }
}
