namespace SharpOpenGl.Engine.Rendering;

/// <summary>Extra procedural geometry layered on race hulls per catalog design.</summary>
internal static class ShipDesignVariantGeometry
{
    public static void Apply(RaceMeshWriter w, ShipDesignSpec spec, ShipDesignVariant v,
        float len, float wid, float hgt, string raceStyle)
    {
        ApplyNoseProfile(w, v.Nose, len, wid, hgt, v.NoseExtension);
        ApplyWingProfile(w, v.Wings, len, wid, hgt, v.WingSweepDelta, v.WingSpanDelta);
        AddHardpoints(w, len, wid, hgt, v.HardpointCount);
        AddAntennas(w, len, wid, hgt, v.AntennaCount, v.AsymmetryDelta);
        AddArmorPlates(w, len, wid, hgt, v.ArmorPlateCount, raceStyle);
        AddSensorArrays(w, len, wid, hgt, v.SensorArrayCount);
        AddCargoPods(w, len, wid, hgt, v.CargoPodCount, spec.HullClass);
        AddFinClusters(w, len, wid, hgt, v.FinClusterCount, v.ProtrusionDelta);
        AddAuxiliaryThrusters(w, len, wid, hgt, v.ExtraEngines);
        ApplyAttachmentBundle(w, v.AttachmentBundle, len, wid, hgt, spec.HullClass, raceStyle);

        if (spec.IsSpecial)
            AddSpecialFlourish(w, spec, len, wid, hgt, raceStyle);
    }

    private static void ApplyNoseProfile(RaceMeshWriter w, NoseProfile nose, float len, float wid, float hgt, float ext)
    {
        float z = len * (0.75f + ext);
        switch (nose)
        {
            case NoseProfile.Needle:
                w.Tri(0, hgt * 0.4f, z + wid * 0.35f, -wid * 0.08f, hgt * 0.15f, z, wid * 0.08f, hgt * 0.15f, z);
                break;
            case NoseProfile.Blunt:
                w.Tri(-wid * 0.2f, hgt * 0.25f, z, wid * 0.2f, hgt * 0.25f, z, 0, hgt * 0.45f, z - wid * 0.15f);
                break;
            case NoseProfile.TwinProng:
                w.Tri(-wid * 0.22f, hgt * 0.2f, z, -wid * 0.08f, 0, z - wid * 0.2f, 0, hgt * 0.1f, z - wid * 0.1f);
                w.Tri(wid * 0.22f, hgt * 0.2f, z, 0, hgt * 0.1f, z - wid * 0.1f, wid * 0.08f, 0, z - wid * 0.2f);
                break;
            case NoseProfile.Hammerhead:
                w.Tri(-wid * 0.45f, hgt * 0.15f, z - wid * 0.05f, wid * 0.45f, hgt * 0.15f, z - wid * 0.05f, 0, hgt * 0.35f, z - wid * 0.25f);
                break;
            case NoseProfile.Forked:
                w.Tri(0, hgt * 0.5f, z, -wid * 0.18f, hgt * 0.1f, z - wid * 0.3f, wid * 0.18f, hgt * 0.1f, z - wid * 0.3f);
                break;
        }
    }

