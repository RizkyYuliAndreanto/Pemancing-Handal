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

    // Joystick state
    private int joystickFingerId = -1;
    private Vector2 joystickCenter;
    private const float JoystickRadius = 80f;
    private const float JoystickDeadZone = 12f;

    // Button state (one-frame flags)
    private bool enterThisFrame;
    private bool spaceThisFrame;
    private bool castThisFrame;

    // Styles
    private GUIStyle btnStyle;
    private GUIStyle joystickBgStyle;
    private GUIStyle joystickKnobStyle;
    private Vector2 knobOffset;

    private void Start()
    {
        // Detect mobile: touch-capable device (WebGL on phone/tablet)
        IsMobile = Application.isMobilePlatform ||
            (Application.platform == RuntimePlatform.WebGLPlayer && IsTouchDevice());
    }

    private static bool IsTouchDevice()
    {
        // In WebGL, Input.touchSupported works on most browsers
        return Input.touchSupported && !Input.mousePresent ||
               Input.touchSupported && Screen.width < 1200;
    }

    private void Update()
    {
        // Re-detect each frame in case of resize / orientation change
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            IsMobile = Input.touchSupported && Screen.width < 1200;

        // Copy one-frame flags then clear
        EnterPressed = enterThisFrame;
        SpacePressed = spaceThisFrame;
        CastPressed = castThisFrame;
        enterThisFrame = false;
        spaceThisFrame = false;
        castThisFrame = false;

        if (!IsMobile)
        {
            JoystickInput = Vector2.zero;
            return;
        }

        ProcessTouchJoystick();
    }

    private void ProcessTouchJoystick()
    {
        // Check if our joystick finger is still active
        if (joystickFingerId >= 0)
        {
            var found = false;
            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId != joystickFingerId) continue;
                found = true;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
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
                break;
            }
            if (!found)
            {
                joystickFingerId = -1;
                JoystickInput = Vector2.zero;
                knobOffset = Vector2.zero;
            }
        }

        // Pick up new joystick touch (left half of screen, bottom area)
        if (joystickFingerId < 0)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                // Left 40% of screen, bottom 40%
                if (t.position.x < Screen.width * 0.4f && t.position.y < Screen.height * 0.4f)
                {
                    joystickFingerId = t.fingerId;
                    joystickCenter = t.position;
                    knobOffset = Vector2.zero;
                    break;
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!IsMobile) return;
        EnsureStyles();

        var scale = Mathf.Max(1f, Screen.dpi / 160f);
        // ponytail: simple scale factor, upgrade to full DPI-aware layout if needed

        // === VIRTUAL JOYSTICK (bottom-left) ===
        var joySize = 160f * scale;
        var joyX = 30f * scale;
        var joyY = Screen.height - joySize - 30f * scale;
        var joyRect = new Rect(joyX, joyY, joySize, joySize);
        GUI.Box(joyRect, GUIContent.none, joystickBgStyle);

        // Knob
        var knobSize = 60f * scale;
        // knobOffset is in screen coords (y-up), GUI is y-down
        var knobX = joyRect.center.x + knobOffset.x * scale - knobSize * 0.5f;
        var knobY = joyRect.center.y - knobOffset.y * scale - knobSize * 0.5f;
        GUI.Box(new Rect(knobX, knobY, knobSize, knobSize), GUIContent.none, joystickKnobStyle);

        // === ACTION BUTTONS (bottom-right) ===
        var btnW = 120f * scale;
        var btnH = 55f * scale;
        var margin = 16f * scale;
        var rightX = Screen.width - btnW - margin;

        // SPACE button (bottom)
        var spaceRect = new Rect(rightX, Screen.height - btnH - margin, btnW, btnH);
        if (GUI.Button(spaceRect, "TARIK\n(SPACE)", btnStyle))
            spaceThisFrame = true;

        // CAST button (above space)
        var castRect = new Rect(rightX, spaceRect.y - btnH - margin, btnW, btnH);
        if (GUI.Button(castRect, "LEMPAR\nPANCING", btnStyle))
            castThisFrame = true;

        // ENTER / MULAI button (above cast)
        var enterRect = new Rect(rightX, castRect.y - btnH - margin, btnW, btnH);
        if (GUI.Button(enterRect, "MULAI\n(ENTER)", btnStyle))
            enterThisFrame = true;
    }

    private void EnsureStyles()
    {
        if (btnStyle != null) return;

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(14f * Mathf.Max(1f, Screen.dpi / 160f)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white, background = MakeTexture(new Color(0.1f, 0.4f, 0.55f, 0.9f)) },
            hover = { textColor = Color.yellow, background = MakeTexture(new Color(0.15f, 0.5f, 0.65f, 0.9f)) },
            active = { textColor = Color.white, background = MakeTexture(new Color(0.2f, 0.6f, 0.75f, 0.95f)) }
        };

        joystickBgStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.2f, 0.2f, 0.2f, 0.4f)) }
        };

        joystickKnobStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0.8f, 0.85f, 0.9f, 0.7f)) }
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
