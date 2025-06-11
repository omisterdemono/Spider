using UnityEngine;

public class Spider : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 60f;

    [Header("Surface Alignment")]
    [SerializeField] private float _alignSpeed = 10f;
    [SerializeField] private float _raycastDistance = 2f;
    [SerializeField] private float _stickToSurfaceForce = 1f; // Зменшено силу притискання
    [SerializeField] private LayerMask _groundMask;

    [Header("References")]
    [SerializeField] private FreeSpringCamera _camera;
    [SerializeField] private Rigidbody _rigidbody;

    private Vector3 _currentNormal = Vector3.up;

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        AlignToSurface();

        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            Move();
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            RotateTowardsCamera();
        }

        if (Input.GetKey(KeyCode.Q))
        {
            Rotate(-1f);
        }

        if (Input.GetKey(KeyCode.E))
        {
            Rotate(1f);
        }
    }

    private void Move()
    {
        Vector3 input = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        Vector3 moveDir = Vector3.ProjectOnPlane(input, _currentNormal).normalized;

        _rigidbody.MovePosition(_rigidbody.position + moveDir * _moveSpeed * Time.deltaTime);
    }

    private void Rotate(float direction)
    {
        Quaternion rotation = Quaternion.AngleAxis(direction * _rotationSpeed * Time.deltaTime, _currentNormal);
        _rigidbody.MoveRotation(rotation * _rigidbody.rotation);
    }

    private void RotateTowardsCamera()
    {
        // Отримаємо напрямок камери по forward, але тільки в площині нормалі
        Vector3 camForward = Vector3.ProjectOnPlane(_camera.transform.forward, _currentNormal).normalized;
        if (camForward.sqrMagnitude < 0.001f) return;

        // Створюємо цільову орієнтацію
        Quaternion targetRotation = Quaternion.LookRotation(camForward, _currentNormal);

        // Обертаємо павука згладжено
        _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _rotationSpeed * Time.deltaTime));
    }

    private void AlignToSurface()
    {
        Vector3 rayOrigin = transform.position + _currentNormal * 0.2f;
        Vector3 rayDir = -_currentNormal;

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, _raycastDistance, _groundMask))
        {
            _currentNormal = hit.normal;

            Quaternion surfaceRotation = Quaternion.FromToRotation(transform.up, _currentNormal) * transform.rotation;
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, surfaceRotation, _alignSpeed * Time.deltaTime));

            // Притискання тільки по нормалі (вгору-вниз)
            Vector3 toSurface = Vector3.Project(hit.point - transform.position, _currentNormal);

            float distanceToSurface = toSurface.magnitude;

            if (distanceToSurface > 0.05f) // Мінімальний поріг для притискання
            {
                _rigidbody.MovePosition(transform.position + toSurface.normalized * distanceToSurface * _stickToSurfaceForce * Time.deltaTime);
            }
        }
        else
        {
            _currentNormal = Vector3.up;
        }

        Debug.DrawRay(rayOrigin, rayDir * _raycastDistance, Color.red);
    }
}
