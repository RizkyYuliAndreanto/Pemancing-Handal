using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingLeaderboardController : MonoBehaviour
{
    private const float SessionLength = 60f;
    private readonly string[] playerNames = { "Player", "Bot_A", "Bot_B", "Bot_C", "Bot_D" };
    private readonly float[] totals = new float[5];
    private readonly int[] catchCounts = new int[5];
    private float remaining = SessionLength;
    private float nextCatch = 3f;
    private float toastRemaining;
    private string toast = string.Empty;
    private float heaviest;
    private string heaviestPlayer = "-";
    private bool running;
    private bool ended;
    private bool showHelp;
    private FishingPlayerController playerController;
    private float playerBiteTimer;
    private bool playerBite;
    private float playerBiteWindow;
    private float biteMarker;
    private float biteZoneStart;
    private const float BiteZoneWidth = 0.2f;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle rowStyle;
    private GUIStyle toastStyle;
    private GUIStyle markerStyle;
    private GUIStyle instrStyle;
    private GUIStyle helpBtnStyle;
    private GUIStyle helpBoxStyle;

    private void Start()
    {
        ResetSession();
        playerController = GameObject.Find("Player")?.GetComponent<FishingPlayerController>();
    }

    private bool IsPlayerAtPond()
    {
        if (playerController == null)
            playerController = GameObject.Find("Player")?.GetComponent<FishingPlayerController>();
        if (playerController == null)
            return false;

        var p = playerController.transform.position;
        // Near any edge of the pond (barriers at x=±10.2, z=±7.2)
        // Player must be within 4m outside the barrier on at least one axis
        var nearEastWest = Mathf.Abs(p.x) >= 10f && Mathf.Abs(p.x) <= 15f && Mathf.Abs(p.z) <= 10f;
        var nearNorthSouth = Mathf.Abs(p.z) >= 7f && Mathf.Abs(p.z) <= 12f && Mathf.Abs(p.x) <= 13f;
        return nearEastWest || nearNorthSouth;
    }

    private void Update()
    {
        if (!running)
        {
            var enterPressed = (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                || MobileTouchControls.EnterPressed;
            if (enterPressed)
            {
                if (IsPlayerAtPond())
                    StartSession();
                else
                {
                    toast = "Dekati tepi kolam dulu untuk memulai sesi.";
                    toastRemaining = 2f;
                }
            }
            return;
        }

        remaining -= Time.deltaTime;
        nextCatch -= Time.deltaTime;
        toastRemaining -= Time.deltaTime;
        if (nextCatch <= 0f)
        {
            SimulateCatch();
            nextCatch = UnityEngine.Random.Range(3f, 6f);
        }
        UpdatePlayerMinigame();
        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            ended = true;
            playerBite = false;
            if (playerController != null)
                playerController.EndFishingSession();
            EndBotSessions();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();
        var mobile = MobileTouchControls.IsMobile;
        var s = mobile ? Mathf.Max(1f, Screen.width / 800f) : 1f; // UI scale factor

        // === TOAST / NOTIF at TOP CENTER ===
        if (toastRemaining > 0f)
        {
            var tw = 400f * s;
            var toastRect = new Rect(Screen.width * 0.5f - tw * 0.5f, 16f * s, tw, 48f * s);
            GUI.Label(toastRect, toast, toastStyle);
        }

        // === LEADERBOARD PANEL (right side) ===
        var pw = 255f * s;
        var ph = 335f * s;
        var panel = new Rect(Screen.width - pw - 10f, 72f * s, pw, ph);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 14f * s, panel.y + 12f * s, 220f * s, 28f * s), "GALATAMA", titleStyle);
        GUI.Label(new Rect(panel.x + 14f * s, panel.y + 44f * s, 95f * s, 26f * s), FormatTime(), titleStyle);
        GUI.Label(new Rect(panel.x + 100f * s, panel.y + 49f * s, 140f * s, 20f * s),
            ended ? "SESI BERAKHIR" : running ? "SESI BERJALAN" : "TEKAN ENTER", instrStyle);

        var sorted = new List<int> { 0, 1, 2, 3, 4 };
        sorted.Sort((a, b) => totals[b].CompareTo(totals[a]));
        for (var rank = 0; rank < sorted.Count; rank++)
        {
            var index = sorted[rank];
            var y = panel.y + 82f * s + rank * 32f * s;
            GUI.Label(new Rect(panel.x + 14f * s, y, 30f * s, 24f * s), $"#{rank + 1}", rowStyle);
            GUI.Label(new Rect(panel.x + 48f * s, y, 105f * s, 24f * s), playerNames[index], rowStyle);
            GUI.Label(new Rect(panel.x + 158f * s, y, 82f * s, 24f * s), $"{totals[index]:0.0} kg", rowStyle);
        }

        GUI.Label(new Rect(panel.x + 14f * s, panel.y + 255f * s, 220f * s, 22f * s), "Tangkapan Terberat", rowStyle);
        GUI.Label(new Rect(panel.x + 14f * s, panel.y + 280f * s, 220f * s, 26f * s),
            $"{heaviestPlayer}: {heaviest:0.0} kg", titleStyle);

        // === HELP BUTTON (top-right, above leaderboard) ===
        if (!running && !ended)
        {
            if (GUI.Button(new Rect(Screen.width - 120f * s, 16f * s, 105f * s, 40f * s), "? BANTUAN", helpBtnStyle))
                showHelp = !showHelp;

            if (showHelp)
            {
                var helpRect = new Rect(Screen.width - 370f * s, 62f * s, 355f * s, 260f * s);
                GUI.Box(helpRect, GUIContent.none, helpBoxStyle);
                GUI.Label(new Rect(helpRect.x + 14f * s, helpRect.y + 10f * s, 325f * s, 26f * s), "CARA BERMAIN", titleStyle);
                var helpText = mobile
                    ? "1. Gerak dengan JOYSTICK (kiri bawah)\n" +
                      "2. Dekati tepi kolam\n" +
                      "3. Tekan tombol MULAI (kanan)\n" +
                      "4. Tekan LEMPAR PANCING\n" +
                      "5. Tunggu ikan menyentak\n" +
                      "6. Tekan TARIK saat marker di zona hijau\n" +
                      "7. Setelah ikan tertarik, tekan LEMPAR lagi\n" +
                      "8. Kumpulkan ikan terbanyak dalam 60 detik!"
                    : "1. Gerak dengan WASD / Arrow Keys\n" +
                      "2. Dekati tepi kolam\n" +
                      "3. Tekan ENTER untuk mulai sesi\n" +
                      "4. Klik LEMPAR PANCING / tekan ENTER\n" +
                      "5. Tunggu ikan menyentak\n" +
                      "6. Tekan SPACE saat marker di zona hijau\n" +
                      "7. Setelah ikan tertarik, klik lagi untuk\n" +
                      "   melempar ulang\n" +
                      "8. Kumpulkan ikan terbanyak dalam 60 detik!";
                GUI.Label(new Rect(helpRect.x + 14f * s, helpRect.y + 38f * s, 325f * s, 215f * s), helpText, instrStyle);
            }
        }

        // === MINIGAME / INSTRUCTION AREA (bottom center) ===
        if (running)
        {
            var mgW = 380f * s;
            var mgH = 92f * s;
            var miniGameRect = new Rect(Screen.width * 0.5f - mgW * 0.5f, Screen.height - mgH - 70f * s, mgW, mgH);
            if (playerController != null && playerController.IsReadyToCast)
            {
                var castButton = new Rect(miniGameRect.x + 55f * s, miniGameRect.y + 18f * s, miniGameRect.width - 110f * s, 42f * s);
                if (GUI.Button(castButton, "LEMPAR PANCING") || MobileTouchControls.CastPressed)
                {
                    playerController.HandleCastInput();
                    toast = playerController.IsReadyToCast
                        ? "Cast belum diterima."
                        : "Pancing dilempar ke kolam.";
                    toastRemaining = 2f;
                }
            }
            else if (playerBite)
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y, miniGameRect.width, 28f * s),
                    "IKAN MENYENTAK! TEKAN SPACE DI ZONA HIJAU", instrStyle);
                var bar = new Rect(miniGameRect.x + 10f * s, miniGameRect.y + 34f * s, miniGameRect.width - 20f * s, 26f * s);
                GUI.Box(bar, GUIContent.none, panelStyle);
                var zone = new Rect(bar.x + bar.width * biteZoneStart, bar.y + 3f * s,
                    bar.width * BiteZoneWidth, bar.height - 6f * s);
                GUI.Box(zone, GUIContent.none, toastStyle);
                var marker = new Rect(bar.x + bar.width * biteMarker - 4f * s, bar.y - 2f * s, 8f * s, bar.height + 4f * s);
                GUI.Box(marker, GUIContent.none, markerStyle);
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 63f * s, miniGameRect.width, 28f * s),
                    mobile ? "TEKAN TARIK SEKARANG!" : "TEKAN SPACE SEKARANG!", instrStyle);
            }
            else if (playerController != null && playerController.IsPulling)
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 25f * s, miniGameRect.width, 28f * s),
                    "MENARIK IKAN! TUNGGU SEBENTAR...", instrStyle);
            }
            else
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 25f * s, miniGameRect.width, 28f * s),
                    mobile ? "MENUNGGU IKAN..." : "MENUNGGU IKAN... JANGAN TEKAN SPACE DULU", instrStyle);
            }
        }
        else if (!ended)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 220f * s, Screen.height - 78f * s, 440f * s, 34f * s),
                mobile ? "JOYSTICK MENUJU KOLAM, LALU TEKAN MULAI" : "WASD MENUJU TEPI KOLAM, LALU TEKAN ENTER", instrStyle);
        }

        DrawPlayerInventory(s);
        DrawFishingDebugStatus(s);
    }

    private void DrawPlayerInventory(float s = 1f)
    {
        if (playerController == null)
            return;

        var inventoryRect = new Rect(14f * s, 14f * s, 220f * s, 92f * s);
        GUI.Box(inventoryRect, GUIContent.none, panelStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f * s, inventoryRect.y + 10f * s, 195f * s, 22f * s), "INVENTORY IKAN", titleStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f * s, inventoryRect.y + 38f * s, 195f * s, 20f * s),
            $"Tangkapan: {playerController.InventoryCount}", rowStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f * s, inventoryRect.y + 61f * s, 195f * s, 20f * s),
            $"Total berat: {playerController.InventoryTotalWeight:0.0} kg", rowStyle);
    }

    private void DrawFishingDebugStatus(float s = 1f)
    {
        if (playerController == null)
            return;

        var status = "FISHING: " + playerController.GetFishingDebugState();
        var debugRect = new Rect(14f * s, Screen.height - 72f * s, 300f * s, 48f * s);
        GUI.Label(debugRect, status, rowStyle);
    }

    private void SimulateCatch()
    {
        var readyBots = new List<int>();
        for (var i = 1; i < playerNames.Length; i++)
        {
            var candidateBot = GameObject.Find(playerNames[i])?.GetComponent<FishingBot>();
            if (candidateBot != null && candidateBot.IsReadyToFish())
                readyBots.Add(i);
        }
        if (readyBots.Count == 0)
            return;

        var index = readyBots[UnityEngine.Random.Range(0, readyBots.Count)];
        var weight = Mathf.Round(UnityEngine.Random.Range(0.5f, 10f) * 10f) / 10f;
        RecordCatch(index, weight);
        var catchingBot = GameObject.Find(playerNames[index])?.GetComponent<FishingBot>();
        if (catchingBot != null)
            catchingBot.TriggerCatch(weight);
    }

    private void RecordCatch(int index, float weight)
    {
        totals[index] += weight;
        catchCounts[index]++;
        if (weight > heaviest)
        {
            heaviest = weight;
            heaviestPlayer = playerNames[index];
        }
        toast = $"{playerNames[index]} dapat ikan {weight:0.0} kg!";
        toastRemaining = 2f;
    }

    private string FormatTime()
    {
        return $"{Mathf.FloorToInt(remaining / 60f):00}:{Mathf.FloorToInt(remaining % 60f):00}";
    }

    private void StartSession()
    {
        if (ended)
            ResetSession();
        running = true;
        ended = false;
        playerBiteTimer = UnityEngine.Random.Range(3f, 6f);
        playerBite = false;
        if (playerController != null)
            playerController.BeginFishingSession();
        BeginBotSessions();
    }

    private void ResetSession()
    {
        for (var i = 0; i < totals.Length; i++)
        {
            totals[i] = 0f;
            catchCounts[i] = 0;
        }
        remaining = SessionLength;
        nextCatch = UnityEngine.Random.Range(3f, 6f);
        heaviest = 0f;
        heaviestPlayer = "-";
        toast = string.Empty;
        toastRemaining = 0f;
        running = false;
        ended = false;
        if (playerController != null)
            playerController.EndFishingSession();
        EndBotSessions();
        playerBite = false;
        playerBiteTimer = 0f;
    }

    private void UpdatePlayerMinigame()
    {
        if (playerController == null)
            return;

        // Don't start/continue bite minigame unless line is in water waiting
        if (!playerController.HasCastLine)
        {
            playerBite = false;
            return;
        }

        // Don't process bites while pull animation is playing
        if (playerController.IsPulling)
        {
            playerBite = false;
            return;
        }

        playerBiteTimer -= Time.deltaTime;
        if (!playerBite && playerBiteTimer <= 0f)
        {
            playerBite = true;
            playerBiteWindow = 1.3f;
            biteMarker = 0f;
            biteZoneStart = UnityEngine.Random.Range(0.25f, 0.7f);
        }
        if (!playerBite)
            return;

        playerBiteWindow -= Time.deltaTime;
        biteMarker = Mathf.PingPong((1.3f - playerBiteWindow) * 0.72f, 1f);
        var spacePressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || MobileTouchControls.SpacePressed;
        if (spacePressed)
        {
            if (playerBiteWindow > 0f && biteMarker >= biteZoneStart && biteMarker <= biteZoneStart + BiteZoneWidth)
            {
                // Minigame success — trigger pull animation
                var weight = Mathf.Round(UnityEngine.Random.Range(0.5f, 10f) * 10f) / 10f;
                RecordCatch(0, weight);
                playerController.TriggerCatch(weight);
                toast = $"Player berhasil menarik ikan {weight:0.0} kg!";
                toastRemaining = 2f;
                playerBite = false;
                playerBiteTimer = UnityEngine.Random.Range(3f, 6f);
            }
            else
            {
                // Minigame fail — fish escapes, line stays in water, wait for next bite
                toast = "Ikan lepas! Timing kurang tepat.";
                toastRemaining = 2f;
                playerBite = false;
                playerBiteTimer = UnityEngine.Random.Range(3f, 6f);
            }
        }
        else if (playerBiteWindow <= 0f)
        {
            // Timeout — fish escapes, line stays in water
            toast = "Ikan lepas! Terlalu lambat menekan SPACE.";
            toastRemaining = 2f;
            playerBite = false;
            playerBiteTimer = UnityEngine.Random.Range(3f, 6f);
        }
    }

    private void BeginBotSessions()
    {
        // ponytail: spots on east/west pond edges, clear of obstacles
        // Barriers at x=±10.2, fishing stations at (±12.5,±8.5), rocks at (±11.5,±5)
        // These sit just outside the barrier on east/west side at z offsets with no obstacles
        var spots = new[]
        {
            new Vector3(-11.0f, 0.15f, -3.0f), new Vector3(11.0f, 0.15f, -3.0f),
            new Vector3(-11.0f, 0.15f, 3.0f), new Vector3(11.0f, 0.15f, 3.0f)
        };
        for (var i = 0; i < spots.Length; i++)
        {
            var bot = GameObject.Find(playerNames[i + 1])?.GetComponent<FishingBot>();
            if (bot != null)
                bot.BeginFishingSession(spots[i]);
        }
    }

    private void EndBotSessions()
    {
        for (var i = 1; i < playerNames.Length; i++)
        {
            var bot = GameObject.Find(playerNames[i])?.GetComponent<FishingBot>();
            if (bot != null)
                bot.EndFishingSession();
        }
    }

    private static bool IsEnterPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame;
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
            // The project may be configured for the new input backend only.
        }

        return inputSystemClick || legacyClick;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;
        panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.03f, 0.12f, 0.17f, 0.94f)) } };
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.75f, 0.9f, 0.92f) } };
        instrStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 0.7f) }, wordWrap = true };
        toastStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white, background = MakeTexture(new Color(0.02f, 0.35f, 0.38f, 0.96f)) } };
        markerStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(1f, 0.85f, 0.1f, 1f)) } };
        helpBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = MakeTexture(new Color(0.1f, 0.45f, 0.55f, 0.95f)) }, hover = { textColor = Color.yellow, background = MakeTexture(new Color(0.15f, 0.55f, 0.65f, 0.95f)) } };
        helpBoxStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.02f, 0.08f, 0.14f, 0.96f)) } };
    }

    private static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
