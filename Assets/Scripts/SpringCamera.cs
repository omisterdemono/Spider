using UnityEngine;

public class FreeSpringCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float height = 2.0f;
    public float distance = 4.0f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5.0f;
    public bool invertY = false;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Spring Settings")]
    public float followSmoothness = 10.0f;

    private float yaw;
    private float pitch;
    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // Обробка обертання мишкою
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * (invertY ? 1 : -1);

        yaw += mouseX;
        pitch += mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Незалежне глобальне обертання по Y (yaw)
        Quaternion yawRotation = Quaternion.Euler(0, yaw, 0);

        // Pitch відносно локального горизонту (вверх/вниз по локальній осі)
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, Vector3.right);

        // Обертання камери: глобальне yaw + локальний pitch відносно up-напрямку target
        Quaternion cameraRotation = yawRotation * Quaternion.LookRotation(Vector3.forward, target.up) * pitchRotation;

        // Позиція цілі з урахуванням локального up об'єкта
        Vector3 targetOffset = target.position + target.up * height;
        Vector3 desiredPosition = targetOffset - cameraRotation * Vector3.forward * distance;

        // Плавне переміщення камери
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSmoothness);

        // Дивимося на об'єкт з урахуванням горизонту
        transform.LookAt(targetOffset, target.up);
    }
}
