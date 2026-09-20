using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 视差背景。factor 越接近 1 跟随相机越紧（看起来越近），
    /// 0 表示完全不动（无限远）。
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] private float factor = 0.7f;

        private Transform _camera;
        private Vector3 _startPosition;
        private float _startCameraX;

        private void Start()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                enabled = false;
                return;
            }
            _camera = main.transform;
            _startPosition = transform.position;
            _startCameraX = _camera.position.x;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            float delta = _camera.position.x - _startCameraX;
            transform.position = new Vector3(
                _startPosition.x + delta * (1f - factor),
                _startPosition.y,
                _startPosition.z);
        }
    }
}
