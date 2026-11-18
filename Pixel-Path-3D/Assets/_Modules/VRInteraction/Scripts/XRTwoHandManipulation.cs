using UnityEngine;

[DefaultExecutionOrder(-300)]
public class XRTwoHandManipulation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInputCoordinator coordinator;

    [Header("Settings")]
    [SerializeField] private float minHandDistance = 0.01f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 10f;

    public bool IsManipulating { get; private set; }
    public bool StartedThisFrame { get; private set; }
    public bool EndedThisFrame { get; private set; }

    public Vector3 StartCenter { get; private set; }
    public Vector3 CurrentCenter { get; private set; }
    public Vector3 Translation { get; private set; }

    public Quaternion Rotation { get; private set; } = Quaternion.identity;
    public float ScaleFactor { get; private set; } = 1f;

    private Vector3 startDirection;
    private float startDistance;

    private void Update()
    {
        StartedThisFrame = false;
        EndedThisFrame = false;

        if (!CanManipulate())
        {
            EndManipulation();
            return;
        }

        if (!IsManipulating)
        {
            BeginManipulation();
            return;
        }

        UpdateManipulation();
    }

    private bool CanManipulate()
    {
        if (coordinator == null || !coordinator.BothHandsPinching) return false;

        return coordinator.IsToolActive(XRInputCoordinator.ToolType.Move) ||
               coordinator.IsToolActive(XRInputCoordinator.ToolType.Rotate) ||
               coordinator.IsToolActive(XRInputCoordinator.ToolType.Scale);
    }

    private void BeginManipulation()
    {
        Vector3 left = coordinator.LeftHand.PinchPosition;
        Vector3 right = coordinator.RightHand.PinchPosition;
        Vector3 difference = right - left;

        if (difference.magnitude < minHandDistance) return;

        StartCenter = (left + right) * 0.5f;
        CurrentCenter = StartCenter;

        startDirection = difference.normalized;
        startDistance = difference.magnitude;

        Translation = Vector3.zero;
        Rotation = Quaternion.identity;
        ScaleFactor = 1f;

        IsManipulating = true;
        StartedThisFrame = true;
    }

    private void UpdateManipulation()
    {
        Vector3 left = coordinator.LeftHand.PinchPosition;
        Vector3 right = coordinator.RightHand.PinchPosition;
        Vector3 difference = right - left;

        float currentDistance = difference.magnitude;
        if (currentDistance < minHandDistance) return;

        CurrentCenter = (left + right) * 0.5f;
        Translation = CurrentCenter - StartCenter;
        Rotation = Quaternion.FromToRotation(startDirection, difference.normalized);
        ScaleFactor = Mathf.Clamp(currentDistance / startDistance, minScale, maxScale);
    }

    private void EndManipulation()
    {
        if (!IsManipulating) return;

        IsManipulating = false;
        EndedThisFrame = true;
    }

    private void OnDisable()
    {
        IsManipulating = false;
        StartedThisFrame = false;
        EndedThisFrame = false;
    }

    public Vector3 TransformPoint(Vector3 originalPosition)
    {
        Vector3 offset = originalPosition - StartCenter;
        return CurrentCenter + Rotation * (offset * ScaleFactor);
    }
}