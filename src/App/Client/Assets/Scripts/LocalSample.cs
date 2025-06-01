using UnityEngine;

namespace Client
{
    /// <summary>
    /// Local sample behavior for a moving object (just to make sure remote interpolation gives the similar lags result)
    /// </summary>
    public class LocalSample : MonoBehaviour
    {
        public Vector3 speed;
        public Rect rect;
        
        private Vector3 _speed;

        private void OnEnable()
        {
            _speed = speed;
        }

        private void Update()
        {
            _speed = speed;
            
            var pos = transform.position;
            pos += speed * Time.smoothDeltaTime;
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
