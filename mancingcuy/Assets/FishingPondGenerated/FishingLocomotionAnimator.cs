using UnityEngine;

public class FishingLocomotionAnimator : MonoBehaviour
{
    private Transform spine;       // spine.001 = Unity Spine
    private Transform chest;       // spine.002 = Unity Chest
    private Transform shoulderL, shoulderR;
    private Transform upperArmL, upperArmR;
    private Transform forearmL, forearmR;
    private Transform thighL, thighR;
    private Transform shinL, shinR;
    private Transform footL, footR;

    private Quaternion spineRest, chestRest;
    private Quaternion shoulderLRest, shoulderRRest;
    private Quaternion upperArmLRest, upperArmRRest;
    private Quaternion forearmLRest, forearmRRest;
    private Quaternion thighLRest, thighRRest;
    private Quaternion shinLRest, shinRRest;
    private Quaternion footLRest, footRRest;

    private Vector3 lastPosition;
    private float gaitPhase;
    private float movementBlend;

    private void Start()
    {
        spine    = FindDeepChild(transform, "spine.001");
        chest    = FindDeepChild(transform, "spine.002");
        shoulderL = FindDeepChild(transform, "shoulder.L");
        shoulderR = FindDeepChild(transform, "shoulder.R");
        upperArmL = FindDeepChild(transform, "upper_arm.L");
        upperArmR = FindDeepChild(transform, "upper_arm.R");
        forearmL  = FindDeepChild(transform, "forearm.L");
        forearmR  = FindDeepChild(transform, "forearm.R");
        thighL   = FindDeepChild(transform, "thigh.L");
        thighR   = FindDeepChild(transform, "thigh.R");
        shinL    = FindDeepChild(transform, "shin.L");
        shinR    = FindDeepChild(transform, "shin.R");
        footL    = FindDeepChild(transform, "foot.L");
        footR    = FindDeepChild(transform, "foot.R");
        StoreRestPose();
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        var displacement = transform.position - lastPosition;
        displacement.y = 0f;
        var speed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        var targetBlend = Mathf.Clamp01(speed / 2.5f);
        movementBlend = Mathf.MoveTowards(movementBlend, targetBlend, Time.deltaTime * 8f);

        // Idle breathing even when standing still
        var breathe = Mathf.Sin(Time.time * 1.8f);
        ApplyRotation(chest, chestRest, Quaternion.Euler(breathe * 0.8f, 0f, 0f));

        if (movementBlend < 0.01f)
        {
            // Standing idle — reset all bones to rest
            ApplyRotation(spine, spineRest, Quaternion.identity);
            ApplyRotation(upperArmL, upperArmLRest, Quaternion.identity);
            ApplyRotation(upperArmR, upperArmRRest, Quaternion.identity);
            ApplyRotation(forearmL, forearmLRest, Quaternion.identity);
            ApplyRotation(forearmR, forearmRRest, Quaternion.identity);
            ApplyRotation(thighL, thighLRest, Quaternion.identity);
            ApplyRotation(thighR, thighRRest, Quaternion.identity);
            ApplyRotation(shinL, shinLRest, Quaternion.identity);
            ApplyRotation(shinR, shinRRest, Quaternion.identity);
            ApplyRotation(footL, footLRest, Quaternion.identity);
            ApplyRotation(footR, footRRest, Quaternion.identity);
            return;
        }

        // Advance gait cycle — frequency scales with speed
        gaitPhase += Time.deltaTime * Mathf.Lerp(5f, 9f, movementBlend);
        var b = movementBlend; // shorthand

        var sinL = Mathf.Sin(gaitPhase);              // left leg phase
        var sinR = Mathf.Sin(gaitPhase + Mathf.PI);   // right leg phase (opposite)

        // --- Spine / torso ---
        // Slight forward lean + lateral sway + counter-rotation
        ApplyRotation(spine, spineRest, Quaternion.Euler(
            3f * b,                          // lean forward
            sinL * 3f * b,                   // counter-rotate with stride
            sinL * 2f * b                    // lateral sway
        ));

        // --- Upper arms swing opposite to legs, relaxed at sides ---
        // No T-pose offset: arms hang naturally from rest pose
        ApplyRotation(upperArmL, upperArmLRest, Quaternion.Euler(
            sinR * 25f * b,    // swing forward/back (opposite leg)
            0f,
            0f
        ));
        ApplyRotation(upperArmR, upperArmRRest, Quaternion.Euler(
            sinL * 25f * b,
            0f,
            0f
        ));

        // --- Forearms: slight bend follows swing ---
        var forearmBendL = Mathf.Clamp01(-sinR) * 15f * b;  // bend when arm swings back
        var forearmBendR = Mathf.Clamp01(-sinL) * 15f * b;
        ApplyRotation(forearmL, forearmLRest, Quaternion.Euler(forearmBendL, 0f, 0f));
        ApplyRotation(forearmR, forearmRRest, Quaternion.Euler(forearmBendR, 0f, 0f));

        // --- Thighs: main stride ---
        ApplyRotation(thighL, thighLRest, Quaternion.Euler(sinL * 28f * b, 0f, 0f));
        ApplyRotation(thighR, thighRRest, Quaternion.Euler(sinR * 28f * b, 0f, 0f));

        // --- Shins: knee bends during back-swing (pass phase) ---
        var kneeBendL = Mathf.Clamp01(-sinL) * 35f * b;  // bend when thigh swings back
        var kneeBendR = Mathf.Clamp01(-sinR) * 35f * b;
        ApplyRotation(shinL, shinLRest, Quaternion.Euler(-kneeBendL, 0f, 0f));
        ApplyRotation(shinR, shinRRest, Quaternion.Euler(-kneeBendR, 0f, 0f));

        // --- Feet: ankle roll for ground contact feel ---
        var footRollL = sinL * 8f * b;
        var footRollR = sinR * 8f * b;
        ApplyRotation(footL, footLRest, Quaternion.Euler(footRollL, 0f, 0f));
        ApplyRotation(footR, footRRest, Quaternion.Euler(footRollR, 0f, 0f));
    }

    private void StoreRestPose()
    {
        if (spine != null)     spineRest = spine.localRotation;
        if (chest != null)     chestRest = chest.localRotation;
        if (shoulderL != null) shoulderLRest = shoulderL.localRotation;
        if (shoulderR != null) shoulderRRest = shoulderR.localRotation;
        if (upperArmL != null) upperArmLRest = upperArmL.localRotation;
        if (upperArmR != null) upperArmRRest = upperArmR.localRotation;
        if (forearmL != null)  forearmLRest = forearmL.localRotation;
        if (forearmR != null)  forearmRRest = forearmR.localRotation;
        if (thighL != null)    thighLRest = thighL.localRotation;
        if (thighR != null)    thighRRest = thighR.localRotation;
        if (shinL != null)     shinLRest = shinL.localRotation;
        if (shinR != null)     shinRRest = shinR.localRotation;
        if (footL != null)     footLRest = footL.localRotation;
        if (footR != null)     footRRest = footR.localRotation;
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
