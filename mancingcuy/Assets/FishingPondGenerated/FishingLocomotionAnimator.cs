using UnityEngine;

public class FishingLocomotionAnimator : MonoBehaviour
{
    private Transform upperArmLeft;
    private Transform upperArmRight;
    private Transform thighLeft;
    private Transform thighRight;
    private Transform spine;
    private Quaternion upperArmLeftRest;
    private Quaternion upperArmRightRest;
    private Quaternion thighLeftRest;
    private Quaternion thighRightRest;
    private Quaternion spineRest;
    private Quaternion upperArmLeftPose;
    private Quaternion upperArmRightPose;
    private Vector3 lastPosition;
    private float gaitTime;
    private float movementBlend;

    private void Start()
    {
        upperArmLeft = FindDeepChild(transform, "upper_arm.L");
        upperArmRight = FindDeepChild(transform, "upper_arm.R");
        thighLeft = FindDeepChild(transform, "thigh.L");
        thighRight = FindDeepChild(transform, "thigh.R");
        spine = FindDeepChild(transform, "spine.001");
        StoreRestPose();
        upperArmLeftPose = Quaternion.Euler(0f, 0f, 48f);
        upperArmRightPose = Quaternion.Euler(0f, 0f, -48f);
        lastPosition = transform.position;
    }

    // LateUpdate so procedural gait layers on top of Animator output
    private void LateUpdate()
    {
        var displacement = transform.position - lastPosition;
        displacement.y = 0f;
        var speed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        var targetBlend = Mathf.Clamp01(speed / 1.2f);
        movementBlend = Mathf.MoveTowards(movementBlend, targetBlend, Time.deltaTime * 7f);
        if (movementBlend > 0.01f)
            gaitTime += Time.deltaTime * Mathf.Lerp(3.5f, 8f, movementBlend);

        var stride = Mathf.Sin(gaitTime);
        var oppositeStride = Mathf.Sin(gaitTime + Mathf.PI);
        ApplyRotation(upperArmLeft, upperArmLeftRest, upperArmLeftPose * Quaternion.Euler(oppositeStride * 10f * movementBlend, 0f, 0f));
        ApplyRotation(upperArmRight, upperArmRightRest, upperArmRightPose * Quaternion.Euler(stride * 10f * movementBlend, 0f, 0f));
        ApplyRotation(thighLeft, thighLeftRest, Quaternion.Euler(stride * 30f * movementBlend, 0f, 0f));
        ApplyRotation(thighRight, thighRightRest, Quaternion.Euler(oppositeStride * 30f * movementBlend, 0f, 0f));

        var breathing = Mathf.Sin(Time.time * 2.2f) * 1.5f;
        ApplyRotation(spine, spineRest, Quaternion.Euler(breathing * movementBlend, 0f, 0f));
    }

    private void StoreRestPose()
    {
        if (upperArmLeft != null) upperArmLeftRest = upperArmLeft.localRotation;
        if (upperArmRight != null) upperArmRightRest = upperArmRight.localRotation;
        if (thighLeft != null) thighLeftRest = thighLeft.localRotation;
        if (thighRight != null) thighRightRest = thighRight.localRotation;
        if (spine != null) spineRest = spine.localRotation;
    }

    private static void ApplyRotation(Transform bone, Quaternion rest, Quaternion offset)
    {
        if (bone != null)
            bone.localRotation = rest * offset;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            var result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}