    private static void ApplyWingProfile(RaceMeshWriter w, WingProfile wings, float len, float wid, float hgt,
        float sweepDelta, float spanDelta)
    {
        float z = len * 0.12f;
        float span = wid * (1f + spanDelta);
        float sweep = sweepDelta * wid;
        switch (wings)
        {
            case WingProfile.Swept:
                w.Tri(-span * 0.35f, hgt * 0.08f, z, -span * 0.95f, 0, z - sweep, -span * 0.4f, 0, z - wid * 0.12f);
                w.Tri(span * 0.35f, hgt * 0.08f, z, span * 0.4f, 0, z - wid * 0.12f, span * 0.95f, 0, z - sweep);
                break;
            case WingProfile.Canted:
                w.Tri(-span * 0.5f, hgt * 0.35f, z, -span * 0.25f, hgt * 0.05f, z - wid * 0.08f, 0, hgt * 0.15f, z);
                w.Tri(span * 0.5f, hgt * 0.35f, z, 0, hgt * 0.15f, z, span * 0.25f, hgt * 0.05f, z - wid * 0.08f);
                break;
            case WingProfile.Ring:
                w.Tri(-span * 0.55f, hgt * 0.45f, z - wid * 0.05f, span * 0.55f, hgt * 0.45f, z - wid * 0.05f, 0, hgt * 0.65f, z + wid * 0.08f);
                break;
            case WingProfile.Stub:
                w.Tri(-span * 0.28f, hgt * 0.12f, z, -span * 0.42f, 0, z - wid * 0.05f, -span * 0.15f, 0, z);
                w.Tri(span * 0.28f, hgt * 0.12f, z, span * 0.15f, 0, z, span * 0.42f, 0, z - wid * 0.05f);
                break;
            case WingProfile.Wide:
                w.Tri(-span * 1.05f, hgt * 0.06f, z - wid * 0.08f, -span * 0.3f, hgt * 0.1f, z, 0, hgt * 0.2f, z + wid * 0.05f);
                w.Tri(span * 1.05f, hgt * 0.06f, z - wid * 0.08f, 0, hgt * 0.2f, z + wid * 0.05f, span * 0.3f, hgt * 0.1f, z);
                break;
        }
    }

