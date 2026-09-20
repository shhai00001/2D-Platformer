using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 平滑跟随玩家，并把镜头限制在关卡范围内，避免拍到关卡外的空白。
    /// 震动偏移在最后叠加。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.18f;
        [Tooltip("镜头相对玩家上移多少，让视线更偏向关卡前方。")]
        [SerializeField] private float lookAheadY = 1.2f;
        [Tooltip("根据玩家朝向做水平前瞻。")]
        [SerializeField] private float lookAheadX = 1.4f;
        [SerializeField] private float lookAheadSmoothing = 3f;

        [Header("边界")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 boundsMin = new Vector2(0f, 0f);
        [SerializeField] private Vector2 boundsMax = new Vector2(64f, 12f);

        private Camera _camera;
        private PlayerController _player;
        private Vector3 _velocity;
        private float _currentLookAheadX;

        public void SetBounds(Vector2 min, Vector2 max)
        {
            boundsMin = min;
            boundsMax = max;
            useBounds = true;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player != null)
                {
                    target = _player.transform;
                    SnapToTarget();
                }
            }
            else
            {
                _player = target.GetComponent<PlayerController>();
            }
        }

        /// <summary>切场景或复活时立刻对齐，避免镜头从上一关的位置飞过来。</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            _velocity = Vector3.zero;
            _currentLookAheadX = 0f;
            transform.position = Clamp(DesiredPosition());
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float desiredLookAhead = 0f;
            if (_player != null) desiredLookAhead = _player.Facing * lookAheadX;
            _currentLookAheadX = Mathf.Lerp(_currentLookAheadX, desiredLookAhead,
                                            1f - Mathf.Exp(-lookAheadSmoothing * Time.deltaTime));

            Vector3 desired = DesiredPosition();
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.position = Clamp(smoothed) + (Vector3)(CameraShake.Instance != null ? CameraShake.Instance.Offset : Vector2.zero);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 p = target.position;
            return new Vector3(p.x + _currentLookAheadX, p.y + lookAheadY, transform.position.z);
        }

        private Vector3 Clamp(Vector3 position)
        {
            if (!useBounds) return position;
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || !_camera.orthographic) return position;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            // 关卡比视口还窄时就不做该轴的限制，直接居中
            float minX = boundsMin.x + halfWidth;
            float maxX = boundsMax.x - halfWidth;
            float minY = boundsMin.y + halfHeight;
            float maxY = boundsMax.y - halfHeight;

            float x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(position.x, minX, maxX);
            float y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(position.y, minY, maxY);

            return new Vector3(x, y, position.z);
        }
    }
}
