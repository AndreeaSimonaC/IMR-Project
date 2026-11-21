using UnityEngine;

public class HSVFingerDrag : MonoBehaviour
{
    [Header("HSV Handles")]
    public HSVSliderHandle hueHandle;
    public HSVSliderHandle saturationHandle;
    public HSVSliderHandle valueHandle;

    [Header("Touch Settings")]
    public float touchDistance = 0.015f;
    public float releaseDistance = 0.035f;

    private HSVSliderHandle activeHandle;

    private void Update()
    {
        Vector3 fingerPosition = transform.position;

        if (activeHandle == null) { TryTouch(fingerPosition); }
        else { DragHandle(fingerPosition); }
    }

    private void TryTouch(Vector3 fingerPosition)
    {
        HSVSliderHandle[] handles = { hueHandle, saturationHandle, valueHandle };

        float closestDistance = touchDistance;
        HSVSliderHandle closestHandle = null;

        foreach (HSVSliderHandle handle in handles)
        {
            if (handle == null || !handle.gameObject.activeInHierarchy) continue;

            Collider col = handle.GetComponentInChildren<Collider>();

            if (col == null || !col.enabled) continue;

            Vector3 closestPoint = col.ClosestPoint(fingerPosition);
            float distance = Vector3.Distance(fingerPosition, closestPoint);

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestHandle = handle;
            }
        }

        if (closestHandle != null) activeHandle = closestHandle;
    }

    private void DragHandle(Vector3 fingerPosition)
    {
        if (!activeHandle.gameObject.activeInHierarchy || activeHandle.startPoint == null || activeHandle.endPoint == null)
        {
            activeHandle = null;
            return;
        }

        Vector3 start = activeHandle.startPoint.position;
        Vector3 end = activeHandle.endPoint.position;
        Vector3 direction = end - start;

        if (direction.sqrMagnitude < 0.000001f) return;

        float value = Vector3.Dot(fingerPosition - start, direction) / direction.sqrMagnitude;

        Vector3 closestTrackPoint = start + direction * Mathf.Clamp01(value);

        float distanceFromTrack = Vector3.Distance(fingerPosition, closestTrackPoint);

        if (distanceFromTrack > releaseDistance)
        {
            activeHandle = null;
            return;
        }

        activeHandle.SetValue(value);
    }
}