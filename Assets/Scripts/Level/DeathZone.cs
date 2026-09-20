using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 深坑死亡区。掉出关卡底部直接判定死亡，无视无敌帧。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeathZone : MonoBehaviour
    {
        [SerializeField] private bool affectEnemies = false;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead) return;
            if (health.Team == Team.Enemy && !affectEnemies) return;

            health.TakeDamage(DamageInfo.Fatal(gameObject));
        }
    }
}
