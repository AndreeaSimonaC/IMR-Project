using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

[DefaultExecutionOrder(-500)]
public class XRRightHandInput : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] private Transform trackingOrigin;

    [Header("Pinch Settings (meters)")]
    [SerializeField, Min(0f)]
    private float pinchStartDistance = 0.025f;

    [SerializeField, Min(0f)]
    private float pinchReleaseDistance = 0.04f;

    [Header("Interaction")]
    [SerializeField] private bool menuOpen = false;

    private XRHandSubsystem handSubsystem;
    private readonly List<XRHandSubsystem> subsystems = new();
    private bool physicalPinch;
    private bool requiresRelease = true;
    public bool IsHandTracked { get; private set; }
    public bool IsPinching { get; private set; }
    public bool PinchStarted { get; private set; }
    public bool PinchHeld => IsPinching;
    public bool PinchEnded { get; private set; }
    public bool IsMenuOpen => menuOpen;
    public bool CanInteract => IsHandTracked && !menuOpen && !requiresRelease;

    public Vector3 IndexTipPosition { get; private set; }
    public Vector3 ThumbTipPosition { get; private set; }
    public Vector3 HandPosition { get; private set; }
    public Vector3 PointerDirection { get; private set; } = Vector3.forward;

    public Ray PointerRay => new Ray(IndexTipPosition, PointerDirection);

    public float PinchDistance { get; private set; }

    private void OnEnable()
    {
        ResetInteraction();
        FindHandSubsystem();
    }

    private void OnDisable()
    {
        ResetInteraction();
    }

    private void FindHandSubsystem()
    {
        SubsystemManager.GetSubsystems(subsystems);

        handSubsystem = null;

        foreach (var subsystem in subsystems)
        {
            if (subsystem.running)
            {
                handSubsystem = subsystem;
                break;
            }
        }
    }

    private void Update()
    {
        PinchStarted = false;
        PinchEnded = false;

        if (handSubsystem == null || !handSubsystem.running)
        {
            FindHandSubsystem();
        }

        if (handSubsystem == null || !handSubsystem.running)
        {
            LoseTracking();
            return;
        }

        XRHand hand = handSubsystem.rightHand;

        if (!hand.isTracked)
        {
            LoseTracking();
            return;
        }

        XRHandJoint indexJoint = hand.GetJoint(XRHandJointID.IndexTip);
        XRHandJoint thumbJoint = hand.GetJoint(XRHandJointID.ThumbTip);
        XRHandJoint wristJoint = hand.GetJoint(XRHandJointID.Wrist);
        XRHandJoint indexKnuckle = hand.GetJoint(XRHandJointID.IndexProximal);

        if (!indexJoint.TryGetPose(out Pose indexPose) || !thumbJoint.TryGetPose(out Pose thumbPose) || !wristJoint.TryGetPose(out Pose wristPose))
        {
            LoseTracking();
            return;
        }

        IsHandTracked = true;

        IndexTipPosition = ToWorld(indexPose.position);
        ThumbTipPosition = ToWorld(thumbPose.position);
        HandPosition = ToWorld(wristPose.position);

        if (indexKnuckle.TryGetPose(out Pose knucklePose))
        {
            Vector3 knucklePosition = ToWorld(knucklePose.position);
            Vector3 direction = IndexTipPosition - knucklePosition;

            if (direction.sqrMagnitude > 0.000001f)
            {
                PointerDirection = direction.normalized;
            }
        }

        UpdatePinch();
    }

    private Vector3 ToWorld(Vector3 trackingPosition)
    {
        return trackingOrigin != null ? trackingOrigin.TransformPoint(trackingPosition) : trackingPosition;
    }

    private void UpdatePinch()
    {
        PinchDistance = Vector3.Distance(IndexTipPosition, ThumbTipPosition);

        float startThreshold = Mathf.Min(pinchStartDistance, pinchReleaseDistance);
        float releaseThreshold = Mathf.Max(pinchStartDistance, pinchReleaseDistance);

        if (physicalPinch)
        {
            if (PinchDistance >= releaseThreshold) physicalPinch = false;
        }
        else
        {
            if (PinchDistance <= startThreshold) physicalPinch = true;
        }

        if (menuOpen)
        {
            BlockUntilRelease();
            return;
        }

        if (requiresRelease)
        {
            IsPinching = false;
            if (!physicalPinch) requiresRelease = false;

            return;
        }

        bool wasPinching = IsPinching;

        IsPinching = physicalPinch;

        PinchStarted = !wasPinching && IsPinching;
        PinchEnded = wasPinching && !IsPinching;
    }

    private void LoseTracking()
    {
        IsHandTracked = false;
        IsPinching = false;
        PinchStarted = false;
        PinchEnded = false;

        physicalPinch = false;
        requiresRelease = true;
    }

    private void BlockUntilRelease()
    {
        IsPinching = false;
        PinchStarted = false;
        PinchEnded = false;
        requiresRelease = true;
    }

    private void ResetInteraction()
    {
        IsHandTracked = false;
        physicalPinch = false;
        requiresRelease = true;

        IsPinching = false;
        PinchStarted = false;
        PinchEnded = false;
    }

    public void SetMenuOpen(bool isOpen)
    {
        if (menuOpen == isOpen)
            return;

        menuOpen = isOpen;
        BlockUntilRelease();
    }

    public void NotifyToolChanged()
    {
        BlockUntilRelease();
    }

    public void RequireFreshPinch()
    {
        BlockUntilRelease();
    }
}