using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 敌人控制器。状态流转：巡逻 → 发现玩家 → 追击 → 进入攻击距离 → 攻击 → 冷却 → 回到追击。
    ///
    /// 攻击判定同样由 Animation Event 驱动；攻击冷却在"进入攻击状态"的瞬间就开始计时，
    /// 所以无论动画播多久，都不可能连续出手。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [Header("巡逻")]
        [SerializeField] private float patrolSpeed = 2f;
        [Tooltip("以出生点为中心，向左右各巡逻多远。")]
        [SerializeField] private float patrolDistance = 4f;
        [Tooltip("到达边界后原地停顿多久再折返。")]
        [SerializeField] private float patrolTurnPause = 0.4f;

        [Header("检测玩家")]
        [SerializeField] private float detectRadius = 8f;
        [Tooltip("眼睛的高度偏移，视线从这里射出。")]
        [SerializeField] private float detectEyeHeight = 1.1f;
        [Tooltip("高度差超过这个值就看不见玩家（不会隔着几层平台发现人）。")]
        [SerializeField] private float maxDetectHeightDiff = 2.6f;
        [Tooltip("失去视线后还会继续追多久。")]
        [SerializeField] private float loseSightMemory = 2.5f;
        [SerializeField] private LayerMask playerMask;
        [SerializeField] private LayerMask sightBlockerMask;

        [Header("追击")]
        [SerializeField] private float chaseSpeed = 3.6f;
        [Tooltip("离出生点超过这个距离就放弃追击。")]
        [SerializeField] private float chaseGiveUpDistance = 13f;

        [Header("攻击")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackVerticalTolerance = 1.4f;
        [Tooltip("两次攻击之间的最短间隔。这是防止连续攻击的关键。")]
        [SerializeField] private float attackCooldown = 1.6f;
        [Tooltip("动画事件丢失时的兜底退出时间。")]
        [SerializeField] private float attackTimeout = 1.5f;

        [Header("地形检测")]
        [SerializeField] private Transform ledgeCheck;
        [SerializeField] private Vector2 ledgeCheckSize = new Vector2(0.22f, 0.22f);
        [SerializeField] private Transform wallCheck;
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.2f, 0.7f);
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask obstacleMask;

        [Header("受击")]
        [SerializeField] private float fallbackHurtDuration = 0.3f;
        [SerializeField] private float knockbackDrag = 8f;

        [Header("引用")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterVisual visual;
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private EnemyHealth health;

        public Rigidbody2D Body { get; private set; }
        public Animator Animator => animator;
        public CharacterVisual Visual => visual;
        public EnemyAttack Attack => attack;
        public EnemyHealth Health => health;
        public StateMachine StateMachine { get; private set; }
        public Transform Player { get; private set; }

        public int Facing { get; private set; } = -1;
        public Vector2 Velocity => Body.velocity;
        public float PatrolSpeed => patrolSpeed;
        public float PatrolDistance => patrolDistance;
        public float PatrolTurnPause => patrolTurnPause;
        public float PatrolOriginX { get; private set; }
        public float ChaseSpeed => chaseSpeed;
        public float ChaseGiveUpDistance => chaseGiveUpDistance;
        public float AttackRange => attackRange;
        public float AttackVerticalTolerance => attackVerticalTolerance;
        public float AttackCooldown => attackCooldown;
        public float AttackTimeout => attackTimeout;
        public float KnockbackDrag => knockbackDrag;
        public Vector2 PendingKnockback { get; private set; }
        public float HurtDuration { get; private set; }

        /// <summary>当前是否锁定玩家（看得见，或刚失去视线还在记忆时间内）。</summary>
        public bool HasTarget { get; private set; }
        /// <summary>本帧是否真的看得见玩家（用于区分"追击"与"搜索"）。</summary>
        public bool HasLineOfSight { get; private set; }
        /// <summary>冷却是否结束，可以出手了。</summary>
        public bool CanAttack => Time.time >= _nextAttackTime;

        public EnemyPatrolState PatrolState { get; private set; }
        public EnemyChaseState ChaseState { get; private set; }
        public EnemyAttackState AttackState { get; private set; }
        public EnemyHurtState HurtState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }

        private float _nextAttackTime = -999f;
        private float _lastSeenTime = -999f;
        private bool _wasBlinking;

        private void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visual == null) visual = GetComponent<CharacterVisual>();
            if (attack == null) attack = GetComponent<EnemyAttack>();
            if (health == null) health = GetComponent<EnemyHealth>();
            if (groundMask == 0) groundMask = GameLayers.GroundMask;
            if (obstacleMask == 0) obstacleMask = GameLayers.GroundMask;
            if (playerMask == 0) playerMask = GameLayers.PlayerMask;
            if (sightBlockerMask == 0) sightBlockerMask = GameLayers.SightBlockers;

            Body.freezeRotation = true;

            PatrolOriginX = transform.position.x;

            PatrolState = new EnemyPatrolState(this);
            ChaseState = new EnemyChaseState(this);
            AttackState = new EnemyAttackState(this);
            HurtState = new EnemyHurtState(this);
            DeadState = new EnemyDeadState(this);

            StateMachine = new StateMachine();

            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Start()
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) Player = pc.transform;

            StateMachine.Initialize(PatrolState);
            Visual?.SetFacing(Facing);
            Attack?.SetFacing(Facing);
            ApplyProbeOffsets();
        }

        private void Update()
        {
            if (health.IsDead) return;

            float dt = Time.deltaTime;

            HasLineOfSight = CheckLineOfSight();
            if (HasLineOfSight) _lastSeenTime = Time.time;
            HasTarget = HasLineOfSight || (Time.time - _lastSeenTime) <= loseSightMemory;

            // 速度驱动 Animator 的 Idle / Walk 切换
            Animator.SetFloat(AnimParams.Speed, Mathf.Abs(Body.velocity.x));

            UpdateBlinking();

            StateMachine.Tick(dt);
        }

        private void FixedUpdate()
        {
            if (health.IsDead) return;
            StateMachine.FixedTick(Time.fixedDeltaTime);
        }

        // ---------- 感知 ----------

        /// <summary>距离 + 高度差 + 视线遮挡，三者都通过才算"看得见玩家"。</summary>
        private bool CheckLineOfSight()
        {
            if (Player == null) return false;

            Vector2 eye = (Vector2)transform.position + Vector2.up * detectEyeHeight;
            Vector2 target = (Vector2)Player.position + Vector2.up * 0.7f;
            Vector2 delta = target - eye;
            float distance = delta.magnitude;

            if (distance > detectRadius) return false;
            if (Mathf.Abs(delta.y) > maxDetectHeightDiff) return false;
            if (distance < 0.01f) return true;

            // 被墙或地板挡住就看不见
            RaycastHit2D blocked = Physics2D.Raycast(eye, delta / distance, distance, sightBlockerMask);
            return blocked.collider == null;
        }

        public float DistanceToPlayer()
        {
            if (Player == null) return float.MaxValue;
            return Vector2.Distance(transform.position, Player.position);
        }

        public float HorizontalDirectionToPlayer()
        {
            if (Player == null) return Facing;
            float dx = Player.position.x - transform.position.x;
            return Mathf.Abs(dx) < 0.05f ? Facing : Mathf.Sign(dx);
        }

        /// <summary>玩家是否在可攻击的高度与距离范围内。</summary>
        public bool IsPlayerInAttackRange()
        {
            if (Player == null) return false;
            Vector2 delta = (Vector2)Player.position - (Vector2)transform.position;
            return Mathf.Abs(delta.x) <= attackRange && Mathf.Abs(delta.y) <= attackVerticalTolerance;
        }

        // ---------- 地形 ----------

        /// <summary>前方是不是悬崖。用于避免巡逻和追击时掉下去。</summary>
        public bool IsLedgeAhead()
        {
            if (ledgeCheck == null) return false;
            return Physics2D.OverlapBox(ledgeCheck.position, ledgeCheckSize, 0f, groundMask) == null;
        }

        /// <summary>前方是不是墙。</summary>
        public bool IsWallAhead()
        {
            if (wallCheck == null) return false;
            return Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, obstacleMask) != null;
        }

        // ---------- 给状态用的操作 ----------

        public void ChangeState(IState next) => StateMachine.ChangeState(next);

        public void SetFacing(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f) return;
            int newFacing = direction > 0f ? 1 : -1;
            if (newFacing == Facing) return;
            Facing = newFacing;
            Visual?.SetFacing(Facing);
            Attack?.SetFacing(Facing);
            ApplyProbeOffsets();
        }

        /// <summary>
        /// 把悬崖/墙壁探测点镜像到朝向那一侧。
        /// 探测点以"localPosition.x 为正 = 角色前方"的约定摆放，这里按朝向取符号。
        /// </summary>
        private void ApplyProbeOffsets()
        {
            if (ledgeCheck != null)
            {
                Vector3 p = ledgeCheck.localPosition;
                p.x = Mathf.Abs(p.x) * Facing;
                ledgeCheck.localPosition = p;
            }
            if (wallCheck != null)
            {
                Vector3 p = wallCheck.localPosition;
                p.x = Mathf.Abs(p.x) * Facing;
                wallCheck.localPosition = p;
            }
        }

        public void Move(float horizontalVelocity)
        {
            Body.velocity = new Vector2(horizontalVelocity, Body.velocity.y);
        }

        public void StopMoving()
        {
            Body.velocity = new Vector2(0f, Body.velocity.y);
        }

        public void ApplyKnockback(Vector2 knockback)
        {
            Body.velocity = new Vector2(knockback.x, Mathf.Max(Body.velocity.y, knockback.y));
        }

        public void DampKnockback(float dt)
        {
            Body.velocity = new Vector2(
                Mathf.MoveTowards(Body.velocity.x, 0f, knockbackDrag * dt),
                Body.velocity.y);
        }

        /// <summary>进入攻击状态时调用，立刻开始冷却计时 —— 这是防止连续攻击的关键。</summary>
        public void MarkAttackUsed()
        {
            _nextAttackTime = Time.time + attackCooldown;
        }

        /// <summary>剩余冷却时间，便于调试与表现。</summary>
        public float RemainingCooldown => Mathf.Max(0f, _nextAttackTime - Time.time);

        // ---------- 生命值事件 ----------

        private void OnDamaged(Health h, DamageInfo info)
        {
            if (h.IsDead) return;

            PendingKnockback = info.knockback;
            HurtDuration = info.hitStun > 0.01f ? info.hitStun : fallbackHurtDuration;
            StateMachine.ChangeState(HurtState);
        }

        private void OnDied(Health h, DamageInfo info)
        {
            StateMachine.ChangeState(DeadState);
        }

        private void UpdateBlinking()
        {
            bool shouldBlink = health.IsInvulnerable && !health.IsDead;
            if (shouldBlink == _wasBlinking) return;
            _wasBlinking = shouldBlink;
            Visual?.SetBlinking(shouldBlink);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 eye = transform.position + Vector3.up * detectEyeHeight;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 1f);
            Gizmos.DrawWireSphere(eye, detectRadius);

            if (ledgeCheck != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
                Gizmos.DrawWireCube(ledgeCheck.position, ledgeCheckSize);
            }
            if (wallCheck != null)
            {
                Gizmos.color = new Color(1f, 0.4f, 0.2f, 1f);
                Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
            }

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 1f);
            Gizmos.DrawWireCube(transform.position,
                                new Vector3(attackRange * 2f, attackVerticalTolerance * 2f, 0f));
        }
    }
}
