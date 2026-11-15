using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class DamageArea : MonoBehaviour
    {
        [Tooltip("Area of damage when the projectile hits something")]
        public float AreaOfEffectDistance = 5f;

        [Tooltip("Damage multiplier over distance for area of effect")]
        public AnimationCurve DamageRatioOverDistance;

        [Tooltip("Damage multiplier applied when damaging oneself (e.g. for rocket jumping)")]
        [Range(0f, 1f)]
        public float SelfDamageMultiplier = 0.35f;

        [Header("Debug")] [Tooltip("Color of the area of effect radius")]
        public Color AreaOfEffectColor = Color.red * 0.5f;

        public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers,
            QueryTriggerInteraction interaction, GameObject owner)
        {
            Dictionary<Health, Damageable> uniqueDamagedHealths = new Dictionary<Health, Damageable>();

            // Create a collection of unique health components that would be damaged in the area of effect (in order to avoid damaging a same entity multiple times)
            Collider[] affectedColliders = Physics.OverlapSphere(center, AreaOfEffectDistance, layers, interaction);
            foreach (var coll in affectedColliders)
            {
                Damageable damageable = coll.GetComponent<Damageable>();
                if (damageable)
                {
                    Health health = damageable.GetComponentInParent<Health>();
                    if (health && !uniqueDamagedHealths.ContainsKey(health))
                    {
                        uniqueDamagedHealths.Add(health, damageable);
                    }
                }
            }

            // Apply damages with distance falloff
            foreach (Damageable uniqueDamageable in uniqueDamagedHealths.Values)
            {
                float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
                float damageMultiplier = DamageRatioOverDistance.Evaluate(distance / AreaOfEffectDistance);
                bool isSelfDamage = owner != null && (uniqueDamageable.gameObject == owner || uniqueDamageable.transform.IsChildOf(owner.transform));
                if (isSelfDamage)
                {
                    damageMultiplier *= SelfDamageMultiplier;
                }

                uniqueDamageable.InflictDamage(damage * damageMultiplier, true, owner);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = AreaOfEffectColor;
            Gizmos.DrawSphere(transform.position, AreaOfEffectDistance);
        }
    }
}