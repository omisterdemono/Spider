using UnityEngine;

public class Spider : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _rotationSpeed = 40f;
    [SerializeField] private FreeSpringCamera _camera;
    [SerializeField] private ColliderEventTrigger _surfaceCollider;
    [SerializeField] private Rigidbody _rigidbody;
    private bool _onSurface = true;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        _surfaceCollider.ColliderTrigered += OnSurface;
    }

    void Update()
    {
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            Move();
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            // Target Y rotation based on camera
            float targetY = _camera.transform.rotation.eulerAngles.y;

            // Current rotation
            Quaternion currentRotation = transform.rotation;

            // Target rotation with only Y affected
            Quaternion targetRotation = Quaternion.Euler(0f, targetY, 0f);

            // Smoothly rotate towards the target
            transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, 90f * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            Rotate(-1f);
        }

        if (Input.GetKey(KeyCode.E))
        {
            Rotate(1f);
        }

        if (Input.GetKey(KeyCode.Space)&& _onSurface)
        {
            _rigidbody.AddForce(new Vector3(0f, 200, 0f));
            _rigidbody.useGravity = true;
            _onSurface = false;
        }
    }

    private void Move()
    {
        //Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        Vector3 direction = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");

        transform.Translate(direction * _speed * Time.deltaTime, Space.World);
    }

    private void Rotate(float direction = 1f)
    {
        transform.eulerAngles += new Vector3(0f, direction * _rotationSpeed * Time.deltaTime, 0f);
    }

    private void OnSurface()
    {
        _rigidbody.useGravity = false;
        _onSurface = true;
        Debug.Log("On Surface!");
    }
}