    private static void AddHardpoints(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float side = (i % 2 == 0 ? -1f : 1f) * wid * (0.35f + i * 0.08f);
            float z = len * (0.05f - i * 0.12f);
            w.Tri(side, hgt * 0.55f, z, side + wid * 0.06f * MathF.Sign(side), hgt * 0.35f, z - wid * 0.05f, side, hgt * 0.2f, z - wid * 0.08f);
        }
    }

    private static void AddAntennas(RaceMeshWriter w, float len, float wid, float hgt, int count, float asym)
    {
        for (int i = 0; i < count; i++)
        {
            float x = wid * (0.1f + asym * 0.3f) * (i % 2 == 0 ? -1f : 1f);
            float z = len * (-0.15f - i * 0.08f);
            w.Tri(x, hgt * (1.1f + i * 0.15f), z, x - wid * 0.04f, hgt * 0.7f, z, x + wid * 0.04f, hgt * 0.7f, z);
        }
    }

    private static void AddArmorPlates(RaceMeshWriter w, float len, float wid, float hgt, int count, string style)
    {
        float plateH = style == "blocky" ? hgt * 0.5f : hgt * 0.35f;
        for (int i = 0; i < count; i++)
        {
            float z = len * (0.2f - i * 0.18f);
            w.Tri(-wid * 0.42f, plateH, z, wid * 0.42f, plateH, z, 0, plateH * 0.7f, z - wid * 0.12f);
        }
    }

    private static void AddSensorArrays(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float z = len * (0.35f + i * 0.05f);
            w.Tri(0, hgt * (0.85f + i * 0.1f), z, -wid * 0.12f, hgt * 0.55f, z - wid * 0.06f, wid * 0.12f, hgt * 0.55f, z - wid * 0.06f);
        }
    }

    private static void AddCargoPods(RaceMeshWriter w, float len, float wid, float hgt, int count, string hull)
    {
        if (count <= 0) return;
        float z = len * (hull is "freighter" or "transport" ? -0.05f : 0.1f);
        for (int i = 0; i < count; i++)
        {
            float side = (i - (count - 1) * 0.5f) * wid * 0.28f;
            w.Tri(side, hgt * 0.45f, z - i * wid * 0.1f, side - wid * 0.1f, hgt * 0.1f, z - wid * 0.15f - i * wid * 0.08f, side + wid * 0.1f, hgt * 0.1f, z - wid * 0.15f - i * wid * 0.08f);
        }
    }

    private static void AddFinClusters(RaceMeshWriter w, float len, float wid, float hgt, int count, float protrusion)
    {
        for (int i = 0; i < count; i++)
        {
            float side = (i % 2 == 0 ? -1f : 1f);
            float z = len * (-0.35f - i * 0.1f);
            float reach = wid * (0.25f + protrusion);
            w.Tri(side * reach, hgt * (0.6f + protrusion), z, side * wid * 0.15f, hgt * 0.2f, z - wid * 0.08f, 0, hgt * 0.35f, z);
        }
    }

    private static void AddAuxiliaryThrusters(RaceMeshWriter w, float len, float wid, float hgt, int count)
    {
        float z = -len * 0.48f;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * wid * 0.22f;
            w.Tri(x, hgt * 0.12f, z, x - wid * 0.05f, 0, z - wid * 0.12f, x + wid * 0.05f, 0, z - wid * 0.12f);
        }
    }

    private static void ApplyAttachmentBundle(RaceMeshWriter w, int bundle, float len, float wid, float hgt, string hull, string style)
    {
        switch (bundle % 16)
        {
            case 0: AddDeflectorRing(w, len * 0.25f, wid, hgt); break;
            case 1: AddRamProw(w, len, wid, hgt); break;
            case 2: AddTwinBoom(w, len, wid, hgt); break;
            case 3: AddDockingClamps(w, len, wid, hgt); break;
            case 4: AddSpikeCluster(w, len, wid, hgt, style); break;
            case 5: AddBridgeBulge(w, len, wid, hgt); break;
            case 6: AddRefineryBoom(w, len, wid, hgt); break;
            case 7: AddHangarSlots(w, len, wid, hgt); break;
            case 8: AddShieldEmitter(w, len, wid, hgt); break;
            case 9: AddTorpedoRack(w, len, wid, hgt); break;
            case 10: AddCommDish(w, len, wid, hgt); break;
            case 11: AddVentralPod(w, len, wid, hgt); break;
            case 12: AddDorsalSpine(w, len, wid, hgt); break;
            case 13: AddSideRadiators(w, len, wid, hgt); break;
            case 14: AddAftSpoiler(w, len, wid, hgt); break;
            case 15: AddForwardCanard(w, len, wid, hgt); break;
        }

        if (hull is "carrier" or "dreadnought")
            AddHangarSlots(w, len, wid, hgt);
        if (hull is "miner")
            AddRefineryBoom(w, len, wid, hgt);
    }

    private static void AddSpecialFlourish(RaceMeshWriter w, ShipDesignSpec spec, float len, float wid, float hgt, string style)
    {
        float z = len * 0.4f;
        w.Tri(0, hgt * 1.35f, z, -wid * 0.2f, hgt * 0.85f, z - wid * 0.2f, wid * 0.2f, hgt * 0.85f, z - wid * 0.2f);
        if (style is "radiant" or "crystalline")
            AddDeflectorRing(w, len * 0.15f, wid * 1.1f, hgt * 1.15f);
    }

    private static void AddDeflectorRing(RaceMeshWriter w, float z, float wid, float hgt)
        => w.Tri(-wid * 0.35f, hgt * 0.7f, z, wid * 0.35f, hgt * 0.7f, z, 0, hgt * 0.95f, z + wid * 0.1f);

    private static void AddRamProw(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(0, hgt * 0.25f, len * 0.95f, -wid * 0.15f, 0, len * 0.7f, wid * 0.15f, 0, len * 0.7f);

    private static void AddTwinBoom(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(-wid * 0.55f, hgt * 0.2f, -len * 0.2f, -wid * 0.35f, hgt * 0.1f, len * 0.15f, -wid * 0.45f, 0, 0);
        w.Tri(wid * 0.55f, hgt * 0.2f, -len * 0.2f, wid * 0.45f, 0, 0, wid * 0.35f, hgt * 0.1f, len * 0.15f);
    }

    private static void AddDockingClamps(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(-wid * 0.5f, hgt * 0.15f, -len * 0.1f, -wid * 0.65f, 0, -len * 0.25f, -wid * 0.4f, 0, -len * 0.3f);
        w.Tri(wid * 0.5f, hgt * 0.15f, -len * 0.1f, wid * 0.4f, 0, -len * 0.3f, wid * 0.65f, 0, -len * 0.25f);
    }

    private static void AddSpikeCluster(RaceMeshWriter w, float len, float wid, float hgt, string style)
    {
        int spikes = style == "spiny" ? 4 : 2;
        for (int i = 0; i < spikes; i++)
        {
            float side = (i % 2 == 0 ? -1f : 1f) * wid * (0.2f + i * 0.12f);
            w.Tri(side, hgt * (0.9f + i * 0.15f), len * (-0.1f - i * 0.12f), side * 0.7f, hgt * 0.3f, len * (-0.25f - i * 0.08f), 0, hgt * 0.4f, len * (-0.15f - i * 0.1f));
        }
    }

    private static void AddBridgeBulge(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(0, hgt * 0.95f, len * 0.05f, -wid * 0.18f, hgt * 0.55f, -len * 0.08f, wid * 0.18f, hgt * 0.55f, -len * 0.08f);

    private static void AddRefineryBoom(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(wid * 0.35f, hgt * 0.3f, len * 0.2f, wid * 0.65f, hgt * 0.15f, len * 0.45f, wid * 0.45f, hgt * 0.05f, len * 0.25f);

    private static void AddHangarSlots(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(-wid * 0.25f, hgt * 0.08f, -len * 0.35f, wid * 0.25f, hgt * 0.08f, -len * 0.35f, 0, hgt * 0.08f, -len * 0.55f);
        w.Tri(-wid * 0.15f, hgt * 0.06f, -len * 0.15f, wid * 0.15f, hgt * 0.06f, -len * 0.15f, 0, hgt * 0.06f, -len * 0.32f);
    }

    private static void AddShieldEmitter(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(0, hgt * 1.05f, len * 0.12f, -wid * 0.1f, hgt * 0.65f, 0, wid * 0.1f, hgt * 0.65f, 0);

    private static void AddTorpedoRack(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(-wid * 0.38f, hgt * 0.25f, -len * 0.18f, -wid * 0.55f, hgt * 0.12f, -len * 0.35f, -wid * 0.28f, hgt * 0.08f, -len * 0.22f);

    private static void AddCommDish(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(wid * 0.3f, hgt * 0.75f, -len * 0.05f, wid * 0.5f, hgt * 0.55f, -len * 0.18f, wid * 0.38f, hgt * 0.95f, -len * 0.12f);

    private static void AddVentralPod(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(0, -hgt * 0.15f, len * 0.02f, -wid * 0.12f, 0, -len * 0.08f, wid * 0.12f, 0, -len * 0.08f);

    private static void AddDorsalSpine(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(0, hgt * 1.2f, len * 0.18f, -wid * 0.06f, hgt * 0.6f, -len * 0.2f, wid * 0.06f, hgt * 0.6f, -len * 0.2f);

    private static void AddSideRadiators(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(-wid * 0.62f, hgt * 0.35f, 0, -wid * 0.72f, hgt * 0.15f, -len * 0.15f, -wid * 0.48f, hgt * 0.12f, -len * 0.1f);
        w.Tri(wid * 0.62f, hgt * 0.35f, 0, wid * 0.48f, hgt * 0.12f, -len * 0.1f, wid * 0.72f, hgt * 0.15f, -len * 0.15f);
    }

    private static void AddAftSpoiler(RaceMeshWriter w, float len, float wid, float hgt)
        => w.Tri(-wid * 0.22f, hgt * 0.55f, -len * 0.55f, wid * 0.22f, hgt * 0.55f, -len * 0.55f, 0, hgt * 0.75f, -len * 0.68f);

    private static void AddForwardCanard(RaceMeshWriter w, float len, float wid, float hgt)
    {
        w.Tri(-wid * 0.55f, hgt * 0.18f, len * 0.42f, -wid * 0.25f, hgt * 0.08f, len * 0.28f, -wid * 0.35f, 0, len * 0.35f);
        w.Tri(wid * 0.55f, hgt * 0.18f, len * 0.42f, wid * 0.35f, 0, len * 0.35f, wid * 0.25f, hgt * 0.08f, len * 0.28f);
    }
}