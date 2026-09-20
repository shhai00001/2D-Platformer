using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 旧版 Input Manager 的按键读取。刻意不写在 Update 里，
    /// 而是由 PlayerController 每帧显式调用 Sample()，
    /// 这样就不依赖 Unity 的脚本执行顺序。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("移动按键")]
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;
        [SerializeField] private KeyCode altLeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode altRightKey = KeyCode.RightArrow;

        [Header("跳跃按键")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode altJumpKey = KeyCode.W;

        [Header("攻击按键")]
        [SerializeField] private KeyCode attackKey = KeyCode.J;
        [SerializeField] private KeyCode altAttackKey = KeyCode.K;
        [SerializeField] private bool mouseAlsoAttacks = true;

        /// <summary>为 false 时所有输入都读成"没按"，用于死亡 / 过场。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>-1 / 0 / 1，本帧的水平输入方向。</summary>
        public float MoveDirection { get; private set; }
        public bool HasMoveInput => Mathf.Abs(MoveDirection) > 0.01f;
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }

        /// <summary>每帧调用一次，采样当前输入状态。</summary>
        public void Sample()
        {
            if (!Enabled)
            {
                MoveDirection = 0f;
                JumpPressed = false;
                JumpHeld = false;
                AttackPressed = false;
                return;
            }

            float dir = 0f;
            if (Input.GetKey(leftKey) || Input.GetKey(altLeftKey)) dir -= 1f;
            if (Input.GetKey(rightKey) || Input.GetKey(altRightKey)) dir += 1f;
            MoveDirection = dir;

            JumpPressed = Input.GetKeyDown(jumpKey) || Input.GetKeyDown(altJumpKey);
            JumpHeld = Input.GetKey(jumpKey) || Input.GetKey(altJumpKey);

            AttackPressed = Input.GetKeyDown(attackKey)
                            || Input.GetKeyDown(altAttackKey)
                            || (mouseAlsoAttacks && Input.GetMouseButtonDown(0));
        }

        /// <summary>临时屏蔽输入（受伤硬直、死亡、场景切换）。</summary>
        public void Block(float seconds)
        {
            Enabled = false;
            CancelInvoke(nameof(Unblock));
            Invoke(nameof(Unblock), seconds);
        }

        private void Unblock() => Enabled = true;
    }
}
