using UnityEngine;

/// <summary>
/// Virtual joystick + action buttons for mobile WebGL.
/// Attach to any always-active GameObject (e.g. the Leaderboard object).
/// Other scripts read the static properties.
/// </summary>
public class MobileTouchControls : MonoBehaviour
{
    // --- Public static state other scripts read ---
    public static Vector2 JoystickInput { get; private set; }
    public static bool EnterPressed { get; private set; }
    public static bool SpacePressed { get; private set; }
    public static bool CastPressed { get; private set; }
    public static bool IsMobile { get; private set; }

    // Camera drag (right-half touch that isn't a button)
    public static Vector2 CameraDelta { get; private set; }

    // Joystick state
    private int joystickFingerId = -1;
    private Vector2 joystickCenter;
    private const float JoystickRadius = 80f;
    private const float JoystickDeadZone = 12f;

    // Camera drag state
    private int cameraFingerId = -1;
    private Vector2 cameraPrevPos;

    // Button flags — set by OnGUI, cleared by Update AFTER exposing for one frame
    private bool enterFlag;
    private bool spaceFlag;
    private bool castFlag;
    private bool enterConsumed;
    private bool spaceConsumed;
    private bool castConsumed;

    // Styles
    private GUIStyle btnStyle;
    private GUIStyle joystickBgStyle;
    private GUIStyle joystickKnobStyle;
    private Vector2 knobOffset;

    // Button rects for touch-filtering (OnGUI sets, ProcessTouch reads)
    private Rect enterBtnRect;
    private Rect castBtnRect;
    private Rect spaceBtnRect;
    private bool rectsReady;

    private void Start()
    {
        IsMobile = Application.isMobilePlatform ||
            (Application.platform == RuntimePlatform.WebGLPlayer && IsTouchDevice());
    }

    private static bool IsTouchDevice()
    {
        return Input.touchSupported;
    }

    private void Update()
    {
        // Re-detect
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            IsMobile = Input.touchSupported && Screen.width < 1200;

        // Expose flags that OnGUI set, then clear for next frame
        if (enterFlag && !enterConsumed)
        {
            EnterPressed = true;
            enterConsumed = true;
        }
        else
        {
            EnterPressed = false;
            enterFlag = false;
            enterConsumed = false;
        }

        if (spaceFlag && !spaceConsumed)
        {
            SpacePressed = true;
            spaceConsumed = true;
        }
        else
        {
            SpacePressed = false;
            spaceFlag = false;
            spaceConsumed = false;
        }

        if (castFlag && !castConsumed)
        {
            CastPressed = true;
            castConsumed = true;
        }
        else
        {
            CastPressed = false;
            castFlag = false;
            castConsumed = false;
        }

        if (!IsMobile)
        {
            JoystickInput = Vector2.zero;
            CameraDelta = Vector2.zero;
            return;
        }

        ProcessTouches();
    }

    private void ProcessTouches()
    {
        var camDelta = Vector2.zero;

        // --- Update active fingers ---
        // Joystick
        if (joystickFingerId >= 0)
        {
            if (!TryGetTouch(joystickFingerId, out var t) || t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                joystickFingerId = -1;
                JoystickInput = Vector2.zero;
                knobOffset = Vector2.zero;
            }
            else
            {
                var delta = t.position - joystickCenter;
                if (delta.magnitude > JoystickRadius)
                    delta = delta.normalized * JoystickRadius;
                knobOffset = delta;
                JoystickInput = delta.magnitude > JoystickDeadZone
                    ? new Vector2(delta.x / JoystickRadius, delta.y / JoystickRadius)
                    : Vector2.zero;
            }
        }

        // Camera drag
        if (cameraFingerId >= 0)
        {
            if (!TryGetTouch(cameraFingerId, out var t) || t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                cameraFingerId = -1;
            }
            else
            {
                camDelta = t.position - cameraPrevPos;
                cameraPrevPos = t.position;
            }
        }

        CameraDelta = camDelta;

        // --- Pick up new fingers ---
        for (var i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;
            if (t.fingerId == joystickFingerId || t.fingerId == cameraFingerId) continue;

            // Skip if touch is on a button rect (let OnGUI handle it)
            if (rectsReady && IsTouchOnButton(t.position)) continue;

            // Left 40%, bottom 40% → joystick
            if (joystickFingerId < 0 && t.position.x < Screen.width * 0.4f && t.position.y < Screen.height * 0.45f)
            {
                joystickFingerId = t.fingerId;
                joystickCenter = t.position;
                knobOffset = Vector2.zero;
            }
            // Anything else in upper/middle area → camera drag
            else if (cameraFingerId < 0)
            {
                cameraFingerId = t.fingerId;
                cameraPrevPos = t.position;
            }
        }
    }

