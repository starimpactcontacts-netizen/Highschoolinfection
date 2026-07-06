using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the match HUD procedurally at Play time (no manually-authored Canvas — matches this
/// project's zero-manual-setup convention) and drives it from MatchManager each frame: timer top
/// center, role badge top left, player count top right, hiding status bottom left, and a full
/// result screen overlay at match end.
///
/// Uses legacy UI Text (UnityEngine.UI) rather than TextMeshPro — TMP needs its "Essentials"
/// resources imported once via a dialog before TextMeshProUGUI reliably renders anything, and
/// there's no guarantee that's happened in this project. Legacy Text + Unity's always-available
/// built-in font has no such dependency, which matters for something built with zero manual steps.
/// </summary>
public class GameHUD : MonoBehaviour
{
    private Text timerText, roleText, playerCountText, hidingText, hideSpotCountdownText, hintText;
    private GameObject resultPanel;
    private Text resultTitleText, resultStatsText, resultCountdownText;

    private void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("GameHUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        timerText = CreateText(canvas.transform, "Timer", font, 54, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(400, 70));

        roleText = CreateText(canvas.transform, "RoleBadge", font, 30, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(300, 45));

        playerCountText = CreateText(canvas.transform, "PlayerCount", font, 26, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(260, 40));

        hidingText = CreateText(canvas.transform, "HidingStatus", font, 28, TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 24), new Vector2(220, 40));
        hidingText.color = Color.green;
        hidingText.text = "HIDING";
        hidingText.gameObject.SetActive(false);

        hideSpotCountdownText = CreateText(canvas.transform, "HideSpotCountdown", font, 46, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 60));
        hideSpotCountdownText.color = new Color(1f, 0.85f, 0.3f);
        hideSpotCountdownText.gameObject.SetActive(false);

        // Contextual "what button do I press" prompt — always tells a Human what's available right
        // now (crouch, or a nearby spot to click into) rather than expecting them to already know.
        hintText = CreateText(canvas.transform, "InteractHint", font, 26, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(500, 40));
        hintText.color = new Color(0.9f, 0.9f, 0.9f);
        hintText.gameObject.SetActive(false);

        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(canvas.transform, false);
        var bg = resultPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        var bgRect = resultPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        resultTitleText = CreateText(resultPanel.transform, "ResultTitle", font, 80, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(1000, 120));
        resultStatsText = CreateText(resultPanel.transform, "ResultStats", font, 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(700, 160));
        resultCountdownText = CreateText(resultPanel.transform, "ResultCountdown", font, 22, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(600, 40));

        resultPanel.SetActive(false);
    }

    private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        return text;
    }

    private void Update()
    {
        var match = MatchManager.Instance;
        if (match == null) return;

        switch (match.Phase)
        {
            case MatchPhase.Lobby:
                resultPanel.SetActive(false);
                timerText.text = "";
                roleText.text = "Waiting for players...";
                playerCountText.text = $"{match.Players.Count}/{match.RequiredPlayers} Players";
                hidingText.gameObject.SetActive(false);
                hideSpotCountdownText.gameObject.SetActive(false);
                hintText.gameObject.SetActive(false);
                break;

            case MatchPhase.Countdown:
                timerText.text = $"MATCH STARTING IN {Mathf.CeilToInt(match.CountdownRemaining)}...";
                playerCountText.text = $"{match.Players.Count}/{match.RequiredPlayers} Players";
                break;

            case MatchPhase.Playing:
                timerText.text = FormatTime(match.TimeRemaining);
                playerCountText.text = $"{match.Players.Count}/{match.RequiredPlayers} Players";
                UpdateLocalPlayerStatus();
                break;

            case MatchPhase.Result:
                resultPanel.SetActive(true);
                resultTitleText.text = match.ResultMessage.StartsWith("HUMANS") ? "HUMANS WIN" : "ZOMBIE WINS";
                resultStatsText.text =
                    $"Time survived: {FormatTime(match.TimeSurvivedAtEnd)}\n" +
                    $"Humans turned: {match.HumansConverted}\n" +
                    $"Survivors: {match.HumansAlive}";
                resultCountdownText.text = $"Returning to lobby in {Mathf.CeilToInt(match.LobbyReturnCountdown)}...";
                break;
        }
    }

    private void UpdateLocalPlayerStatus()
    {
        var localPlayer = GameObject.Find("Player");
        if (localPlayer == null) return;

        bool isZombie = localPlayer.GetComponent<ZombieAbility>() != null;
        var human = localPlayer.GetComponent<HumanAbility>();

        roleText.text = isZombie ? "ZOMBIE" : "HUMAN";

        var occupiedSpot = HidingSpot.FindOccupiedBy(localPlayer.transform);
        hideSpotCountdownText.gameObject.SetActive(occupiedSpot != null);
        if (occupiedSpot != null)
            hideSpotCountdownText.text = $"HIDDEN: {Mathf.CeilToInt(occupiedSpot.RemainingHideTime)}s";

        // HumanAbility gets disabled while occupying a HidingSpot (see HidingSpot.Enter) so its own
        // IsHiding doesn't fight the locker's forced state — check the spot first, crouch second.
        bool crouching = human != null && human.IsHiding;
        hidingText.gameObject.SetActive(occupiedSpot == null && crouching);

        // Only Humans need hide prompts — a Zombie has nothing to hide from.
        if (isZombie || human == null)
        {
            hintText.gameObject.SetActive(false);
            return;
        }

        if (occupiedSpot != null)
        {
            hintText.gameObject.SetActive(false);
        }
        else if (HidingSpot.FindNearbyAvailable(localPlayer.transform) != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Press [Left Click] to hide";
        }
        else
        {
            hintText.gameObject.SetActive(true);
            hintText.text = crouching ? "Press [C] to stop hiding" : "Press [C] to hide";
        }
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.Max(Mathf.CeilToInt(seconds), 0);
        return $"{s / 60}:{s % 60:00}";
    }

    // Runs the instant Play starts — this is the only thing the user has to do.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<GameHUD>() != null) return;
        new GameObject("GameHUD").AddComponent<GameHUD>();
    }
}
