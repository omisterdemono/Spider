using UnityEngine;

public class IKSpiderLegSolver : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private Transform body;
    [SerializeField] private IKSpiderLegSolver pairedLeg; // Інша лапа з групи

    [Header("Settings")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Vector3 footOffset;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float stepDistance = 0.4f;
    [SerializeField] private float _minDistance = 0.01f;
    [SerializeField] private float stepLength = 0.4f;
    [SerializeField] private float stepHeight = 0.1f;
    [SerializeField] private float raycastRange = 2f;

    private float footSpacing;
    private Vector3 oldPosition, currentPosition, newPosition;
    private Vector3 oldNormal, currentNormal, newNormal;
    private float lerp = 1f;

    private void Start()
    {
        footSpacing = transform.localPosition.x;

        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.up;
        lerp = 1f;
    }

    private void Update()
    {
        
        transform.position = currentPosition;
        transform.up = currentNormal;

        Vector3 rayOrigin = body.position + (body.right * footOffset.x) + (body.forward * footOffset.z) ;
        Debug.Log(rayOrigin);
        Ray ray = new Ray(rayOrigin + body.up * 1.5f, -body.up);
        Debug.DrawRay(ray.origin, ray.direction * raycastRange, Color.cyan);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, terrainLayer))
        {
            float distanceToNewPoint = Vector3.Distance(newPosition, hit.point);

            bool shouldStep = distanceToNewPoint > stepDistance && distanceToNewPoint > _minDistance;

            if (shouldStep && !IsMoving() && !pairedLeg.IsMoving())
            {
                lerp = 0f;

                int direction = body.InverseTransformPoint(hit.point).z > body.InverseTransformPoint(newPosition).z ? 1 : -1;
                newPosition = hit.point + (body.forward * stepLength * direction);
                newNormal = hit.normal;
            }
        }

        if (lerp < 1f)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);

            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    public bool IsMoving()
    {
        return lerp < 1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.02f);
    }

#if UNITY_EDITOR
    [ContextMenu("Compute Foot Offset")]
    private void ComputeFootOffset()
    {
        if (body == null)
        {
            Debug.LogWarning("Body reference not assigned.");
            return;
        }

        footOffset = body.InverseTransformPoint(transform.position);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[IKSpiderLegSolver] Foot offset updated to {footOffset}", this);
    }
#endif
}
