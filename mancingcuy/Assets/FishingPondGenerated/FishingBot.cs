using UnityEngine;

public class FishingBot : MonoBehaviour
{
    public Vector3 homePosition;
    public float wanderRadius = 3f;
    public float moveSpeed = 1.2f;
    public GameObject fishPrefab;
    public RuntimeAnimatorController fishingAnimatorController;

    // Same state machine concept as FishingPlayerController
    private enum FishingState { Idle, Casting, WaitingForBite, Pulling, ReelBack }
    private FishingState state = FishingState.Idle;

    private Vector3 target;
    private float waitTimer;
    private Transform rod;
    private Transform rodTip;
    private Transform rodRightGrip;
    private Transform rodLeftGrip;
    private Transform fishingLine;
    private Transform bobber;
    private GameObject caughtFish;
    private float stateTimer;
    private bool sessionFishing;
    private bool atFishingSpot;
    private Vector3 waterPoint;
    private Animator fishingAnimator;
    private FishingLocomotionAnimator locomotionAnimator;
    private Transform rightHandBone;
    private float ikWeight;
    private CharacterController characterController;
    private float gravity;

    private float clipCastDuration = 1.2f;
    private float clipFightDuration = 1.8f;
    private const float ReelBackDuration = 0.6f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
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

        PickTarget();
    }

    private void CacheClipDurations()
    {
        if (fishingAnimator == null || fishingAnimator.runtimeAnimatorController == null)
            return;
        foreach (var clip in fishingAnimator.runtimeAnimatorController.animationClips)
        {
            var n = clip.name;
            if (n.Contains("Begin") || n.Contains("Cast"))
                clipCastDuration = clip.length;
            else if (n.Contains("Fight") || n.Contains("Pull") || n.Contains("Reel"))
                clipFightDuration = clip.length;
        }
    }

    // --- Public API (same contract as FishingPlayerController) ---

    public bool IsReadyToFish()
    {
        return sessionFishing && atFishingSpot && state == FishingState.WaitingForBite;
    }

    public void BeginFishingSession(Vector3 fishingSpot)
    {
        sessionFishing = true;
        atFishingSpot = false;
        state = FishingState.Idle;
        waitTimer = 0f;
        target = fishingSpot;
        stateTimer = 0f;
        SetFishingLineVisible(false);
        if (bobber != null)
            bobber.GetComponent<Renderer>().enabled = false;
    }

    public void EndFishingSession()
    {
        var wasFishing = state != FishingState.Idle;
        sessionFishing = false;
        atFishingSpot = false;
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
        PickTarget();
    }

    public void TriggerCatch(float weight)
    {
        if (state != FishingState.WaitingForBite)
            return;
        state = FishingState.Pulling;
        stateTimer = clipFightDuration;
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

    // --- Update ---

    private void Update()
    {
        AnimateFishing();

        if (state == FishingState.Pulling || state == FishingState.ReelBack)
            return;
        if (sessionFishing && atFishingSpot)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        var offset = target - transform.position;
        offset.y = 0f;
        if (offset.magnitude < 1.0f)
        {
            if (sessionFishing)
            {
                atFishingSpot = true;
                // Face toward pond center
                var towardPond = new Vector3(-transform.position.x, 0f, -transform.position.z).normalized;
                transform.forward = towardPond;
                // Cast line — random distance 4~8m toward pond center (can reach middle)
                var castDist = Random.Range(4f, 8f);
                waterPoint = transform.position + towardPond * castDist;
                waterPoint.y = 0.15f; // water surface
                state = FishingState.Casting;
                stateTimer = clipCastDuration;
                if (bobber != null)
                    bobber.GetComponent<Renderer>().enabled = true;
                PlayFishingAnimation("Fishing Begin");
                return;
            }
            waitTimer = Random.Range(1.5f, 4f);
            PickTarget();
            return;
        }

        var direction = offset.normalized;
        var move = direction * moveSpeed;
        if (characterController != null && !characterController.isGrounded)
            gravity -= 9.81f * Time.deltaTime;
        else
            gravity = -0.5f;
        move.y = gravity;
        if (characterController != null)
            characterController.Move(move * Time.deltaTime);
        else
            transform.position += direction * moveSpeed * Time.deltaTime;
        transform.forward = Vector3.Slerp(transform.forward, direction, 5f * Time.deltaTime);
    }

    // --- Animate fishing (mirrors FishingPlayerController.AnimateFishing) ---

    private void AnimateFishing()
    {
        if (state == FishingState.Idle)
            return;

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case FishingState.Casting:
            {
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
                var pullProgress = 1f - Mathf.Clamp01(stateTimer / clipFightDuration);
                if (caughtFish != null)
                {
                    var handPoint = transform.position + transform.forward * 0.55f + Vector3.up * 1.15f;
                    caughtFish.transform.position = Vector3.Lerp(waterPoint, handPoint, pullProgress);
                    caughtFish.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                }
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
                    // Re-cast with new random distance
                    var towardPond = new Vector3(-transform.position.x, 0f, -transform.position.z).normalized;
                    var castDist = Random.Range(4f, 8f);
                    waterPoint = transform.position + towardPond * castDist;
                    waterPoint.y = 0.15f;
                    state = FishingState.Casting;
                    stateTimer = clipCastDuration;
                    if (bobber != null)
                        bobber.GetComponent<Renderer>().enabled = true;
                    PlayFishingAnimation("Fishing Begin");
                }
                break;
        }

        // Fishing line — same logic as player
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
        bobber.name = "FishingBobber";
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

    private void SetFishingLineVisible(bool visible)
    {
        if (fishingLine != null)
            fishingLine.GetComponent<Renderer>().enabled = visible;
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

    private void PickTarget()
    {
        var random = Random.insideUnitCircle * wanderRadius;
        target = homePosition + new Vector3(random.x, 0f, random.y);
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
