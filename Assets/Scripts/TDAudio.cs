using UnityEngine;

public class TDAudio : MonoBehaviour
{
    public static TDAudio Instance;

    const string MusicVolKey = "td_music_vol";
    const string SfxVolKey = "td_sfx_vol";

    private AudioSource src;
    private AudioSource boardSrc;
    private AudioSource music;
    private AudioClip[] shots;
    private AudioClip death, leak, build, merge, clear, click;

    public bool MusicOn { get; private set; } = true;

    private float musicVolume = 0.40f;
    private float sfxVolume = 1f;

    /// <summary>Background-music level (0..1). Applied live and persisted.</summary>
    public float MusicVolume
    {
        get { return musicVolume; }
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, musicVolume)) return;
            musicVolume = v;
            if (music != null) music.volume = musicVolume;
            PlayerPrefs.SetFloat(MusicVolKey, musicVolume);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Sound-effect level (0..1) multiplied into every one-shot. Applied live and persisted.</summary>
    public float SfxVolume
    {
        get { return sfxVolume; }
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(v, sfxVolume)) return;
            sfxVolume = v;
            PlayerPrefs.SetFloat(SfxVolKey, sfxVolume);
            PlayerPrefs.Save();
        }
    }

    public static void Ensure()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TDAudio");
        go.AddComponent<TDAudio>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolKey, 0.40f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolKey, 1f));

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        boardSrc = gameObject.AddComponent<AudioSource>();
        boardSrc.playOnAwake = false;
        boardSrc.spatialBlend = 0f;

        // index by TowerType
        shots = new AudioClip[]
        {
            TDSynth.Shot(0), // SingleShot
            TDSynth.Shot(1), // Splash
            TDSynth.Shot(3), // Slow
            TDSynth.Shot(2), // Sniper
            TDSynth.Shot(2), // Chain
            TDSynth.Shot(0), // Pierce
            TDSynth.Shot(3), // Poison (burn)
            TDSynth.Shot(2), // Gold
            // Tier 7 fusion towers (T6+T6)
            TDSynth.Shot(1), // FondueFountain
            TDSynth.Shot(3), // IceCreamTruck
            TDSynth.Shot(0), // BobaBlaster
            TDSynth.Shot(1), // PizzaOven
            TDSynth.Shot(3), // HotSauce
            TDSynth.Shot(3), // CoffeeMug
            TDSynth.Shot(0), // PopTartToaster
            TDSynth.Shot(0), // CookieCrumbler
            TDSynth.Shot(1)  // SourFizz
        };
        death = TDSynth.Death();
        leak = TDSynth.Leak();
        build = TDSynth.Build();
        merge = TDSynth.Merge();
        clear = TDSynth.RoundClear();
        click = TDSynth.Click();

        // looping background music (procedural, seamless)
        GameObject mgo = new GameObject("Music");
        mgo.transform.SetParent(transform, false);
        music = mgo.AddComponent<AudioSource>();
        music.clip = TDSynth.Music();
        music.loop = true;
        music.playOnAwake = false;
        music.spatialBlend = 0f;
        music.volume = musicVolume;
        music.Play();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) ToggleMusic();
    }

    public void ToggleMusic()
    {
        MusicOn = !MusicOn;
        if (music != null) music.mute = !MusicOn;
    }

    public void Play(AudioClip c, float v)
    {
        if (c != null && src != null) src.PlayOneShot(c, v * sfxVolume);
    }

    void PlayBoard(AudioClip clip, float volume, BoardSound sound, byte tower = 0)
    {
        BoardAudioSync.Queue(sound, tower);
        TDGameManager gm = TDGameManager.Instance;
        if ((gm == null || gm.ViewingOwnBoard) && clip != null && boardSrc != null)
            boardSrc.PlayOneShot(clip, volume * sfxVolume);
    }

    /// <summary>Only invoked by the board-audio receiver; never re-queues a sound.</summary>
    public void PlayRemoteBoard(BoardSound sound, byte tower)
    {
        if (boardSrc == null) return;
        AudioClip clip = null;
        float volume = 0f;
        switch (sound)
        {
            case BoardSound.Shot:
                if (tower < shots.Length) { clip = shots[tower]; volume = .45f; }
                break;
            case BoardSound.Death: clip = death; volume = .40f; break;
            case BoardSound.Leak: clip = leak; volume = .7f; break;
            case BoardSound.Build: clip = build; volume = .6f; break;
            case BoardSound.Merge: clip = merge; volume = .7f; break;
            case BoardSound.RoundClear: clip = clear; volume = .8f; break;
        }
        if (clip != null) boardSrc.PlayOneShot(clip, volume * sfxVolume);
    }

    public void StopBoardSounds() { if (boardSrc != null) boardSrc.Stop(); }

    public void Shot(TowerType t)
    {
        int i = (int)t;
        if (shots != null && i >= 0 && i < shots.Length)
            PlayBoard(shots[i], .45f, BoardSound.Shot, (byte)i);
    }

    public void Death() { PlayBoard(death, .40f, BoardSound.Death); }
    public void Leak() { PlayBoard(leak, .7f, BoardSound.Leak); }
    public void Build() { PlayBoard(build, .6f, BoardSound.Build); }
    public void Merge() { PlayBoard(merge, .7f, BoardSound.Merge); }
    public void RoundClear() { PlayBoard(clear, .8f, BoardSound.RoundClear); }
    public void Click() { Play(click, 0.5f); }
}
