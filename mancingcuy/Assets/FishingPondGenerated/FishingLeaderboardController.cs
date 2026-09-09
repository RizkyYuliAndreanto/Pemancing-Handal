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
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
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
        var panel = new Rect(Screen.width - 270f, 14f, 255f, 335f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 12f, 220f, 28f), "GALATAMA", titleStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 44f, 95f, 26f), FormatTime(), titleStyle);
        GUI.Label(new Rect(panel.x + 112f, panel.y + 49f, 125f, 20f),
            ended ? "SESI BERAKHIR" : running ? "SESI BERJALAN" : "TEKAN ENTER UNTUK MULAI", rowStyle);

        var sorted = new List<int> { 0, 1, 2, 3, 4 };
        sorted.Sort((a, b) => totals[b].CompareTo(totals[a]));
        for (var rank = 0; rank < sorted.Count; rank++)
        {
            var index = sorted[rank];
            var y = panel.y + 82f + rank * 32f;
            GUI.Label(new Rect(panel.x + 14f, y, 30f, 24f), $"#{rank + 1}", rowStyle);
            GUI.Label(new Rect(panel.x + 48f, y, 105f, 24f), playerNames[index], rowStyle);
            GUI.Label(new Rect(panel.x + 158f, y, 82f, 24f), $"{totals[index]:0.0} kg", rowStyle);
        }

        GUI.Label(new Rect(panel.x + 14f, panel.y + 255f, 220f, 22f), "Tangkapan Terberat", rowStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 280f, 220f, 26f),
            $"{heaviestPlayer}: {heaviest:0.0} kg", titleStyle);

        if (running)
        {
            var miniGameRect = new Rect(Screen.width * 0.5f - 190f, Screen.height - 155f, 380f, 92f);
            if (playerController != null && playerController.IsReadyToCast)
            {
                var castButton = new Rect(miniGameRect.x + 55f, miniGameRect.y + 18f, miniGameRect.width - 110f, 42f);
                if (GUI.Button(castButton, "LEMPAR PANCING"))
                {
                    Debug.Log($"[FishingUI] Cast button clicked. PlayerController={playerController.name}, Ready={playerController.IsReadyToCast}", this);
                    playerController.HandleCastInput();
                    toast = playerController.IsReadyToCast
                        ? "Cast belum diterima. Pastikan Player memiliki FishingPlayerController."
                        : "Pancing dilempar ke kolam.";
                    toastRemaining = 2f;
                }
            }
            else if (playerBite)
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y, miniGameRect.width, 24f),
                    "IKAN MENYENTAK! TEKAN SPACE SAAT MARKER DI ZONA HIJAU", rowStyle);
                var bar = new Rect(miniGameRect.x + 10f, miniGameRect.y + 34f, miniGameRect.width - 20f, 26f);
                GUI.Box(bar, GUIContent.none, panelStyle);
                var zone = new Rect(bar.x + bar.width * biteZoneStart, bar.y + 3f,
                    bar.width * BiteZoneWidth, bar.height - 6f);
                GUI.Box(zone, GUIContent.none, toastStyle);
                var marker = new Rect(bar.x + bar.width * biteMarker - 4f, bar.y - 2f, 8f, bar.height + 4f);
                GUI.Box(marker, GUIContent.none, markerStyle);
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 63f, miniGameRect.width, 24f),
                    "Tekan tombol SPACE di keyboard sekarang", rowStyle);
            }
            else if (playerController != null && playerController.IsPulling)
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 25f, miniGameRect.width, 28f),
                    "Menarik ikan! Tunggu sebentar...", rowStyle);
            }
            else
            {
                GUI.Label(new Rect(miniGameRect.x, miniGameRect.y + 25f, miniGameRect.width, 28f),
                    "Menunggu ikan... jangan tekan SPACE dulu", rowStyle);
            }
        }
        else if (!ended)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 180f, Screen.height - 78f, 360f, 30f),
                "WASD menuju tepi kolam, lalu tekan ENTER", rowStyle);
        }

        DrawPlayerInventory();
        DrawFishingDebugStatus();

        if (toastRemaining > 0f)
        {
            var toastRect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 90f, 360f, 48f);
            GUI.Label(toastRect, toast, toastStyle);
        }
    }

    private void DrawPlayerInventory()
    {
        if (playerController == null)
            return;

        var inventoryRect = new Rect(14f, 14f, 220f, 92f);
        GUI.Box(inventoryRect, GUIContent.none, panelStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f, inventoryRect.y + 10f, 195f, 22f), "INVENTORY IKAN", titleStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f, inventoryRect.y + 38f, 195f, 20f),
            $"Tangkapan: {playerController.InventoryCount}", rowStyle);
        GUI.Label(new Rect(inventoryRect.x + 12f, inventoryRect.y + 61f, 195f, 20f),
            $"Total berat: {playerController.InventoryTotalWeight:0.0} kg", rowStyle);
    }

    private void DrawFishingDebugStatus()
    {
        if (playerController == null)
            return;

        var status = "FISHING: " + playerController.GetFishingDebugState();
        var debugRect = new Rect(14f, Screen.height - 72f, 300f, 48f);
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
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
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
        rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.75f, 0.9f, 0.92f) } };
        toastStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white, background = MakeTexture(new Color(0.02f, 0.35f, 0.38f, 0.96f)) } };
        markerStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(1f, 0.85f, 0.1f, 1f)) } };
    }

    private static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
