using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderLegController : MonoBehaviour
{
    [System.Serializable]
    public class SpiderLeg
    {
        public string name;
        public Transform target;  // LegX_Target
        public Transform hint;    // LegX_Hint
        [HideInInspector] public Vector3 footOffset;
        [HideInInspector] public Vector3 lastPosition;
        [HideInInspector] public bool isStepping = false;
    }

    [Header("General")]
    [SerializeField] private Transform body;
    [SerializeField] private LayerMask terrainMask;
    [SerializeField] private bool autoDetectOffsets = true;

    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 1f;
    [SerializeField] private float raycastLength = 2f;

    [Header("Step Settings")]
    [SerializeField] private float stepDistance = 0.5f;
    [SerializeField] private float stepHeight = 0.2f;
    [SerializeField] private float stepSpeed = 5f;
    [SerializeField] private float stepDelay = 0.1f;

    [Header("Leg Groups")]
    [Tooltip("Ліва 1+3 та Права 2+4")]
    [SerializeField] private SpiderLeg[] groupA;
    [Tooltip("Права 1+3 та Ліва 2+4")]
    [SerializeField] private SpiderLeg[] groupB;

    private bool _isGroupAStepping = false;
    private bool _isGroupBStepping = false;

    private void Start()
    {
        InitLegs(groupA);
        InitLegs(groupB);
        StartCoroutine(StepRoutine());
    }

    private void InitLegs(SpiderLeg[] group)
    {
        foreach (var leg in group)
        {
            if (leg.target == null)
            {
                Debug.LogWarning($"SpiderLeg '{leg.name}' не має Target!", this);
                continue;
            }

            if (autoDetectOffsets)
                leg.footOffset = body.InverseTransformPoint(leg.target.position);

            Vector3 worldFootPos = body.TransformPoint(leg.footOffset);
            leg.target.position = worldFootPos;
            leg.lastPosition = worldFootPos;
        }
    }

    private IEnumerator StepRoutine()
    {
        while (true)
        {
            yield return TryMoveGroup(groupA, true);
            yield return new WaitForSeconds(stepDelay);
            yield return TryMoveGroup(groupB, false);
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private IEnumerator TryMoveGroup(SpiderLeg[] group, bool isGroupA)
    {
        if ((isGroupA && _isGroupAStepping) || (!isGroupA && _isGroupBStepping))
            yield break;

        if (isGroupA) _isGroupAStepping = true;
        else _isGroupBStepping = true;

        List<Coroutine> stepCoroutines = new List<Coroutine>();

        foreach (var leg in group)
        {
            Vector3 worldFootPos = body.TransformPoint(leg.footOffset);
            Vector3 rayOrigin = worldFootPos + Vector3.up * raycastHeight;

            if (Physics.Raycast(rayOrigin, -transform.up, out RaycastHit hit, raycastHeight + raycastLength, terrainMask))
            {
                float distance = Vector3.Distance(leg.target.position, hit.point);
                if (distance > stepDistance && !leg.isStepping)
                {
                    stepCoroutines.Add(StartCoroutine(StepLeg(leg, hit.point)));
                }
            }
            Debug.DrawRay(rayOrigin, -transform.up * (raycastHeight + raycastLength), Color.aliceBlue);
        }

        foreach (var c in stepCoroutines)
            yield return c;

        if (isGroupA) _isGroupAStepping = false;
        else _isGroupBStepping = false;
    }

    private IEnumerator StepLeg(SpiderLeg leg, Vector3 targetPos)
    {
        leg.isStepping = true;

        Vector3 start = leg.target.position;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * stepSpeed;
            float t = Mathf.Clamp01(elapsed);

            Vector3 foot = Vector3.Lerp(start, targetPos, t);
            foot += Vector3.up * Mathf.Sin(t * Mathf.PI) * stepHeight;

            leg.target.position = foot;

            if (leg.hint != null)
            {
                Vector3 mid = (body.position + foot) * 0.5f;
                leg.hint.position = mid + body.right * 0.1f;
            }

            yield return null;
        }

        leg.lastPosition = targetPos;
        leg.isStepping = false;
    }
}
