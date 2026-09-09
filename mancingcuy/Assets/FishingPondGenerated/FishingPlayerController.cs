using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingPlayerController : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public Transform cameraTransform;
    public GameObject fishPrefab;
    public RuntimeAnimatorController fishingAnimatorController;

    private enum FishingState { Idle, ReadyToCast, Casting, WaitingForBite, Pulling, ReelBack }
    private FishingState state = FishingState.Idle;

    private Transform rod;
    private Transform rodTip;
    private Transform rodRightGrip;
    private Transform rodLeftGrip;
    private Transform fishingLine;
    private Transform bobber;
    private readonly List<float> inventory = new List<float>();
    private Vector3 waterPoint;
    private float stateTimer;          // single timer driven by clip length
    private GameObject caughtFish;
    private Animator fishingAnimator;
    private FishingLocomotionAnimator locomotionAnimator;
    private Transform rightHandBone;
    private float ikWeight;

    // ponytail: clip durations cached once — upgrade to dict if clip count grows
    private float clipCastDuration = 1.2f;
    private float clipFightDuration = 1.8f;
    private const float ReelBackDuration = 0.6f;

    private void Start()
    {
        rod = transform.Find("HeldFishingRod");
        rodTip = rod != null ? FindDeepChild(rod, "Pole_Tip") : null;
        rodRightGrip = rod != null ? FindDeepChild(rod, "RodRightGrip") : null;
        rodLeftGrip = rod != null ? FindDeepChild(rod, "RodLeftGrip") : null;
        fishingLine = transform.Find("HeldFishingLine");
        CreateBobber();
        SetFishingLineVisible(false);
        fishingAnimator = GetComponentInChildren<Animator>();
        locomotionAnimator = GetComponent<FishingLocomotionAnimator>();
        ConfigureFishingAnimator();
        CacheClipDurations();

        if (fishingAnimator != null && fishingAnimator.isHuman)
        {
            rightHandBone = fishingAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHandBone != null && rod != null)
            {
                rod.SetParent(rightHandBone, false);
                rod.localPosition = new Vector3(0.02f, 0.08f, 0.03f);
                rod.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
        }
    }

    private void CacheClipDurations()
    {
        if (fishingAnimator == null || fishingAnimator.runtimeAnimatorController == null)
            return;
        foreach (var clip in fishingAnimator.runtimeAnimatorController.animationClips)
        {
            // Kevin Iglesias clips: names contain "Fishing01_Begin", "Fishing01_Fighting" etc.
            var n = clip.name;
            if (n.Contains("Begin") || n.Contains("Cast"))
                clipCastDuration = clip.length;
            else if (n.Contains("Fight") || n.Contains("Pull") || n.Contains("Reel"))
                clipFightDuration = clip.length;
        }
    }

    // --- Public API ---

    public bool HasCastLine => state == FishingState.WaitingForBite;
    public bool IsReadyToCast => state == FishingState.ReadyToCast;
    public bool IsPulling => state == FishingState.Pulling || state == FishingState.ReelBack;
    public int InventoryCount => inventory.Count;

    public float InventoryTotalWeight
    {
        get
        {
            var total = 0f;
            for (var i = 0; i < inventory.Count; i++)
                total += inventory[i];
            return total;
        }
    }

    public float GetLastCatchWeight()
    {
        return inventory.Count == 0 ? 0f : inventory[inventory.Count - 1];
    }

    public bool IsFishingSessionActive()
    {
        return state != FishingState.Idle;
    }

    public string GetFishingDebugState()
    {
        return state.ToString();
    }

    public void BeginFishingSession()
    {
        state = FishingState.ReadyToCast;
        stateTimer = 0f;
        SetFishingLineVisible(false);
        if (bobber != null)
            bobber.GetComponent<Renderer>().enabled = false;
    }

    public void HandleEnterInput()
    {
        if (state == FishingState.ReadyToCast)
            CastFishingRod();
    }

    public void HandleCastInput()
    {
        if (state == FishingState.ReadyToCast)
            CastFishingRod();
    }

    public void CastFishingRod()
    {
        if (state != FishingState.ReadyToCast)
            return;

        state = FishingState.Casting;
        stateTimer = clipCastDuration;
        waterPoint = transform.position + transform.forward * 2.5f + Vector3.up * 0.3f;
        if (bobber != null)
            bobber.GetComponent<Renderer>().enabled = true;
        PlayFishingAnimation("Fishing Begin");
    }

    public void TriggerCatch(float weight)
    {
        if (state != FishingState.WaitingForBite)
            return;

        state = FishingState.Pulling;
        stateTimer = clipFightDuration;
        inventory.Add(weight);
        if (caughtFish != null)
            Destroy(caughtFish);
        if (fishPrefab != null)
        {
            caughtFish = Instantiate(fishPrefab, waterPoint, Quaternion.identity);
            caughtFish.name = "CaughtFishVisual";
            caughtFish.transform.localScale = Vector3.one * 0.55f;
        }
        PlayFishingAnimation("Fishing Fighting");
    }

    public void EndFishingSession()
    {
        var wasFishing = state != FishingState.Idle && state != FishingState.ReadyToCast;

        state = FishingState.Idle;
        stateTimer = 0f;
        SetFishingLineVisible(false);
        if (bobber != null)
            bobber.GetComponent<Renderer>().enabled = false;
        if (caughtFish != null)
        {
            Destroy(caughtFish);
            caughtFish = null;
        }

        if (wasFishing)
            PlayFishingAnimation("Fishing Stop");
    }

    // --- Update ---

    private void Update()
    {
        if (state == FishingState.ReadyToCast && IsLeftClickPressed())
            HandleCastInput();

        AnimateFishing();

        if (state == FishingState.Idle || state == FishingState.ReadyToCast)
            HandleMovement();
    }

    private void HandleMovement()
    {
        var input = Vector3.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.z -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.z += 1f;
        }
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        var activeCamera = cameraTransform;
        if (activeCamera == null && Camera.main != null)
            activeCamera = Camera.main.transform;

        if (activeCamera != null && input.sqrMagnitude > 0.01f)
        {
            var cameraForward = activeCamera.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            var cameraRight = activeCamera.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
            input = cameraRight * input.x + cameraForward * input.z;
        }

        transform.position += input * moveSpeed * Time.deltaTime;
        if (input.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Slerp(transform.forward, input, 12f * Time.deltaTime);
    }

    private void AnimateFishing()
    {
        if (state == FishingState.Idle || state == FishingState.ReadyToCast)
            return;

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case FishingState.Casting:
            {
                // Bobber flies from rod tip to water, synced to clip progress
                var progress = 1f - Mathf.Clamp01(stateTimer / clipCastDuration);
                if (bobber != null)
                    bobber.position = Vector3.Lerp(GetRodTip(), waterPoint, progress);
                if (stateTimer <= 0f)
                {
                    state = FishingState.WaitingForBite;
                    SetFishingLineVisible(true);
                    PlayFishingAnimation("Fishing Loop");
                }
                break;
            }

            case FishingState.WaitingForBite:
                if (bobber != null)
                    bobber.position = waterPoint + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * 0.08f);
                break;

            case FishingState.Pulling:
            {
                if (bobber != null)
                    bobber.GetComponent<Renderer>().enabled = false;
                // Fish moves from water to hand, synced to fight clip
                var pullProgress = 1f - Mathf.Clamp01(stateTimer / clipFightDuration);
                if (caughtFish != null)
                {
                    var handPoint = transform.position + transform.forward * 0.55f + Vector3.up * 1.15f;
                    caughtFish.transform.position = Vector3.Lerp(waterPoint, handPoint, pullProgress);
                    caughtFish.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                }
                // Destroy fish and transition only when clip is done
                if (stateTimer <= 0f)
                {
                    if (caughtFish != null)
                    {
                        Destroy(caughtFish);
                        caughtFish = null;
                    }
                    state = FishingState.ReelBack;
                    stateTimer = ReelBackDuration;
                }
                break;
            }

            case FishingState.ReelBack:
                if (stateTimer <= 0f)
                {
                    state = FishingState.Casting;
                    stateTimer = clipCastDuration;
                    waterPoint = transform.position + transform.forward * 2.5f + Vector3.up * 0.3f;
                    if (bobber != null)
                        bobber.GetComponent<Renderer>().enabled = true;
                    PlayFishingAnimation("Fishing Begin");
                }
                break;
        }

        // Fishing line
        var showLine = state == FishingState.WaitingForBite || state == FishingState.Pulling;
        if (fishingLine != null && showLine)
        {
            SetFishingLineVisible(true);
            var lineEnd = caughtFish != null ? caughtFish.transform.position :
                bobber != null ? bobber.position : GetRodTip();
            var lineVector = lineEnd - GetRodTip();
            if (lineVector.sqrMagnitude < 0.0001f)
                return;
            fishingLine.position = GetRodTip() + lineVector * 0.5f;
            fishingLine.rotation = Quaternion.FromToRotation(Vector3.up, lineVector.normalized);
            fishingLine.localScale = new Vector3(0.012f, lineVector.magnitude * 0.5f, 0.012f);
        }
        else
        {
            SetFishingLineVisible(false);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (fishingAnimator == null || !fishingAnimator.isHuman)
            return;

        var ikActive = state == FishingState.Casting || state == FishingState.WaitingForBite
            || state == FishingState.Pulling || state == FishingState.ReelBack;
        var targetWeight = ikActive ? 1f : 0f;
        ikWeight = Mathf.MoveTowards(ikWeight, targetWeight, Time.deltaTime * 5f);

        if (ikWeight < 0.01f)
            return;

        var leftGrip = rodLeftGrip != null ? rodLeftGrip.position :
            (rod != null ? rod.TransformPoint(Vector3.up * 0.50f) : transform.position + transform.forward * 0.35f + Vector3.up * 1.1f);
        var rodDir = rod != null ? rod.up : transform.forward;
        var leftHandRot = Quaternion.LookRotation(rodDir, transform.up);

        fishingAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
        fishingAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight * 0.7f);
        fishingAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftGrip);
        fishingAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRot);

        fishingAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight * 0.5f);
        fishingAnimator.SetIKRotation(AvatarIKGoal.RightHand, leftHandRot);
    }

    // --- Helpers ---

    private void CreateBobber()
    {
        bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        bobber.name = "PlayerFishingBobber";
        bobber.SetParent(transform);
        bobber.localPosition = new Vector3(1.65f, 0.25f, 0.72f);
        bobber.localScale = new Vector3(0.16f, 0.28f, 0.16f);
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(1f, 0.05f, 0.02f);
        bobber.GetComponent<Renderer>().sharedMaterial = material;
        bobber.GetComponent<Renderer>().enabled = false;
    }

    private Vector3 GetRodTip()
    {
        if (rod == null)
            return transform.position + Vector3.up * 1.5f;
        if (rodTip != null)
            return rodTip.position;
        return rod.position + rod.up * rod.lossyScale.y;
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

    private void SetFishingLineVisible(bool visible)
    {
        if (fishingLine != null)
            fishingLine.GetComponent<Renderer>().enabled = visible;
    }

    private static bool IsLeftClickPressed()
    {
        var inputSystemClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        var legacyClick = false;
        try
        {
            legacyClick = Input.GetMouseButtonDown(0);
        }
        catch (System.InvalidOperationException)
        {
        }
        return inputSystemClick || legacyClick;
    }

    private void PlayFishingAnimation(string stateName)
    {
        if (fishingAnimator == null)
            fishingAnimator = GetComponentInChildren<Animator>();
        if (fishingAnimator == null)
            return;

        if (locomotionAnimator != null)
            locomotionAnimator.enabled = stateName == "Fishing Stop";

        var stateHash = Animator.StringToHash(stateName);
        var baseLayerStateHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (!fishingAnimator.HasState(0, stateHash) && !fishingAnimator.HasState(0, baseLayerStateHash))
            return;

        fishingAnimator.enabled = true;
        fishingAnimator.Play(fishingAnimator.HasState(0, stateHash) ? stateHash : baseLayerStateHash, 0, 0f);
    }

    private void ConfigureFishingAnimator()
    {
        if (fishingAnimator == null || fishingAnimatorController == null)
            return;

        fishingAnimator.runtimeAnimatorController = fishingAnimatorController;
        fishingAnimator.enabled = false;
    }
}
