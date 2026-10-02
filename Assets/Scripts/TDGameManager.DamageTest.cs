using UnityEngine;

// Solo "Damage Test" scenario (issue #9): a follow-up to a won run where the
// player's surviving board is pitted against a single invincible Test Dummy.
//
// The world (map + towers) is still alive on the Victory screen, so the test
// simply spawns one dummy at the path start and scores the damage it absorbs
// over TDBalance.DamageTestDuration (ending early if it walks off the end).
// It then drops back to Victory with a fading result banner. The 35-wave run
// is untouched: this only ever runs from a solo Victory, and multiplayer keeps
// its own end screen.
public partial class TDGameManager
{
    private Mob damageTestDummy;          // the one invincible dummy, null when not running
    private float damageTestTimer;        // seconds left in the run
    private float damageTestTotal;        // last read of the dummy's recorded damage
    private string damageTestResult = ""; // final total, shown on the Victory screen
    private float damageTestResultTimer;  // fades the result banner after 10s

    /// <summary>Starts the solo Damage Test: reuses the current board/towers and
    /// spawns ONE invincible Test Dummy at the start of the path.</summary>
    void StartDamageTest()
    {
        if (State != GameState.Victory || mpActive) return;   // solo Victory only
        if (map == null || mobsRoot == null) return;          // world was cleared

        // Enter clean: no pause/time-freeze, modes or pick modal may leak in.
        paused = false;
        settingsOpen = false;
        RestoreTimeScale();
        CancelMode();
        SetSelected(null);
        rogueOpen = false;
        rogueOffered = null;
        Round = RoundState.Preparing;

        // The field is normally empty at Victory; clear it defensively.
        for (int i = Mobs.Count - 1; i >= 0; i--)
            if (Mobs[i] != null) Destroy(Mobs[i].gameObject);
        Mobs.Clear();

        damageTestTotal = 0f;
        damageTestTimer = TDBalance.DamageTestDuration;
        damageTestResult = "";
        damageTestResultTimer = 0f;
        message = "";
        messageTimer = 0f;

        MobDef def = MobCatalog.Get("TestDummy");
        GameObject go = new GameObject("Mob_TestDummy");
        go.transform.SetParent(mobsRoot, false);
        Mob m = go.AddComponent<Mob>();
        m.Init(def, map.Waypoints, this, 1f, 1f);   // no wave scaling: a fixed target
        m.NetId = nextMobId++;
        Mobs.Add(m);
        damageTestDummy = m;

        State = GameState.DamageTest;
    }

    /// <summary>Per-frame Damage Test timers. Called from the main Update so the
    /// result banner also fades while the state is back at Victory.</summary>
    void TickDamageTest()
    {
        if (damageTestResultTimer > 0f)
        {
            damageTestResultTimer -= Time.deltaTime;
            if (damageTestResultTimer <= 0f) damageTestResult = "";
        }

        if (State != GameState.DamageTest) return;

        if (damageTestDummy != null) damageTestTotal = damageTestDummy.DamageTaken;

        damageTestTimer -= Time.deltaTime;
        // End on time, or early once the dummy reaches the path end (ReachEnd
        // destroys it, so the reference goes null).
        if (damageTestTimer <= 0f || damageTestDummy == null) EndDamageTest();
    }

    /// <summary>Ends the test cleanly, removes the dummy and returns to Victory
    /// with a result banner. Best-effort chat relay when in a match.</summary>
    void EndDamageTest()
    {
        if (damageTestDummy != null)
        {
            damageTestTotal = damageTestDummy.DamageTaken;
            Mobs.Remove(damageTestDummy);
            Destroy(damageTestDummy.gameObject);
            damageTestDummy = null;
        }

        damageTestTimer = 0f;
        State = GameState.Victory;

        string total = Mathf.FloorToInt(damageTestTotal).ToString("N0");
        damageTestResult = total;
        damageTestResultTimer = 10f;

        // Best-effort relay. The button is solo-only today, so in a real match
        // this would only fire if the scenario were ever exposed there.
        if (mpActive && ChatSync.Instance != null)
            ChatSync.Instance.SendLocal("Damage Test: " + total);
    }

    /// <summary>Running total and time-left readout drawn at the top while the
    /// test is live, plus an abort button (Esc works too).</summary>
    void DrawDamageTestHud()
    {
        GUI.Label(new Rect(0f, 10f, Screen.width, 34f),
            "Damage Test: " + Mathf.FloorToInt(damageTestTotal).ToString("N0"),
            Style(28, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.45f)));
        GUI.Label(new Rect(0f, 46f, Screen.width, 24f),
            "Time left " + Mathf.CeilToInt(Mathf.Max(0f, damageTestTimer)) + "s   -   invincible dummy",
            Style(16, TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 1f)));
        if (GUI.Button(new Rect((Screen.width - 220f) * 0.5f, Screen.height - 66f, 220f, 40f),
            "End Test (Esc)", PaperButton(18)))
        {
            Click();
            EndDamageTest();
        }
    }

    /// <summary>Final-total banner drawn over the Victory screen; fades out over
    /// the last moment of its 10 second life.</summary>
    void DrawDamageTestResult()
    {
        if (State != GameState.Victory || damageTestResultTimer <= 0f ||
            string.IsNullOrEmpty(damageTestResult)) return;

        float a = Mathf.Clamp01(damageTestResultTimer / 1.5f);
        Color old = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f * a);
        GUI.DrawTexture(new Rect(0f, 76f, Screen.width, 46f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.Label(new Rect(0f, 82f, Screen.width, 34f),
            "Damage Test: " + damageTestResult,
            Style(26, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.45f)));
        GUI.color = old;
    }
}
