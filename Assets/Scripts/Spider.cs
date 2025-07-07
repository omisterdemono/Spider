using UnityEngine;

public class Spider : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 60f;

    [Header("Surface Alignment")]
    [SerializeField] private float _alignSpeed = 10f;
    [SerializeField] private float _raycastDistance = 2f;
    [SerializeField] private float _stickToSurfaceForce = 1f;
    [SerializeField] private LayerMask _groundMask;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _jumpCooldown = 0.5f;
    [SerializeField] private float _jumpDuration = 0.6f;
    [SerializeField] private float _minJumpPercent = 0.1f;
    [SerializeField] private float _maxChargeTime = 2f;

    [Header("References")]
    [SerializeField] private FreeSpringCamera _camera;
    [SerializeField] private Rigidbody _rigidbody;

    private Vector3 _currentNormal = Vector3.up;
    private float _lastJumpTime = -999f;
    private bool _isJumping = false;
    private float _jumpHoldStartTime;
    private bool _isChargingJump = false;

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

        if (Input.GetKeyDown(KeyCode.Space) && !_isJumping && !_isChargingJump && Mathf.Abs(transform.rotation.y - _camera.transform.rotation.y) < 0.1f)
        {
            _isChargingJump = true;
            _jumpHoldStartTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.Space) && _isChargingJump && Mathf.Abs(transform.rotation.y - _camera.transform.localRotation.y) < 0.1f)
        {
            float heldTime = Time.time - _jumpHoldStartTime;
            float chargePercent = Mathf.Clamp01(heldTime / _maxChargeTime);
            chargePercent = Mathf.Max(chargePercent, _minJumpPercent);

            PerformJump(chargePercent);
            _isChargingJump = false;
        }
    }

    private void FixedUpdate()
    {
        if (!_isJumping)
        {
            AlignToSurface();
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
        Vector3 camForward = Vector3.ProjectOnPlane(_camera.transform.forward, _currentNormal).normalized;
        if (camForward.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(camForward, _currentNormal);
        _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _rotationSpeed * Time.deltaTime));
    }

    private void PerformJump(float chargePercent)
    {
        if (Time.time - _lastJumpTime < _jumpCooldown || _isJumping) return;
        _lastJumpTime = Time.time;
        _isJumping = true;

        _rigidbody.useGravity = true;

        Vector3 jumpDir = Vector3.ProjectOnPlane(_camera.transform.forward, _currentNormal).normalized;
        if (jumpDir.sqrMagnitude < 0.01f)
            jumpDir = transform.forward;

        Vector3 upwardPush = _currentNormal * 0.8f;
        Vector3 totalJumpVector = (jumpDir + upwardPush).normalized;

        _rigidbody.linearVelocity = Vector3.zero;
        float finalJumpForce = _jumpForce * chargePercent;
        _rigidbody.AddForce(totalJumpVector * finalJumpForce, ForceMode.VelocityChange);

        Invoke(nameof(EndJump), _jumpDuration);
    }

    private void EndJump()
    {
        _isJumping = false;
    }

    private void AlignToSurface()
    {
        Vector3 rayOrigin = transform.position + _currentNormal * 0.2f;
        Vector3 rayDir = -_currentNormal;

        if (Physics.SphereCast(rayOrigin, 0.1f, rayDir, out RaycastHit hit, _raycastDistance, _groundMask))
        {
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;

            _currentNormal = hit.normal;

            Quaternion surfaceRotation = Quaternion.FromToRotation(transform.up, _currentNormal) * transform.rotation;
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, surfaceRotation, _alignSpeed * Time.deltaTime));

            Vector3 toSurface = Vector3.Project(hit.point - transform.position, _currentNormal);
            float distanceToSurface = toSurface.magnitude;

            if (distanceToSurface > 0.05f)
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
