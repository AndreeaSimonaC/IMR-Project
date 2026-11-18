using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

[DefaultExecutionOrder(-500)]
public class XRHandInput : MonoBehaviour
{
    public enum HandSide { Left, Right }

    [Header("Hand")]
    [SerializeField] private HandSide handSide;
    [SerializeField] private Transform trackingOrigin;

    [Header("Tracked Transforms")]
    [SerializeField] private Transform indexTip;
    [SerializeField] private Transform thumbTip;
    [SerializeField] private Transform palm;
    [SerializeField] private Transform wrist;

    [Header("Pinch")]
    [SerializeField] private float pinchStartDistance = 0.025f;
    [SerializeField] private float pinchReleaseDistance = 0.04f;

    private XRHandSubsystem subsystem;
    private readonly List<XRHandSubsystem> subsystems = new();

    private bool physicalPinch;
    private bool requireRelease = true;

    public bool IsTracked { get; private set; }
    public bool IsPinching { get; private set; }
    public bool PinchStarted { get; private set; }
    public bool PinchEnded { get; private set; }
    public Vector3 IndexTipPosition { get; private set; }
    public Vector3 ThumbTipPosition { get; private set; }
    public Vector3 PalmPosition { get; private set; }
    public Quaternion PalmRotation { get; private set; }
    public Vector3 WristPosition { get; private set; }
    public Vector3 PinchPosition => (IndexTipPosition + ThumbTipPosition) * 0.5f;

    public float PinchDistance { get; private set; }

    private void OnEnable()
    {
        ResetInput();
    }

    private void OnDisable()
    {
        ResetInput();
    }

    private void Update()
    {
        PinchStarted = false;
        PinchEnded = false;

        if (subsystem == null || !subsystem.running)
        {
            SubsystemManager.GetSubsystems(subsystems);
            subsystem = subsystems.Find(s => s.running);
        }

        if (subsystem == null || !subsystem.running)
        {
            ResetInput();
            return;
        }

        XRHand hand = handSide == HandSide.Right ? subsystem.rightHand : subsystem.leftHand;

        if (!hand.isTracked || indexTip == null || thumbTip == null)
        {
            ResetInput();
            return;
        }

        IsTracked = true;

        IndexTipPosition = indexTip.position;
        ThumbTipPosition = thumbTip.position;

        if (palm != null)
        {
            PalmPosition = palm.position;
            PalmRotation = palm.rotation;
        }
        else if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose))
        {
            PalmPosition = ToWorld(palmPose.position);
            PalmRotation = trackingOrigin != null ? trackingOrigin.rotation * palmPose.rotation : palmPose.rotation;
        }

        if (wrist != null)
            WristPosition = wrist.position;
        else if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wristPose))
            WristPosition = ToWorld(wristPose.position);

        UpdatePinch();
    }

    private Vector3 ToWorld(Vector3 position)
    {
        return trackingOrigin != null ? trackingOrigin.TransformPoint(position) : position;
    }

    private void UpdatePinch()
    {
        PinchDistance = Vector3.Distance(IndexTipPosition, ThumbTipPosition);

        float start = Mathf.Min(pinchStartDistance, pinchReleaseDistance);
        float release = Mathf.Max(pinchStartDistance, pinchReleaseDistance);

        if (physicalPinch)
        {
            if (PinchDistance >= release) physicalPinch = false;
        }
        else if (PinchDistance <= start)
        {
            physicalPinch = true;
        }

        if (requireRelease)
        {
            IsPinching = false;
            if (!physicalPinch) requireRelease = false;
            return;
        }

        bool previous = IsPinching;
        IsPinching = physicalPinch;

        PinchStarted = !previous && IsPinching;
        PinchEnded = previous && !IsPinching;
    }

    public void RequireFreshPinch()
    {
        IsPinching = false;
        PinchStarted = false;
        PinchEnded = false;
        requireRelease = true;
    }

    private void ResetInput()
    {
        IsTracked = false;
        physicalPinch = false;
        requireRelease = true;

        IsPinching = false;
        PinchStarted = false;
        PinchEnded = false;
    }
}