using UnityEngine;

// A small procedural child character with a speed-driven walk cycle.
public class ChildModel : MonoBehaviour
{
    public const float UnitHeight = 1.5f; // height at scale 1

    private Transform container, torso, head;
    private Transform shoulderL, shoulderR, elbowL, elbowR;
    private Transform hipL, hipR, kneeL, kneeR;

    private bool built;
    private Vector3 lastPos;
    private float speed;
    private float phase;

    public void Build(Color shirt, Color pants, Color skin, Color hair, float scale, bool longHair = false)
    {
        if (built) return;

        Material mShirt = TDVisuals.Mat(shirt, 0f, 0.35f);
        Material mPants = TDVisuals.Mat(pants, 0f, 0.35f);
        Material mSkin = TDVisuals.Mat(skin, 0f, 0.4f);
        Material mHair = TDVisuals.Mat(hair, 0f, 0.3f);
        Material mShoe = TDVisuals.Mat(new Color(0.14f, 0.14f, 0.17f), 0f, 0.3f);

        container = new GameObject("Body").transform;
        container.SetParent(transform, false);
        container.localScale = Vector3.one * scale;

        // child proportions: big head, short limbs
        TDVisuals.Box(container, "Pelvis", new Vector3(0f, 0.62f, 0f), new Vector3(0.34f, 0.18f, 0.22f), mPants);
        torso = TDVisuals.Box(container, "Torso", new Vector3(0f, 0.92f, 0f), new Vector3(0.42f, 0.46f, 0.26f), mShirt).transform;
        head = TDVisuals.Sphere(container, "Head", new Vector3(0f, 1.32f, 0f), 0.42f, mSkin).transform;
        TDVisuals.Sphere(container, "Hair", new Vector3(0f, 1.37f, -0.02f), 0.45f, mHair);
        if (longHair)
        {
            // long hair down the back plus side strands and a messy bun
            TDVisuals.Box(container, "HairBack", new Vector3(0f, 1.00f, -0.17f), new Vector3(0.40f, 0.86f, 0.18f), mHair);
            TDVisuals.Box(container, "HairSideL", new Vector3(-0.19f, 1.14f, -0.02f), new Vector3(0.10f, 0.62f, 0.24f), mHair);
            TDVisuals.Box(container, "HairSideR", new Vector3(0.19f, 1.14f, -0.02f), new Vector3(0.10f, 0.62f, 0.24f), mHair);
            TDVisuals.Sphere(container, "Bun", new Vector3(0.05f, 1.62f, -0.12f), 0.26f, mHair);
        }
        TDVisuals.Sphere(container, "EyeL", new Vector3(-0.09f, 1.34f, 0.19f), 0.06f, TDVisuals.Mat(Color.white, 0f, 0.5f));
        TDVisuals.Sphere(container, "EyeR", new Vector3(0.09f, 1.34f, 0.19f), 0.06f, TDVisuals.Mat(Color.white, 0f, 0.5f));

        // arms
        shoulderR = Pivot(container, "ShoulderR", new Vector3(0.26f, 1.06f, 0f));
        elbowR = Pivot(shoulderR, "ElbowR", new Vector3(0f, -0.26f, 0f));
        TDVisuals.Limb(shoulderR, "UpperArmR", Vector3.zero, new Vector3(0f, -0.26f, 0f), 0.05f, mShirt);
        TDVisuals.Limb(elbowR, "ForearmR", Vector3.zero, new Vector3(0f, -0.24f, 0f), 0.045f, mSkin);

        shoulderL = Pivot(container, "ShoulderL", new Vector3(-0.26f, 1.06f, 0f));
        elbowL = Pivot(shoulderL, "ElbowL", new Vector3(0f, -0.26f, 0f));
        TDVisuals.Limb(shoulderL, "UpperArmL", Vector3.zero, new Vector3(0f, -0.26f, 0f), 0.05f, mShirt);
        TDVisuals.Limb(elbowL, "ForearmL", Vector3.zero, new Vector3(0f, -0.24f, 0f), 0.045f, mSkin);

        // legs
        hipR = Pivot(container, "HipR", new Vector3(0.11f, 0.60f, 0f));
        kneeR = Pivot(hipR, "KneeR", new Vector3(0f, -0.28f, 0f));
        TDVisuals.Limb(hipR, "ThighR", Vector3.zero, new Vector3(0f, -0.28f, 0f), 0.07f, mPants);
        TDVisuals.Limb(kneeR, "ShinR", Vector3.zero, new Vector3(0f, -0.26f, 0f), 0.06f, mPants);
        TDVisuals.Box(kneeR, "ShoeR", new Vector3(0f, -0.28f, 0.05f), new Vector3(0.13f, 0.09f, 0.22f), mShoe);

        hipL = Pivot(container, "HipL", new Vector3(-0.11f, 0.60f, 0f));
        kneeL = Pivot(hipL, "KneeL", new Vector3(0f, -0.28f, 0f));
        TDVisuals.Limb(hipL, "ThighL", Vector3.zero, new Vector3(0f, -0.28f, 0f), 0.07f, mPants);
        TDVisuals.Limb(kneeL, "ShinL", Vector3.zero, new Vector3(0f, -0.26f, 0f), 0.06f, mPants);
        TDVisuals.Box(kneeL, "ShoeL", new Vector3(0f, -0.28f, 0.05f), new Vector3(0.13f, 0.09f, 0.22f), mShoe);

        built = true;
        lastPos = transform.position;
    }

    static Transform Pivot(Transform parent, string name, Vector3 localPos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        return g.transform;
    }

    void Update()
    {
        if (!built) return;

        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 d = transform.position - lastPos;
        d.y = 0f;
        speed = Mathf.Lerp(speed, d.magnitude / dt, dt * 8f);
        lastPos = transform.position;

        phase += dt * speed * 3.2f;
        float amp = Mathf.Clamp01(speed / 3f);
        ApplyPose(Mathf.Sin(phase) * amp);

        if (torso != null)
            torso.localPosition = new Vector3(0f, 0.92f + Mathf.Abs(Mathf.Sin(phase)) * 0.02f * amp, 0f);
    }

    // fixed pose for editor previews
    public void PoseForPreview(float p)
    {
        ApplyPose(Mathf.Sin(p));
    }

    void ApplyPose(float swing)
    {
        if (hipR != null) hipR.localRotation = Quaternion.Euler(swing * 34f, 0f, 0f);
        if (hipL != null) hipL.localRotation = Quaternion.Euler(-swing * 34f, 0f, 0f);
        if (kneeR != null) kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, -swing) * 50f, 0f, 0f);
        if (kneeL != null) kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, swing) * 50f, 0f, 0f);

        if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(-swing * 30f, 0f, 0f);
        if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(swing * 30f, 0f, 0f);
        if (elbowR != null) elbowR.localRotation = Quaternion.Euler(-40f, 0f, 0f);
        if (elbowL != null) elbowL.localRotation = Quaternion.Euler(-40f, 0f, 0f);
    }
}
