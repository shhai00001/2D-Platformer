using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 玩家控制器。这里只管物理与状态流转，动画交给 Animator 状态机。
    /// 两者通过 Animator 参数（Speed / Grounded / VerticalSpeed / Attack / Hurt / Die）通信。
    ///
    /// 地面判定用 Physics2D.OverlapBox 加 LayerMask，脚下放一个 GroundCheck 空物体即可，
    /// 不依赖任何 Tag 或碰撞回调。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float groundAcceleration = 90f;
        [SerializeField] private float groundDeceleration = 110f;
        [SerializeField] private float airAcceleration = 55f;
        [SerializeField] private float airDeceleration = 30f;

        [Header("跳跃")]
        [SerializeField] private float jumpVelocity = 15.5f;
        [Tooltip("基础重力倍率。上升时用它，下落和短跳时再乘以下面的系数。")]
        [SerializeField] private float baseGravityScale = 4f;
        [SerializeField] private float fallGravityMultiplier = 1.7f;
        [SerializeField] private float lowJumpGravityMultiplier = 2.8f;
        [SerializeField] private float maxFallSpeed = 24f;
        [Tooltip("离开地面后仍可起跳的宽容时间。")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("落地前提前按跳的缓冲时间。")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("0 表示不能二段跳，1 表示可以二段跳。")]
        [SerializeField] private int maxAirJumps = 0;

        [Header("地面检测")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.14f);
        [SerializeField] private LayerMask groundMask;

        [Header("受击")]
        [SerializeField] private float fallbackHurtDuration = 0.3f;
        [SerializeField] private float knockbackDrag = 7f;

        [Header("攻击")]
        [Tooltip("动画事件万一没触发时的兜底退出时间。")]
        [SerializeField] private float attackTimeout = 1.2f;

        [Header("引用")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterVisual visual;
        [SerializeField] private PlayerAttack attack;
        [SerializeField] private PlayerHealth health;

        public Rigidbody2D Body { get; private set; }
        public Animator Animator => animator;
        public CharacterVisual Visual => visual;
        public PlayerInputReader Input { get; private set; }
        public PlayerAttack Attack => attack;
        public PlayerHealth Health => health;
        public StateMachine StateMachine { get; private set; }

        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => Body.velocity;
        public float MoveSpeed => moveSpeed;
        public float JumpVelocity => jumpVelocity;
        public float MaxFallSpeed => maxFallSpeed;
        public float BaseGravityScale => baseGravityScale;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float LowJumpGravityMultiplier => lowJumpGravityMultiplier;
        public float AttackTimeout => attackTimeout;
        public float KnockbackDrag => knockbackDrag;
        public Vector2 PendingKnockback { get; private set; }
        public float HurtDuration { get; private set; }

        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerHurtState HurtState { get; private set; }
        public PlayerDeadState DeadState { get; private set; }

        private float _lastGroundedTime = -999f;
        private float _jumpBufferedUntil = -999f;
        private int _airJumpsUsed;
        private bool _wasBlinking;

        private void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Input = GetComponent<PlayerInputReader>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visual == null) visual = GetComponent<CharacterVisual>();
            if (attack == null) attack = GetComponent<PlayerAttack>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (groundMask == 0) groundMask = GameLayers.GroundMask;

            Body.gravityScale = baseGravityScale;
            Body.freezeRotation = true;

            IdleState = new PlayerIdleState(this);
            MoveState = new PlayerMoveState(this);
            JumpState = new PlayerJumpState(this);
            FallState = new PlayerFallState(this);
            AttackState = new PlayerAttackState(this);
            HurtState = new PlayerHurtState(this);
            DeadState = new PlayerDeadState(this);

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
            StateMachine.Initialize(IdleState);
            Visual?.SetFacing(Facing);
            Attack?.SetFacing(Facing);
        }

        private void Update()
        {
            Input.Sample();

            if (Input.JumpPressed) _jumpBufferedUntil = Time.time + jumpBufferTime;

            UpdateBlinking();

            StateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            UpdateGroundState();
            StateMachine.FixedTick(Time.fixedDeltaTime);
            ApplyGravity();
        }

        // ---------- 地面 ----------

        private void UpdateGroundState()
        {
            if (groundCheck == null)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask) != null;

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                _airJumpsUsed = 0;
            }

            Animator.SetBool(AnimParams.Grounded, IsGrounded);
        }

        // ---------- 重力 ----------

        private void ApplyGravity()
        {
            if (health.IsDead) return;

            float scale = baseGravityScale;

            if (Body.velocity.y < -0.01f)
            {
                // 下落加速，落地手感更利落
                scale *= fallGravityMultiplier;
            }
            else if (Body.velocity.y > 0.01f && !Input.JumpHeld)
            {
                // 松开跳跃键就快速减速，实现短跳与长跳
                scale *= lowJumpGravityMultiplier;
            }

            Body.gravityScale = scale;

            if (Body.velocity.y < -maxFallSpeed)
            {
                Body.velocity = new Vector2(Body.velocity.x, -maxFallSpeed);
            }
        }

        // ---------- 供各状态调用的操作 ----------

        public void ChangeState(IState next) => StateMachine.ChangeState(next);

        public void SetFacing(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f) return;
            int newFacing = direction > 0f ? 1 : -1;
            if (newFacing == Facing) return;
            Facing = newFacing;
            Visual?.SetFacing(Facing);
            Attack?.SetFacing(Facing);
        }

        /// <summary>检查跳跃缓冲与土狼时间，满足条件就消耗掉这次跳跃输入。</summary>
        public bool ConsumeJumpIfAvailable()
        {
            if (Time.time > _jumpBufferedUntil) return false;

            bool canGroundJump = IsGrounded || (Time.time - _lastGroundedTime) <= coyoteTime;
            if (!canGroundJump)
            {
                if (_airJumpsUsed >= maxAirJumps) return false;
                _airJumpsUsed++;
            }
            else
            {
                _airJumpsUsed = 0;
            }

            _jumpBufferedUntil = -999f;
            _lastGroundedTime = -999f;   // 防止同一段土狼时间被用两次
            return true;
        }

        public void DoJump()
        {
            Body.velocity = new Vector2(Body.velocity.x, jumpVelocity);
        }

        public void MoveHorizontal(float direction, float dt, float controlScale = 1f)
        {
            float accel = (IsGrounded ? groundAcceleration : airAcceleration) * controlScale;
            float target = direction * moveSpeed;
            Body.velocity = new Vector2(Mathf.MoveTowards(Body.velocity.x, target, accel * dt), Body.velocity.y);
        }

        public void Decelerate(float dt, float controlScale = 1f)
        {
            float decel = (IsGrounded ? groundDeceleration : airDeceleration) * controlScale;
            Body.velocity = new Vector2(Mathf.MoveTowards(Body.velocity.x, 0f, decel * dt), Body.velocity.y);
        }

        /// <summary>受击时把击退速度注入刚体。</summary>
        public void ApplyKnockback(Vector2 knockback)
        {
            Body.velocity = new Vector2(knockback.x, Mathf.Max(Body.velocity.y, knockback.y));
        }

        /// <summary>水平击退逐帧衰减。</summary>
        public void DampKnockback(float dt)
        {
            Body.velocity = new Vector2(
                Mathf.MoveTowards(Body.velocity.x, 0f, knockbackDrag * dt),
                Body.velocity.y);
        }

        // ---------- 生命值事件 ----------

        private void OnDamaged(Health h, DamageInfo info)
        {
            if (h.IsDead) return;   // 致命一击交给 Died 处理，避免先播受击再播死亡

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
            if (groundCheck == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawCube(groundCheck.position, groundCheckSize);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
