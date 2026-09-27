using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A compact snapshot of one board's dynamic entities, used for spectating.
/// Positions are board-local and quantised to centimetres so they fit in shorts.
/// </summary>
public class BoardSnapshot
{
    public struct MobSnap { public ushort Id; public byte Type; public short X; public short Z; public byte Hp; public byte Status; public byte Stacks; }
    public struct TowerSnap { public byte Type; public byte Tier; public short Cx; public short Cy; public byte Yaw; }
    public struct ProjSnap { public int Id; public byte Type; public short X; public short Y; public short Z; }

    public readonly List<MobSnap> Mobs = new List<MobSnap>();
    public readonly List<TowerSnap> Towers = new List<TowerSnap>();
    public readonly List<ProjSnap> Projs = new List<ProjSnap>();

    public static short Enc(float v) { return (short)Mathf.Clamp(Mathf.Round(v * 100f), -32000f, 32000f); }
    public static float Dec(short v) { return v / 100f; }
    public static float YawDeg(byte b) { return b / 255f * 360f; }

    public void Capture()
    {
        Mobs.Clear(); Towers.Clear(); Projs.Clear();
        TDGameManager gm = TDGameManager.Instance;
        if (gm == null) return;
        Vector3 off = gm.BoardOffset;

        for (int i = 0; i < gm.Mobs.Count; i++)
        {
            Mob m = gm.Mobs[i];
            if (m == null) continue;
            Vector3 p = m.transform.position - off;
            float hf = m.MaxHealth > 0f ? Mathf.Clamp01(m.Health / m.MaxHealth) : 0f;
            byte status = 0;
            if (m.IsSlowed) status |= 1;   // bit0: slowed (or stunned)
            if (m.IsStunned) status |= 2;  // bit1: stunned
            if (m.IsTarred) status |= 4;   // bit2: tar (+damage taken)
            Mobs.Add(new MobSnap
            {
                Id = m.NetId,
                Type = (byte)m.Def.index,
                X = Enc(p.x),
                Z = Enc(p.z),
                Hp = (byte)Mathf.Clamp(Mathf.RoundToInt(hf * 255f), 0, 255),
                Status = status,
                Stacks = (byte)Mathf.Clamp(m.PoisonStacks, 0, 255)
            });
        }

        foreach (Tower t in gm.AllTowers)
        {
            if (t == null) continue;
            float yaw = t.TurretYaw;
            Towers.Add(new TowerSnap
            {
                Type = (byte)t.Type,
                Tier = (byte)t.Tier,
                Cx = (short)t.CellX,
                Cy = (short)t.CellY,
                Yaw = (byte)Mathf.Clamp(Mathf.RoundToInt(yaw / 360f * 255f), 0, 255)
            });
        }

        Transform pr = gm.ProjectilesRoot;
        if (pr != null)
        {
            for (int i = 0; i < pr.childCount; i++)
            {
                Transform c = pr.GetChild(i);
                // Mirror only real tower projectiles: a zone/FX child has no
                // Projectile/PierceProjectile and is skipped (so it can't show as a
                // stray sphere). Type lets the remote render the right model.
                byte type;
                Projectile proj = c.GetComponent<Projectile>();
                if (proj != null)
                {
                    type = (byte)proj.Type;
                }
                else
                {
                    PierceProjectile pierce = c.GetComponent<PierceProjectile>();
                    if (pierce == null) continue;   // a zone/FX child, not a projectile
                    type = (byte)pierce.Type;
                }
                Vector3 p = c.position - off;
                Projs.Add(new ProjSnap
                {
                    Id = c.gameObject.GetHashCode(),
                    Type = type,
                    X = Enc(p.x), Y = Enc(p.y), Z = Enc(p.z)
                });
            }
        }
    }

    public void Write(FastBufferWriter w)
    {
        w.WriteValueSafe((ushort)Mobs.Count);
        for (int i = 0; i < Mobs.Count; i++)
        {
            MobSnap m = Mobs[i];
            w.WriteValueSafe(m.Id);
            w.WriteValueSafe(m.Type);
            w.WriteValueSafe(m.X);
            w.WriteValueSafe(m.Z);
            w.WriteValueSafe(m.Hp);
            w.WriteValueSafe(m.Status);
            w.WriteValueSafe(m.Stacks);
        }

        w.WriteValueSafe((ushort)Towers.Count);
        for (int i = 0; i < Towers.Count; i++)
        {
            TowerSnap t = Towers[i];
            w.WriteValueSafe(t.Type);
            w.WriteValueSafe(t.Tier);
            w.WriteValueSafe(t.Cx);
            w.WriteValueSafe(t.Cy);
            w.WriteValueSafe(t.Yaw);
        }

        w.WriteValueSafe((ushort)Projs.Count);
        for (int i = 0; i < Projs.Count; i++)
        {
            ProjSnap p = Projs[i];
            w.WriteValueSafe(p.Id);
            w.WriteValueSafe(p.Type);
            w.WriteValueSafe(p.X);
            w.WriteValueSafe(p.Y);
            w.WriteValueSafe(p.Z);
        }
    }

    public void Read(FastBufferReader r)
    {
        Mobs.Clear(); Towers.Clear(); Projs.Clear();

        r.ReadValueSafe(out ushort mc);
        for (int i = 0; i < mc; i++)
        {
            MobSnap m;
            r.ReadValueSafe(out m.Id);
            r.ReadValueSafe(out m.Type);
            r.ReadValueSafe(out m.X);
            r.ReadValueSafe(out m.Z);
            r.ReadValueSafe(out m.Hp);
            r.ReadValueSafe(out m.Status);
            r.ReadValueSafe(out m.Stacks);
            Mobs.Add(m);
        }

        r.ReadValueSafe(out ushort tc);
        for (int i = 0; i < tc; i++)
        {
            TowerSnap t;
            r.ReadValueSafe(out t.Type);
            r.ReadValueSafe(out t.Tier);
            r.ReadValueSafe(out t.Cx);
            r.ReadValueSafe(out t.Cy);
            r.ReadValueSafe(out t.Yaw);
            Towers.Add(t);
        }

        r.ReadValueSafe(out ushort pc);
        for (int i = 0; i < pc; i++)
        {
            ProjSnap p;
            r.ReadValueSafe(out p.Id);
            r.ReadValueSafe(out p.Type);
            r.ReadValueSafe(out p.X);
            r.ReadValueSafe(out p.Y);
            r.ReadValueSafe(out p.Z);
            Projs.Add(p);
        }
    }
}
