using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal relay chat (issue #27).
///
/// A client sends its line to the host ("td.chat"); the host prefixes the
/// sender's name, records it locally and fans it out to everyone ("td.chatall").
/// Clients show the echoed line once; the host shows its own line locally and
/// broadcasts it directly. There is no persistence and no
/// long-term storage: each peer keeps the last <see cref="HistorySize"/> lines
/// in memory only.
///
/// Input is owned here (T / Enter opens, Enter sends, Esc cancels) so the game
/// can suppress gameplay keys while <see cref="IsTyping"/> is true. The drawing
/// itself lives in TDGameManager.Multiplayer.cs (called from OnGUI below) so it
/// shares the game's IMGUI styles and screen layout.
/// </summary>
public class ChatSync : MonoBehaviour
{
    public static ChatSync Instance { get; private set; }

    static class Msg
    {
        public const string Chat = "td.chat";       // client -> host
        public const string ChatAll = "td.chatall"; // host -> everyone
    }

    public struct Line
    {
        public string Name;
        public string Text;
        public bool System;
    }

    public const int MaxLength = 120;      // chars per message
    public const int HistorySize = 30;     // local ring buffer
    public const int VisibleLines = 6;     // lines drawn
    const float RateLimit = 0.5f;          // seconds between sent messages

    /// <summary>True while the local player has the chat input open. The game
    /// checks this to ignore gameplay hotkeys and camera panning.</summary>
    public static bool IsTyping { get; private set; }

    public bool Open { get; private set; }
    public string Buffer { get; private set; } = "";

    /// <summary>Newest last; at most <see cref="HistorySize"/> entries.</summary>
    public readonly List<Line> Lines = new List<Line>();

    private bool host;
    private float lastSend = -999f;
    private bool releaseTyping;   // keep IsTyping one extra frame after Esc

    public static ChatSync Ensure()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("ChatSync");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ChatSync>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SetTyping(false);
    }

    /// <summary>Starts a fresh chat session for a new match.</summary>
    public void Begin()
    {
        host = NetworkSession.Instance != null && NetworkSession.Instance.IsHost;
        Open = false;
        Buffer = "";
        lastSend = -999f;
        releaseTyping = false;
        SetTyping(false);
        Lines.Clear();

        NetworkSession.RegisterNamed(Msg.Chat, OnChatMessage);
        NetworkSession.RegisterNamed(Msg.ChatAll, OnChatAllMessage);
    }

    /// <summary>Hard-closes chat (leaving a match, match over).</summary>
    public void Close()
    {
        Open = false;
        Buffer = "";
        releaseTyping = false;
        SetTyping(false);
    }

    // ---------------------------------------------------------------- input
    void Update()
    {
        if (releaseTyping) { releaseTyping = false; SetTyping(false); }

        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || !ns.InSession) return;

        if (Open)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Keep IsTyping true for the rest of this frame so the game's
                // Esc handler (which may run before or after us) ignores it.
                Open = false;
                Buffer = "";
                releaseTyping = true;
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string text = Buffer;
                Close();
                SendLocal(text);
                return;
            }

            // Read raw typed characters rather than an IMGUI TextField, so the
            // input works without focus juggling and the caret is ours.
            string typed = Input.inputString;
            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b')
                {
                    if (Buffer.Length > 0) Buffer = Buffer.Substring(0, Buffer.Length - 1);
                }
                else if (c == '\n' || c == '\r')
                {
                    // handled by Return/KeypadEnter above
                }
                else if (c >= ' ' && Buffer.Length < MaxLength)
                {
                    Buffer += c;
                }
            }
            return;
        }

        // Spectators remain in the match and can keep chatting after dismissing
        // their result. The undismissed end screen and lobby don't accept input.
        TDGameManager gm = TDGameManager.Instance;
        if (gm == null || (gm.State != GameState.Playing && !gm.SpectatingAfterResult) || gm.Paused) return;
        if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Open = true;
            Buffer = "";
            SetTyping(true);
        }
    }

    // ---------------------------------------------------------------- send
    /// <summary>Sends a line (rate-limited). Clients display the host's echo
    /// instead of an optimistic local copy, so each line appears once.</summary>
    public void SendLocal(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
        NetworkSession ns = NetworkSession.Instance;
        if (ns == null || !ns.LinkUp) { SendSystem("Chat unavailable while disconnected."); return; }
        if (Time.unscaledTime - lastSend < RateLimit) return;
        lastSend = Time.unscaledTime;

        string name = LocalName();
        if (host) { AddLine(name, text, false); Broadcast(name, text); }
        else SendToServer(text);
    }

    /// <summary>Adds a local-only notice drawn in the system colour. Hook for
    /// later features (joins, wave alerts, ...).</summary>
    public void SendSystem(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        AddLine("", text, true);
    }

    static void SendToServer(string text)
    {
        using var w = new FastBufferWriter(MaxLength * 2 + 16, Allocator.Temp);
        w.WriteValueSafe(text);
        NetworkSession.SendNamedToServer(Msg.Chat, w);
    }

    static void Broadcast(string name, string text)
    {
        using var w = new FastBufferWriter((name.Length + text.Length) * 2 + 16, Allocator.Temp);
        w.WriteValueSafe(name ?? "");
        w.WriteValueSafe(text ?? "");
        NetworkSession.SendNamedToAll(Msg.ChatAll, w);
    }

    void OnChatMessage(ulong sender, FastBufferReader reader)
    {
        if (!host) return;
        reader.ReadValueSafe(out string text);
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
        string name = NameFor(sender);
        AddLine(name, text, false);
        Broadcast(name, text);
    }

    void OnChatAllMessage(ulong sender, FastBufferReader reader)
    {
        if (host) return;   // the host already showed its own copy
        reader.ReadValueSafe(out string name);
        reader.ReadValueSafe(out string text);
        AddLine(name, text, false);
    }

    // --------------------------------------------------------------- helpers
    void AddLine(string name, string text, bool system)
    {
        Lines.Add(new Line { Name = name ?? "", Text = text ?? "", System = system });
        while (Lines.Count > HistorySize) Lines.RemoveAt(0);
    }

    /// <summary>Flips the typing flag and mutes TDAudio's polling so its 'M'
    /// music hotkey does not fire on a letter typed into chat.</summary>
    static void SetTyping(bool value)
    {
        if (IsTyping == value) return;
        IsTyping = value;
        if (TDAudio.Instance != null) TDAudio.Instance.enabled = !value;
    }

    static string LocalName()
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null)
        {
            ulong id = NetworkSession.LocalClientId;
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == id) return ns.Players[i].Name;
        }
        return "Player";
    }

    static string NameFor(ulong clientId)
    {
        NetworkSession ns = NetworkSession.Instance;
        if (ns != null)
            for (int i = 0; i < ns.Players.Count; i++)
                if (ns.Players[i].ClientId == clientId) return ns.Players[i].Name;
        return "Player";
    }

    // All in-match overlay IMGUI is drawn by the game's partial class (kept in
    // one place); this component just provides the OnGUI entry point.
    void OnGUI()
    {
        TDGameManager gm = TDGameManager.Instance;
        if (gm != null) gm.DrawMpOverlays();
    }
}
