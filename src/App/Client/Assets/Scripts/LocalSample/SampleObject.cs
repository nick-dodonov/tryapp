using UnityEngine;

namespace Client.LocalSample
{
    /// <summary>
    /// Local sample behavior for a moving object (just to make sure remote interpolation gives the similar lags result)
    /// </summary>
    public class SampleObject : MonoBehaviour
    {
        public Vector3 speed;
        public Rect rect;

        private Vector3 _speed;

        private void Awake() => gameObject.SetActive(false);
        private void OnEnable() => _speed = speed;

        public void UpdateOptions(SampleOptions sampleOptions)
        {
            var enable = sampleOptions.Enabled;
            if (enable != gameObject.activeSelf)
                gameObject.SetActive(enable);
        }

        private void Update()
        {
            _speed = speed;

            var pos = transform.position;
            pos += speed * Time.deltaTime;
            transform.position = pos;

            if (pos.x < rect.xMin && _speed.x < 0 ||
                pos.x > rect.xMax && _speed.x > 0)
                _speed.x *= -1;
            if (pos.y < rect.yMin && _speed.y < 0 ||
                pos.y > rect.yMax && _speed.y > 0)
                _speed.y *= -1;

            speed = _speed;
        }
    }
}