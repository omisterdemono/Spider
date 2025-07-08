using UnityEngine;

public class FreeSpringCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 2.0f;
    [SerializeField] private float _distance = 4.0f;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 5.0f;
    [SerializeField] private bool _invertY = false;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 60f;

    [Header("Spring Settings")]
    [SerializeField] private float _followSmoothness = 10.0f;
    [SerializeField] private float _upSmoothness = 5.0f;

    private float _yaw;
    private float _pitch;
    private Vector3 _currentVelocity;
    private Vector3 _smoothedUp = Vector3.up;
    private Spider _spider;

    private void Awake()
    {
        _spider = _target.GetComponent<Spider>();
    }

    void LateUpdate()
    {
        if (_target == null) return;

        float mouseX = Input.GetAxis("Mouse X") * _rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * _rotationSpeed * (_invertY ? 1 : -1);

        _yaw += mouseX;

        // Оновлюємо smoothedUp до target.up
        _smoothedUp = Vector3.Slerp(_smoothedUp, _target.up, Time.deltaTime * _upSmoothness);

        // Спершу обчислимо напрямок камери відносно павука
        // Використаємо yaw для обертання навколо smoothedUp
        Quaternion yawRotation = Quaternion.AngleAxis(_yaw, _smoothedUp);

        // Поточний напрямок вперед камери відносно павука
        Vector3 camForward = yawRotation * Vector3.forward;

        // Локальна права вісь камери (ортогональна до camForward і smoothedUp)
        Vector3 right = Vector3.Cross(camForward, _smoothedUp).normalized;

        // Тепер змінюємо pitch на основі руху миші навколо right
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        // Поворот навколо right (pitch)
        Quaternion pitchRotation = Quaternion.AngleAxis(_pitch, right);

        // Остаточне обертання камери
        Quaternion finalRotation = pitchRotation * yawRotation;

        Vector3 targetOffset = _target.position + _smoothedUp * _height;
        Vector3 desiredPosition = targetOffset - finalRotation * Vector3.forward * _distance;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, 1f / _followSmoothness);

        if (_spider.IsOnSurface) transform.rotation = finalRotation;
        
        transform.LookAt(targetOffset, _smoothedUp);
    }
}