    private bool IsTouchOnButton(Vector2 screenPos)
    {
        // Convert screen touch (origin bottom-left) to GUI coords (origin top-left)
        var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        return enterBtnRect.Contains(guiPos) || castBtnRect.Contains(guiPos) || spaceBtnRect.Contains(guiPos);
    }

    private static bool TryGetTouch(int fingerId, out Touch touch)
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.fingerId == fingerId)
            {
                touch = t;
                return true;
            }
        }
        touch = default;
        return false;
    }

    private void OnGUI()
    {
        if (!IsMobile) return;
        EnsureStyles();

        var scale = Mathf.Max(1f, Screen.dpi > 0 ? Screen.dpi / 160f : 2f);

        // === VIRTUAL JOYSTICK (bottom-left) ===
        var joySize = 150f * scale;
        var joyX = 24f * scale;
        var joyY = Screen.height - joySize - 24f * scale;
        var joyRect = new Rect(joyX, joyY, joySize, joySize);
        GUI.Box(joyRect, GUIContent.none, joystickBgStyle);

        // Knob
        var knobSize = 56f * scale;
        var knobX = joyRect.center.x + knobOffset.x * (joySize * 0.5f / JoystickRadius) - knobSize * 0.5f;
        var knobY = joyRect.center.y - knobOffset.y * (joySize * 0.5f / JoystickRadius) - knobSize * 0.5f;
        GUI.Box(new Rect(knobX, knobY, knobSize, knobSize), GUIContent.none, joystickKnobStyle);

        // === ACTION BUTTONS (bottom-right) ===
        var btnW = 110f * scale;
        var btnH = 52f * scale;
        var margin = 14f * scale;
        var rightX = Screen.width - btnW - margin;

        // SPACE / TARIK button (bottom)
        spaceBtnRect = new Rect(rightX, Screen.height - btnH - margin, btnW, btnH);
        if (GUI.Button(spaceBtnRect, "TARIK", btnStyle))
            spaceFlag = true;

        // CAST / LEMPAR button (above space)
        castBtnRect = new Rect(rightX, spaceBtnRect.y - btnH - margin, btnW, btnH);
        if (GUI.Button(castBtnRect, "LEMPAR", btnStyle))
            castFlag = true;

        // ENTER / MULAI button (above cast)
        enterBtnRect = new Rect(rightX, castBtnRect.y - btnH - margin, btnW, btnH);
        if (GUI.Button(enterBtnRect, "MULAI", btnStyle))
            enterFlag = true;

        rectsReady = true;
    }

    private void EnsureStyles()
    {
        if (btnStyle != null) return;
        var fs = Mathf.RoundToInt(16f * Mathf.Max(1f, Screen.dpi > 0 ? Screen.dpi / 160f : 2f));

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fs,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white, background = MakeTexture(new Color(0.1f, 0.4f, 0.55f, 0.85f)) },
            hover = { textColor = Color.yellow, background = MakeTexture(new Color(0.15f, 0.5f, 0.65f, 0.85f)) },
            active = { textColor = Color.white, background = MakeTexture(new Color(0.25f, 0.65f, 0.8f, 0.95f)) }
        };

        joystickBgStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.15f, 0.15f, 0.15f, 0.45f)) }
        };

        joystickKnobStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.85f, 0.88f, 0.92f, 0.75f)) }
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
