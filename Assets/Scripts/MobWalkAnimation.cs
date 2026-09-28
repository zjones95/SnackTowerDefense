using UnityEngine;

/// <summary>
/// Plays a mob model's baked walk clip (glTFast Legacy <see cref="Animation"/>) at a rate that
/// follows how fast the mob is actually moving. A slowed mob walks slower and a stunned mob holds
/// its pose, because the rate is measured from the mob's real movement rather than its stat speed.
/// Models with no clip (older or procedural art) are left alone.
/// </summary>
[DisallowMultipleComponent]
public class MobWalkAnimation : MonoBehaviour
{
    /// <summary>Gait cycles per world unit travelled (one cycle is two steps).</summary>
    public const float CyclesPerUnit = 1.2f;
    /// <summary>Cap so a dashing or enraged mob doesn't blur its legs.</summary>
    public const float MaxCyclesPerSecond = 4f;

    private Animation anim;
    private AnimationState state;
    private Transform mover;
    private Vector3 last;
    private float smoothed;

    /// <summary>Wires up the walk clip on an instantiated mob model, if it has one.</summary>
    public static void Attach(GameObject model)
    {
        if (model == null) return;
        Animation a = model.GetComponentInChildren<Animation>();
        if (a == null) return;

        AnimationState st = null;
        foreach (AnimationState s in a) { st = s; break; }
        if (st == null) return;

        a.playAutomatically = false;
        a.cullingType = AnimationCullingType.AlwaysAnimate;
        st.wrapMode = WrapMode.Loop;
        st.speed = 0f;
        // Desync a group so a wave doesn't march in lockstep.
        st.normalizedTime = Random.value;
        a.Play(st.name);

        MobWalkAnimation w = model.AddComponent<MobWalkAnimation>();
        w.anim = a;
        w.state = st;
        w.mover = model.transform.parent != null ? model.transform.parent : model.transform;
        w.last = w.mover.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (state == null || dt <= 1e-5f) return;

        Vector3 p = mover.position;
        Vector3 d = new Vector3(p.x - last.x, 0f, p.z - last.z);
        last = p;

        float speed = d.magnitude / dt;
        smoothed = Mathf.Lerp(smoothed, speed, 1f - Mathf.Exp(-12f * dt));
        state.speed = Mathf.Min(smoothed * CyclesPerUnit, MaxCyclesPerSecond);
    }
}
