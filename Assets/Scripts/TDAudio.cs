using UnityEngine;

public class TDAudio : MonoBehaviour
{
    public static TDAudio Instance;

    private AudioSource src;
    private AudioSource music;
    private AudioClip[] shots;
    private AudioClip death, leak, build, merge, clear, click;

    public bool MusicOn { get; private set; } = true;

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

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        // index by TowerType
        shots = new AudioClip[]
        {
            TDSynth.Shot(0), // SingleShot
            TDSynth.Shot(1), // Splash
            TDSynth.Shot(3), // Slow
            TDSynth.Shot(2), // Sniper
            TDSynth.Shot(2), // Chain
            TDSynth.Shot(0), // Pierce
            TDSynth.Shot(3)  // Poison
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
        music.volume = 0.40f;
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
        if (c != null && src != null) src.PlayOneShot(c, v);
    }

    public void Shot(TowerType t)
    {
        int i = (int)t;
        if (shots != null && i >= 0 && i < shots.Length) Play(shots[i], 0.45f);
    }

    public void Death() { Play(death, 0.40f); }
    public void Leak() { Play(leak, 0.7f); }
    public void Build() { Play(build, 0.6f); }
    public void Merge() { Play(merge, 0.7f); }
    public void RoundClear() { Play(clear, 0.8f); }
    public void Click() { Play(click, 0.5f); }
}
