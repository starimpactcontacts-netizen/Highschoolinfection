using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerRole { Human, Zombie }
public enum MatchPhase { Lobby, Countdown, Playing, Result }

/// <summary>
/// The match rules engine: lobby -> countdown -> playing -> result -> back to lobby. Role
/// assignment (first registered player = Zombie, the rest = Human), the 5-minute timer, zombie
/// detection, and win conditions all live here. Touched Humans are converted to Zombies rather than
/// removed from the match — the Zombie side grows as the match goes on, until either the timer runs
/// out (Humans win, survivors counted) or every Human has been converted (Zombies win).
///
/// This is NOT networked. Photon PUN2 isn't in this project — that requires an actual Photon
/// account + App ID (only creatable by the project owner) and importing the PUN2 package via
/// Unity's Package Manager/Asset Store, neither of which is something achievable from here. This
/// runs the complete rules engine for a single local session so it's fully testable today; wiring
/// real networking later means adding PhotonView/RPCs around RegisterPlayer/ConvertToZombie/the
/// timer sync, not rebuilding the rules.
///
/// Since there's only ever one real (keyboard-controlled) player in a local session, and that
/// player is always registered first (always Zombie) with zero Humans that would make every
/// match instantly resolve as "zombie wins" with nothing to test, <see cref="DummyHuman"/> stand-ins
/// fill the Human roster locally so detection/conversion/timer logic is actually exercisable.
/// Remove/replace those once Photon spawns real networked Humans.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Match Settings")]
    [Tooltip("Real match size is 11. Left low so a solo local session actually fills the lobby and starts.")]
    [SerializeField] private int requiredPlayers = 6;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float matchDurationSeconds = 300f; // 5 minutes
    [SerializeField] private float lobbyReturnDelay = 5f;

    [Header("Detection")]
    [SerializeField] private float zombieDetectionRadius = 15f;
    // Spec said hiding needs "50m+ radius" to be detected, which — read literally — is a LARGER
    // radius than the normal 15m, i.e. easier to detect, contradicting "harder to detect" in the
    // same sentence. Implemented as the sensible opposite: hiding shrinks the effective detection
    // radius way down. Flagged here and in the delivery report rather than guessed silently.
    [SerializeField] private float hidingDetectionRadius = 3f;
    [SerializeField] private float touchConversionDistance = 1.2f;

    public MatchPhase Phase { get; private set; } = MatchPhase.Lobby;
    public int RequiredPlayers => requiredPlayers;
    public float TimeRemaining { get; private set; }
    public float CountdownRemaining { get; private set; }
    public float LobbyReturnCountdown { get; private set; }
    public string ResultMessage { get; private set; }
    public float TimeSurvivedAtEnd { get; private set; }

    public class PlayerRecord
    {
        public Transform Transform;
        public PlayerRole Role;
        public bool Detected;
    }

    private readonly List<PlayerRecord> players = new List<PlayerRecord>();
    public IReadOnlyList<PlayerRecord> Players => players;

    public int HumansAlive => players.Count(p => p.Role == PlayerRole.Human);
    public int HumansConverted { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Registers a player (real or dummy) and returns the role they were assigned. The role isn't
    /// known until this returns, so the caller attaches ZombieAbility/HumanAbility afterward — this
    /// looks HumanAbility up lazily via GetComponent in Update() rather than needing it passed in.
    /// </summary>
    public PlayerRole RegisterPlayer(Transform playerTransform)
    {
        PlayerRole role = players.Count == 0 ? PlayerRole.Zombie : PlayerRole.Human;
        players.Add(new PlayerRecord { Transform = playerTransform, Role = role });

        if (Phase == MatchPhase.Lobby && players.Count >= requiredPlayers)
            StartCoroutine(BeginCountdown());

        return role;
    }

    private IEnumerator BeginCountdown()
    {
        Phase = MatchPhase.Countdown;
        CountdownRemaining = countdownSeconds;
        while (CountdownRemaining > 0f)
        {
            yield return null;
            CountdownRemaining -= Time.deltaTime;
        }
        Phase = MatchPhase.Playing;
        TimeRemaining = matchDurationSeconds;
    }

    private void Update()
    {
        if (Phase != MatchPhase.Playing) return;

        TimeRemaining -= Time.deltaTime;

        // Snapshot this frame's zombies before any conversions happen, so a human infected this
        // frame doesn't also immediately hunt within the same tick — they start hunting next frame.
        var zombies = players.Where(p => p.Role == PlayerRole.Zombie && p.Transform != null).ToList();
        var toConvert = new List<PlayerRecord>();

        if (zombies.Count > 0)
        {
            foreach (var human in players)
            {
                if (human.Role != PlayerRole.Human || human.Transform == null) continue;

                bool hiding = human.Transform.TryGetComponent<HumanAbility>(out var ability) && ability.IsHiding;
                float radius = hiding ? hidingDetectionRadius : zombieDetectionRadius;

                float nearestDist = float.MaxValue;
                foreach (var zombie in zombies)
                {
                    float dist = Vector3.Distance(zombie.Transform.position, human.Transform.position);
                    if (dist < nearestDist) nearestDist = dist;
                    if (dist <= touchConversionDistance) { toConvert.Add(human); break; }
                }
                human.Detected = nearestDist <= radius;
            }
        }

        foreach (var human in toConvert)
            ConvertToZombie(human);

        if (TimeRemaining <= 0f) EndMatch();
        else if (HumansAlive == 0) EndMatch();
    }

    /// <summary>
    /// Touched Humans don't get removed from the match — they join the Zombie side, same as the
    /// brief describes ("they become zombie"). The match keeps going with more Zombies hunting
    /// fewer Humans until either the timer runs out or every Human has been converted.
    /// </summary>
    private void ConvertToZombie(PlayerRecord human)
    {
        if (human.Role == PlayerRole.Zombie) return;
        human.Role = PlayerRole.Zombie;
        HumansConverted++;

        var go = human.Transform.gameObject;

        // If they were mid-hide (frozen/invisible), force them out first so conversion doesn't
        // leave a zombie stuck in a locker's hidden state.
        HidingSpot.ForceEjectIfOccupying(human.Transform);

        var humanAbility = go.GetComponent<HumanAbility>();
        if (humanAbility != null) Destroy(humanAbility);

        // Dummies wander/auto-hide on their own via DummyHuman — that's Human behavior, so it has
        // to stop once they're a Zombie, or its HideCycle coroutine would keep calling SetHiding on
        // the HumanAbility this just destroyed.
        var dummy = go.GetComponent<DummyHuman>();
        if (dummy != null)
        {
            dummy.StopAllCoroutines();
            dummy.enabled = false;
        }

        if (go.GetComponent<ZombieAbility>() == null) go.AddComponent<ZombieAbility>();
    }

    private void EndMatch()
    {
        Phase = MatchPhase.Result;
        TimeSurvivedAtEnd = matchDurationSeconds - Mathf.Max(TimeRemaining, 0f);
        ResultMessage = HumansAlive > 0
            ? $"HUMANS WIN - SURVIVORS: {HumansAlive}"
            : "ZOMBIE WINS - ALL EATEN";
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        LobbyReturnCountdown = lobbyReturnDelay;
        while (LobbyReturnCountdown > 0f)
        {
            yield return null;
            LobbyReturnCountdown -= Time.deltaTime;
        }

        players.Clear();
        HumansConverted = 0;
        Phase = MatchPhase.Lobby;
    }

    // Runs the instant Play starts — this is the only thing the user has to do.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Object.FindFirstObjectByType<MatchManager>() != null) return;
        new GameObject("MatchManager").AddComponent<MatchManager>();
    }
}
