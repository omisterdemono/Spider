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
    [SerializeField] private float _alignRayOffset = 0.2f;
    [SerializeField] private float _alignSurfaceThreshold = 0.05f;
    [SerializeField] private float _alignSphereRadius = 0.1f;
    [SerializeField] private LayerMask _groundMask;

    [Header("In Air Aligment")]
    [SerializeField] private float _airAlignSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private float _jumpCooldown = 0.5f;
    [SerializeField] private float _jumpDuration = 0.6f;
    [SerializeField] private float _minJumpPercent = 0.1f;
    [SerializeField] private float _maxChargeTime = 2f;
    [SerializeField] private float _upwardJumpFactor = 0.8f;

    [Header("References")]
    [SerializeField] private FreeSpringCamera _mainCamera;
    [SerializeField] private Rigidbody _rigitBody;

    private Vector3 _currentNormal = Vector3.up;
    private float _lastJumpTime = -999f;
    private bool _isJumping = false;
    private float _jumpHoldStartTime;
    private bool _isChargingJump = false;
    private bool _isOnSurface = false;

    public bool IsOnSurface { get => _isOnSurface; }

    private void Awake()
    {
        if (_rigitBody == null)
            _rigitBody = GetComponent<Rigidbody>();

        _rigitBody.useGravity = false;
        _rigitBody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigitBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleRotationInput();
        HandleJumpInput();
    }

    private void FixedUpdate()
    {
        if (!_isJumping && TryGetSurfaceBelow(out RaycastHit hit))
        {
            _isOnSurface = true;
            AlignToSurfaceByHit(hit);
        }
        else
        {
            if (_rigitBody.linearVelocity.y > 0)
            {
                AlignToSurfaceByNormal(_rigitBody.linearVelocity);
            }
            else
            {
                AlignToSurfaceByNormal(-_rigitBody.linearVelocity);
            }
        }
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 input = transform.right * horizontal + transform.forward * vertical;
            Vector3 moveDirection = Vector3.ProjectOnPlane(input, _currentNormal).normalized;
            _rigitBody.MovePosition(_rigitBody.position + moveDirection * _moveSpeed * Time.deltaTime);
        }
    }

    private void HandleRotationInput()
    {
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

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !_isJumping && !_isChargingJump)
        {
            _isChargingJump = true;
            _jumpHoldStartTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.Space) && _isChargingJump)
        {
            float heldTime = Time.time - _jumpHoldStartTime;
            float chargePercent = Mathf.Clamp01(heldTime / _maxChargeTime);
            chargePercent = Mathf.Max(chargePercent, _minJumpPercent);

            PerformJump(chargePercent);
            _isChargingJump = false;
        }
    }

    private void Rotate(float direction)
    {
        Quaternion deltaRotation = Quaternion.AngleAxis(direction * _rotationSpeed * Time.deltaTime, _currentNormal);
        _rigitBody.MoveRotation(deltaRotation * _rigitBody.rotation);
    }

    private void RotateTowardsCamera()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, _currentNormal).normalized;
        if (camForward.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(camForward, _currentNormal);
        Quaternion smoothedRotation = Quaternion.RotateTowards(_rigitBody.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        _rigitBody.MoveRotation(smoothedRotation);
    }

    private void PerformJump(float chargePercent)
    {
        if (Time.time - _lastJumpTime < _jumpCooldown || _isJumping) return;

        _lastJumpTime = Time.time;
        _isJumping = true;
        _rigitBody.useGravity = true;
        _isOnSurface = false;

        Vector3 jumpDirection = Vector3.ProjectOnPlane(_mainCamera.transform.forward, _currentNormal).normalized;
        if (jumpDirection.sqrMagnitude < 0.01f)
        {
            jumpDirection = transform.forward;
        }

        Vector3 upward = _currentNormal * _upwardJumpFactor;
        Vector3 jumpVector = (jumpDirection + upward).normalized;

        _rigitBody.linearVelocity = Vector3.zero;
        _rigitBody.AddForce(jumpVector * (_jumpForce * chargePercent), ForceMode.VelocityChange);

        Invoke(nameof(EndJump), _jumpDuration);
    }

    private void EndJump()
    {
        _isJumping = false;
    }

    private void AlignToSurfaceByHit(RaycastHit hit)
    {
        _rigitBody.useGravity = false;
        _rigitBody.linearVelocity = Vector3.zero;

        _currentNormal = hit.normal;

        Quaternion surfaceRotation = Quaternion.FromToRotation(transform.up, _currentNormal) * transform.rotation;
        Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, surfaceRotation, _alignSpeed * Time.deltaTime);
        _rigitBody.MoveRotation(smoothedRotation);

        Vector3 toSurface = Vector3.Project(hit.point - transform.position, _currentNormal);
        float distance = toSurface.magnitude;

        if (distance > _alignSurfaceThreshold)
        {
            _rigitBody.MovePosition(transform.position + toSurface.normalized * distance * _stickToSurfaceForce * Time.deltaTime);
        }
    }

    private void AlignToSurfaceByNormal(Vector3 normal)
    {
        _currentNormal = Vector3.Slerp(_currentNormal, normal.normalized, _airAlignSpeed * Time.deltaTime);

        Quaternion surfaceRotation = Quaternion.FromToRotation(transform.up, _currentNormal) * transform.rotation;
        Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, surfaceRotation, _airAlignSpeed * Time.deltaTime);
        _rigitBody.MoveRotation(smoothedRotation);
        Debug.DrawRay(transform.position,_rigitBody.linearVelocity,Color.purple);
    }

    private bool TryGetSurfaceBelow(out RaycastHit hit)
    {
        Vector3 rayOrigin = transform.position + transform.up * _alignRayOffset;
        Vector3 rayDir = -transform.up;

        bool found = Physics.SphereCast(rayOrigin, _alignSphereRadius, rayDir, out hit, _raycastDistance, _groundMask);
        Debug.DrawRay(rayOrigin, rayDir * _raycastDistance, found ? Color.green : Color.red);

        return found;
    }
}
