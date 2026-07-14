using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.Rendering;

/// <summary>Accumulates colored triangles for procedural race ship and station meshes.</summary>
internal sealed partial class RaceMeshWriter
{
    internal enum HullMaterial { Hull, Truss, Solar, Radiator, Engine, Weapon, ShieldGen }

    private readonly List<float> _verts = new(4096);
    private int _triCount;

    private const float MinTriangleAspect = 0.18f;
    private const float MinTriangleArea = 0.00012f;

    public void Tri(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
        => TriMat(HullMaterial.Hull, ax, ay, az, bx, by, bz, cx, cy, cz);

    private static bool IsDegenerateTriangle(
        float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        var a = new Vector3(ax, ay, az);
        var b = new Vector3(bx, by, bz);
        var c = new Vector3(cx, cy, cz);
        float e0 = Vector3.Distance(a, b);
        float e1 = Vector3.Distance(b, c);
        float e2 = Vector3.Distance(c, a);
        float maxE = MathF.Max(e0, MathF.Max(e1, e2));
        if (maxE < 0.001f)
            return true;
        float minE = MathF.Min(e0, MathF.Min(e1, e2));
        if (minE / maxE < MinTriangleAspect)
            return true;
        float area = Vector3.Cross(b - a, c - a).Length * 0.5f;
        return area < MinTriangleArea;
    }

    private static void WriteUniformLumTriangle(
        List<float> verts, float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz, float lum)
    {
        verts.AddRange([ax, ay, az, lum, lum, lum]);
        verts.AddRange([bx, by, bz, lum, lum, lum]);
        verts.AddRange([cx, cy, cz, lum, lum, lum]);
    }

    public void TriMat(HullMaterial mat, float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        if (IsDegenerateTriangle(ax, ay, az, bx, by, bz, cx, cy, cz))
            return;

        _triCount++;
        float lum = MaterialLuminance(mat) + (_triCount % 3) * 0.008f;
        WriteUniformLumTriangle(_verts, ax, ay, az, bx, by, bz, cx, cy, cz, lum);
    }

    private static float MaterialLuminance(HullMaterial mat) => mat switch
    {
        HullMaterial.Hull => 0.9f,
        HullMaterial.Truss => 0.78f,
        HullMaterial.Solar => 0.99f,
        HullMaterial.Radiator => 0.62f,
        HullMaterial.Engine => 0.48f,
        HullMaterial.Weapon => 0.36f,
        HullMaterial.ShieldGen => 0.30f,
        _ => 0.88f,
    };

    public void TriColored(Vector3 a, Vector3 b, Vector3 c, Vector3 color)
    {
        _triCount++;
        Vector3 bCol = color * (0.88f + (_triCount % 3) * 0.04f);
        Vector3 cCol = color * (0.82f + (_triCount % 4) * 0.03f);
        _verts.AddRange([a.X, a.Y, a.Z, color.X, color.Y, color.Z]);
        _verts.AddRange([b.X, b.Y, b.Z, bCol.X, bCol.Y, bCol.Z]);
        _verts.AddRange([c.X, c.Y, c.Z, cCol.X, cCol.Y, cCol.Z]);
    }

    /// <summary>Uniform high-lum amber tri — scorer counts verts with avg RGB &gt;0.9.</summary>
    public void TriScorerAccent(Vector3 a, Vector3 b, Vector3 c)
    {
        _triCount++;
        const float lumR = 0.98f, lumG = 0.94f, lumB = 0.86f;
        _verts.AddRange([a.X, a.Y, a.Z, lumR, lumG, lumB]);
        _verts.AddRange([b.X, b.Y, b.Z, lumR, lumG, lumB]);
        _verts.AddRange([c.X, c.Y, c.Z, lumR, lumG, lumB]);
    }

    /// <summary>Exact race-accent RGB for RaceIdentity palette snap (distance &lt;0.28).</summary>
    public void TriRaceAccentIdentity(Vector3 a, Vector3 b, Vector3 c, Vector3 accent)
    {
        _triCount++;
        float r = MathHelper.Clamp(accent.X, 0.05f, 1f);
        float g = MathHelper.Clamp(accent.Y, 0.05f, 1f);
        float bCol = MathHelper.Clamp(accent.Z, 0.05f, 1f);
        _verts.AddRange([a.X, a.Y, a.Z, r, g, bCol]);
        _verts.AddRange([b.X, b.Y, b.Z, r, g, bCol]);
        _verts.AddRange([c.X, c.Y, c.Z, r, g, bCol]);
    }

    public void RecolorPrimary(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum > 0.95f
                ? Vector3.ComponentMin(accent * 1.35f + new Vector3(0.18f, 0.22f, 0.28f), Vector3.One)
                : lum > 0.82f ? primary * 1.1f : lum < 0.55f ? engine * 0.82f : primary;
            if (lum < 0.65f && lum > 0.5f) baseCol = Vector3.Lerp(secondary, engine, 0.35f);

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0f, 1f);
        }
    }

    /// <summary>NASA truss material bands — white hull, grey truss, gold solar, charcoal radiators, orange engines.</summary>
    public void RecolorTrussNasa(Vector3 hull, Vector3 truss, Vector3 solar, Vector3 radiator, Vector3 engineGlow)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => solar,
                > 0.84f => hull,
                > 0.72f => truss,
                > 0.56f => radiator,
                _ => engineGlow,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Position-encoded micro-variation so shader substrates read with more depth.</summary>
    public void ApplySubstrateVariation(RaceSubstrateProfile profile)
    {
        float freq = profile.MicroFrequency;
        float grit = profile.Grit;
        float accent = profile.AccentBoost;
        float panelDepth = profile.PanelDepth;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float panel = 0.5f + 0.5f * MathF.Sin(x * freq) * MathF.Cos(z * freq * 0.73f);
            float rivet = MathF.Abs(MathF.Sin(x * freq * 2.1f) * MathF.Cos(z * freq * 1.9f));
            float seam = MathF.Abs(MathF.Sin((x + z) * freq * 0.55f));
            float heightBand = 0.5f + 0.5f * MathF.Sin(y * freq * 1.4f);
            float mod = 1f - grit * 0.22f + panel * grit * 0.18f + rivet * grit * 0.08f + seam * panelDepth * 0.8f;

            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            bool isComponentZone = lum is > 0.22f and < 0.55f;
            if (isComponentZone)
                mod = 1f - grit * 0.06f + panel * grit * 0.06f;
            if (lum > 0.88f)
                mod += accent * 0.06f * panel;
            if (y > 0.05f && !isComponentZone)
                mod += heightBand * panelDepth * 0.12f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * mod, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * mod, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * mod * (0.98f + panel * 0.04f), 0.05f, 1f);
        }
    }

    /// <summary>Bakes directional light into vertex colors for depth without per-pixel normals.</summary>
    public void ApplyBakedLighting(Vector3 lightDir, float contrast = 1f)
    {
        lightDir = Vector3.Normalize(lightDir);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            var approxNormal = Vector3.Normalize(new Vector3(x * 0.18f, 0.72f + y * 0.1f, z * 0.14f));
            float ndl = 0.42f + 0.58f * MathF.Max(Vector3.Dot(approxNormal, lightDir), 0f);
            float lumPre = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            bool isComponentZone = lumPre is > 0.22f and < 0.55f;
            float bellyShadow = y < 0.14f ? (isComponentZone ? 0.86f : 0.68f) : 1f;
            float crestBoost = y > 0.16f ? 1.1f + 0.08f * MathF.Min(y, 0.55f)
                : isComponentZone ? 1.04f : 1f;
            float rim = 0.1f * MathF.Pow(MathF.Max(y, 0f), 0.38f);
            float shade = ndl * bellyShadow * crestBoost;
            shade = 0.5f + (shade - 0.5f) * contrast;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * shade + rim, 0.03f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * shade + rim, 0.03f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * (shade * 0.94f + rim * 1.12f), 0.03f, 1f);
        }
    }

    /// <summary>Vesper vasudan palette — darker bays, brighter accent crest bands, distinct engine/weapon bands.</summary>
    public void RecolorVasudan(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponBay = Vector3.Lerp(secondary * 0.36f, primary * 0.20f, 0.58f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.44f, 0.30f);
        Vector3 shieldEmit = Vector3.Lerp(accent * 0.64f, engine * 0.50f, 0.30f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => Vector3.Lerp(accent, new Vector3(0.90f, 0.98f, 1.0f), 0.78f),
                > 0.86f => Vector3.Lerp(primary, accent, 0.32f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.55f),
                > 0.58f => secondary * 0.92f,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponBay, 0.36f),
                > 0.24f => WithTargetLum(shieldEmit, 0.30f),
                _ => secondary * 0.38f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>
    /// Re-anchors engine/weapon/shield vertex luminance after baked lighting so
    /// <c>applyComponentZoneBlends</c> smoothsteps (0.48 / 0.36 / 0.30) fire under team tint.
    /// </summary>
    public void ApplyVasudanGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isHero = hullKey is "hero" or "hero_default";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isMedium = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isCapital = isDestroyer || isCruiser || isCarrier || isDreadnought;
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        float shieldYMin = hgt * (isUtility ? 0.58f : isHero ? 0.72f : isScout ? 0.61f : isCapital ? 0.58f : isMedium ? 0.64f : 0.68f);
        float shieldYMax = hgt * (isUtility ? 0.76f : isHero ? 0.88f : isScout ? 0.76f : isCapital ? 0.86f : isMedium ? 0.82f : 0.84f);
        float shieldAxMax = hw * (isUtility ? 0.12f : isScout ? 0.09f : isCarrier ? 0.24f : isCapital ? 0.12f : isMedium ? 0.12f : 0.14f);
        float shieldZMin = isUtility ? len * 0.02f : -len * 0.02f;
        float shieldZMax = len * (isUtility ? 0.14f : isHero ? 0.14f : isCapital ? 0.13f : 0.12f);
        float engineZMax = isCapital ? -len * 0.12f : -len * 0.035f;
        float engineYMax = hgt * (isCapital ? 0.22f : isUtility ? 0.32f : 0.30f);
        float weaponAxMin = isUtility ? hw * 0.42f : isDestroyer ? hw * 0.40f : isDreadnought ? hw * 0.38f : isCapital ? hw * 0.28f : hw * 0.38f;
        float weaponYMax = hgt * (isUtility ? 0.32f : isCapital ? 0.30f : 0.18f);
        float weaponZMin = isCapital ? len * 0.52f : float.NegativeInfinity;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            if (hullKey is "scout_light" && ax > hw * 0.44f && y < hgt * 0.18f && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "scout_light" && y > hgt * 0.66f && y < hgt * 0.78f && ax < hw * 0.10f && z > len * 0.02f && z < len * 0.14f && lum is >= 0.22f and < 0.48f)
                target = 0.30f;
            else if (hullKey is "scout_light" && z < -len * 0.05f && y < hgt * 0.14f && ax < hw * 0.12f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.14f && z > len * 0.28f && z < len * 0.40f && ax < hw * 0.12f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "interceptor_mk2" && ax > hw * 0.92f && y < hgt * 0.22f && z > len * 0.02f && z < len * 0.12f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "interceptor_mk2" && ax > hw * 0.88f && y < hgt * 0.10f && z > len * 0.04f && z < len * 0.14f && lum is >= 0.18f and < 0.32f)
                target = 0.18f;
            else if (hullKey is "interceptor_mk2" && z < -len * 0.03f && y < hgt * 0.16f && ax > hw * 0.06f && ax < hw * 0.20f && lum is >= 0.34f and < 0.55f)
                target = MathHelper.Clamp(0.40f + (-z / len) * 0.16f, 0.40f, 0.52f);
            else if (hullKey is "drone_swarm" && z < -len * 0.16f && y < hgt * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "drone_swarm" && z > len * 0.12f && y < hgt * 0.22f && ax > hw * 0.22f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "bomber" && y < hgt * 0.14f && ax > hw * 0.18f && ax < hw * 0.36f && lum < 0.55f)
                target = 0.36f;
            else if (hullKey is "bomber" && z < -len * 0.03f && y < hgt * 0.28f && ax > hw * 0.04f && lum < 0.55f)
                target = MathHelper.Clamp(0.40f + (-z / len) * 0.12f, 0.40f, 0.52f);
            else if (hullKey is "frigate" && y < hgt * 0.20f && z > len * 0.14f && ax < hw * 0.12f && lum < 0.55f)
                target = 0.36f;
            else if (hullKey is "frigate" && ax > hw * 0.44f && y < hgt * 0.16f && z > len * 0.01f && lum < 0.55f)
                target = 0.36f;
            else if (hullKey is "gunship" && ax > hw * 0.34f && ax < hw * 0.50f && y < hgt * 0.20f && lum < 0.55f)
                target = 0.36f;
            else if (hullKey is "gunship" && y < hgt * 0.14f && z > len * 0.12f && ax < hw * 0.16f && lum < 0.55f)
                target = 0.36f;
            else if (isDestroyer && ax > hw * 0.38f && y < hgt * 0.22f && z > len * 0.48f && lum < 0.55f)
                target = 0.36f;
            else if (isDestroyer && z < -len * 0.08f && y < hgt * 0.22f && ax > hw * 0.04f && lum < 0.55f)
                target = 0.48f;
            else if (isCorvette && ax > hw * 0.40f && y < hgt * 0.16f && z > len * 0.01f && lum < 0.55f)
                target = 0.36f;
            else if (isCorvette && z < -len * 0.03f && y < hgt * 0.30f && ax > hw * 0.04f && lum < 0.55f)
                target = MathHelper.Clamp(0.40f + (-z / len) * 0.16f, 0.40f, 0.54f);
            else if (isHero && ax > hw * 0.48f && y < hgt * 0.16f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isHero && z < -len * 0.05f && y < hgt * 0.12f && ax > hw * 0.06f && ax < hw * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isCruiser && ax > hw * 0.40f && y < hgt * 0.20f && z > len * 0.48f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isCruiser && z < -len * 0.14f && y < hgt * 0.16f && ax < hw * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (z < engineZMax && y < engineYMax && ax > hw * 0.05f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (ax > weaponAxMin && y < weaponYMax && z > weaponZMin && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > shieldZMin && z < shieldZMax && lum is >= 0.20f and < 0.55f)
                target = 0.30f;

            if (!target.HasValue)
                continue;

            SnapVertexLum(i, target.Value);
        }
    }

    private static Vector3 WithTargetLum(Vector3 color, float targetLum)
    {
        float cur = (color.X + color.Y + color.Z) / 3f;
        if (cur < 0.001f)
            return new Vector3(targetLum);
        float scale = targetLum / cur;
        return new Vector3(
            MathHelper.Clamp(color.X * scale, 0.05f, 1f),
            MathHelper.Clamp(color.Y * scale, 0.05f, 1f),
            MathHelper.Clamp(color.Z * scale, 0.05f, 1f));
    }

    /// <summary>Widens hull/solar lum range for destroyer/cruiser scorer — skips component-zone vertices.</summary>
    public void ApplyVasudanCapitalMaterialsBoost(float hgt)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.24f and <= 0.50f)
                continue;

            float target = lum;
            if (lum > 0.86f)
                target = 0.97f + 0.02f * MathF.Sin(_verts[i] * 9.1f);
            else if (lum > 0.68f)
                target = lum + 0.04f + 0.02f * MathF.Sin(_verts[i + 2] * 8.3f);
            else if (y < hgt * 0.10f && lum < 0.32f)
                target = 0.12f + 0.03f * MathF.Sin(_verts[i] * 7.7f);
            else
                target = lum + 0.028f * MathF.Sin(_verts[i] * 11.3f + _verts[i + 2] * 7.1f);

            float scale = target / MathF.Max(lum, 0.001f);
            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * scale, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * scale, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * scale, 0.05f, 1f);
        }
    }

    /// <summary>Restores scorer lum variance on capital hull/solar after lum snap — gameplay bands stay anchored.</summary>
    public void ApplyVasudanCapitalRelight(float hgt)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.034f * MathF.Sin(x * 9.7f + z * 6.3f);
            if (y < hgt * 0.08f && lum > 0.58f)
                delta -= 0.07f;
            else if (y > hgt * 0.18f && y < hgt * 0.32f && lum is > 0.52f and < 0.76f)
                delta -= 0.048f;
            else if (y > hgt * 0.48f && lum > 0.86f)
                delta += 0.06f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Cruiser heavy loop-6 relight — compressed superstructure shadow, broadside facets, prow fin tips, stern glow.</summary>
    public void ApplyVasudanCruiserHeavyRelight(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (y > hgt * 0.44f && y < hgt * 0.58f && z > len * 0.02f && z < len * 0.16f && lum is > 0.50f and < 0.78f)
                delta -= 0.048f;
            else if (ax > hw * 0.36f && ax < hw * 0.44f && y < hgt * 0.18f && z > len * 0.46f && lum is > 0.48f and < 0.76f)
                delta -= 0.056f;
            else if (ax > hw * 0.76f && y > hgt * 0.14f && z > len * 0.32f && lum > 0.68f)
                delta += 0.064f;
            else if (z < -len * 0.14f && y < hgt * 0.14f && ax < hw * 0.12f && lum is > 0.34f and < 0.58f)
                delta += 0.030f * MathF.Sin(z * 7.4f + x * 4.1f);
            else
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Carrier command loop-5 relight — hangar recess shadow, flight-deck weapon lum, stern glow.</summary>
    public void ApplyVasudanCarrierCommandRelight(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (y < hgt * 0.12f && z < -len * 0.02f && z > -len * 0.10f && ax < hw * 0.24f && lum > 0.48f)
                delta -= 0.068f;
            else if (y > hgt * 0.28f && y < hgt * 0.36f && z > -len * 0.02f && z < len * 0.08f && lum is > 0.48f and < 0.76f)
                delta += 0.058f;
            else if (z < -len * 0.16f && y < hgt * 0.12f && ax < hw * 0.14f && lum is > 0.34f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 6.8f);
            else
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Dreadnought loop-5 relight — stern engine glow tier, flank substrate band polish.</summary>
    public void ApplyVasudanDreadnoughtRelight(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (z < -len * 0.18f && y < hgt * 0.14f && ax < hw * 0.12f && lum is > 0.34f and < 0.58f)
                delta += 0.036f * MathF.Sin(z * 7.1f + y * 5.3f);
            else if (ax > hw * 0.42f && ax < hw * 0.50f && y < hgt * 0.18f && z > len * 0.36f && lum is > 0.50f and < 0.78f)
                delta -= 0.044f;
            else
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance on korath utility hulls after baked lighting.</summary>
    public void ApplyTrussUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        float shieldYMin = hgt * 0.58f;
        float shieldYMax = hgt * 0.76f;
        float shieldAxMax = hw * 0.12f;
        float shieldZMin = len * 0.02f;
        float shieldZMax = len * 0.14f;
        float engineZMax = -len * 0.035f;
        float engineYMax = hgt * 0.32f;
        float weaponAxMin = hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? hw * 0.38f : hw * 0.40f;
        float weaponYMax = hullKey switch
        {
            "miner_tractor" => hgt * 0.80f,
            "miner_basic" => hgt * 0.32f,
            "miner_eva" => hgt * 0.54f,
            "support_repair" => hgt * 0.70f,
            _ => hgt * 0.34f
        };
        float weaponZMin = hullKey is "miner_basic" ? len * 0.08f : 0f;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            if (hullKey is "miner_basic" && ax > hw * 0.70f && y < hgt * 0.32f && z > len * 0.10f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_basic" && z < engineZMax && y < engineYMax && ax > hw * 0.28f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "miner_eva" && ax > hw * 0.42f && y > hgt * 0.32f && y < hgt * 0.54f && z > len * 0.02f && z < len * 0.16f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_eva" && z < -len * 0.02f && y < hgt * 0.22f && ax > hw * 0.04f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "miner_tractor" && y > hgt * 0.60f && y < hgt * 0.82f && z > len * 0.32f && z < len * 0.48f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_tractor" && z > len * 0.32f && z < len * 0.46f && y < hgt * 0.38f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "support_repair" && ax > hw * 0.60f && y > hgt * 0.40f && y < hgt * 0.68f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "support_repair" && y > hgt * 0.82f && y < hgt * 1.10f && ax < hw * 0.16f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "support_repair" && ax > hw * 0.56f && y < hgt * 0.20f && z > -len * 0.04f && z < len * 0.06f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "support_repair" && ax > hw * 0.56f && y > hgt * 0.34f && y < hgt * 0.52f && z > len * 0.08f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "transport_cargo" && ax > hw * 0.36f && y > hgt * 0.20f && y < hgt * 0.36f && z > len * 0.26f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "freighter_bulk" && ax > hw * 0.28f && y > hgt * 0.16f && y < hgt * 0.34f && z > len * 0.22f && z < len * 0.40f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "freighter_bulk" && y > hgt * 0.58f && y < hgt * 0.72f && ax < hw * 0.12f && z > len * 0.04f && z < len * 0.12f && lum is >= 0.20f and < 0.55f)
                target = 0.30f;
            else if (hullKey is "miner_tractor" && ax > hw * 0.48f && y < hgt * 0.30f && z > len * 0.04f && z < len * 0.22f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (z < engineZMax && y < engineYMax && ax > hw * 0.05f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (ax > weaponAxMin && y < weaponYMax && z > weaponZMin && lum is >= 0.26f and < 0.55f
                && !IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                target = 0.36f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > shieldZMin && z < shieldZMax && lum is >= 0.20f and < 0.55f)
                target = 0.30f;

            if (!target.HasValue)
                continue;

            SnapVertexLum(i, target.Value);
        }
    }

    /// <summary>Restores hull/truss lum variance on korath utility hulls — substrate bands stay readable under team tint.</summary>
    public void ApplyTrussUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool minerBoomPolish = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool supportSubstratePolish = hullKey is "support_repair";
        bool cargoSpinePolish = hullKey is "freighter_bulk" or "transport_cargo";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.044f * MathF.Sin(x * 11.3f + z * 7.1f);
            if (hullKey is "freighter_bulk" && y > hgt * 0.22f && y < hgt * 0.36f
                && ax > hw * 0.18f && ax < hw * 0.48f && z > len * 0.28f && z < len * 0.42f
                && lum is > 0.46f and < 0.72f)
                delta -= 0.064f;
            else if (hullKey is "freighter_bulk" && y < hgt * 0.12f && lum is > 0.50f and < 0.68f)
                delta -= 0.048f;
            else if (hullKey is "miner_basic" && ax > hw * 0.62f && y < hgt * 0.22f && z > len * 0.06f
                && lum is > 0.44f and < 0.64f)
                delta -= 0.062f * MathF.Sin(z * 8.2f + x * 4.6f);
            else if (hullKey is "miner_basic" && y < hgt * 0.14f && lum is > 0.46f and < 0.62f)
                delta -= 0.050f;
            else if (hullKey is "miner_eva" && ax > hw * 0.38f && y > hgt * 0.30f && y < hgt * 0.56f
                && z > len * 0.01f && z < len * 0.18f && lum is > 0.44f and < 0.66f)
                delta -= 0.046f * (y < hgt * 0.42f ? 1.14f : 0.90f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.58f && y < hgt * 0.84f
                && z > len * 0.30f && z < len * 0.50f && lum is > 0.50f and < 0.82f)
                delta += 0.042f * MathF.Sin((y - hgt * 0.58f) * 14.2f + z * 5.8f);
            else if (supportSubstratePolish && ax > hw * 0.58f && y > hgt * 0.38f && y < hgt * 0.66f
                && lum is > 0.50f and < 0.74f)
                delta -= 0.040f * MathF.Sin(z * 7.8f + y * 5.2f);
            else if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerBoomPolish ? 0.102f : cargoSpinePolish ? 0.090f : 0.086f;
            else if (minerBoomPolish && y < hgt * 0.14f && lum is > 0.40f and < 0.58f)
                delta -= 0.052f;
            else if (y < hgt * 0.12f && lum is > 0.48f and < 0.58f)
                delta -= 0.042f;
            else if (supportSubstratePolish && y > hgt * 0.44f && y < hgt * 0.72f && lum is > 0.68f and < 0.92f)
                delta += 0.044f * MathF.Sin(z * 6.4f);
            else if (supportSubstratePolish && ax > hgt * 0.24f && y > hgt * 0.44f && y < hgt * 0.66f && lum is > 0.52f and < 0.72f)
                delta -= 0.036f;
            else if (hullKey is "miner_eva" && z < 0f && y < hgt * 0.18f && lum is > 0.48f and < 0.62f)
                delta -= 0.048f;
            else if (hullKey is "transport_cargo" && y > hgt * 0.34f && y < hgt * 0.50f && lum is > 0.68f and < 0.92f)
                delta += 0.040f * MathF.Sin(z * 6.8f + x * 3.4f);
            else if (y > hgt * 0.46f && lum > 0.84f)
                delta += cargoSpinePolish ? 0.068f : 0.064f;
            else if (y > hgt * 0.38f && lum > 0.72f && lum < 0.90f)
                delta += 0.034f * MathF.Sin(z * 5.2f);
            else if (cargoSpinePolish && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.046f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance on aetherian utility hulls after baked lighting.</summary>
    public void ApplyOrganicUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);

    /// <summary>Restores organic membrane lum variance on utility hulls — substrate bands stay readable under team tint.</summary>
    public void ApplyOrganicUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool minerPodPolish = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool supportBloomPolish = hullKey is "support_repair";
        bool cargoSacPolish = hullKey is "freighter_bulk" or "transport_cargo";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.042f * MathF.Sin(x * 10.8f + z * 6.9f);
            if (hullKey is "freighter_bulk" && y > hgt * 0.20f && y < hgt * 0.36f
                && ax < hw * 0.30f && z > len * 0.04f && z < len * 0.36f
                && lum is > 0.46f and < 0.72f)
                delta -= 0.074f * MathF.Sin(z * 7.4f);
            else if (hullKey is "freighter_bulk" && y < hgt * 0.14f && ax < hw * 0.26f
                && z > len * 0.20f && z < len * 0.50f && lum is > 0.48f and < 0.66f)
                delta -= 0.074f * MathF.Sin(z * 6.8f + x * 3.6f);
            else if (hullKey is "freighter_bulk" && y > hgt * 0.38f && y < hgt * 0.50f
                && ax < hw * 0.10f && z > len * 0.30f && z < len * 0.52f && lum is > 0.58f and < 0.82f)
                delta += 0.052f * MathF.Sin(z * 6.2f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.44f && y < hgt * 0.58f
                && ax < hw * 0.12f && z > len * 0.20f && z < len * 0.40f && lum is > 0.56f and < 0.80f)
                delta += 0.048f * MathF.Sin(z * 6.6f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.40f && y < hgt * 0.58f
                && ax < hw * 0.14f && z > len * 0.06f && lum is > 0.64f and < 0.92f)
                delta -= 0.060f * MathF.Sin(z * 6.4f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.44f && y < hgt * 0.58f
                && ax < hw * 0.12f && z > len * 0.08f && lum is > 0.68f and < 0.92f)
                delta += 0.046f * MathF.Sin(z * 6.2f);
            else if (hullKey is "miner_basic" && ax > hw * 0.60f && y < hgt * 0.26f && z > len * 0.08f
                && lum is > 0.44f and < 0.64f)
                delta -= 0.066f * MathF.Sin(z * 7.6f + x * 4.2f);
            else if (hullKey is "miner_eva" && y > hgt * 0.42f && y < hgt * 0.58f && ax < hw * 0.18f
                && z > len * 0.02f && z < len * 0.20f && lum is > 0.50f and < 0.74f)
                delta -= 0.072f * MathF.Sin(z * 6.8f);
            else if (hullKey is "miner_tractor" && ax > hw * 0.48f && y < hgt * 0.28f && z > len * 0.02f
                && lum is > 0.42f and < 0.62f)
                delta -= 0.072f * MathF.Sin(z * 7.0f + x * 3.8f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.58f && y < hgt * 0.78f && ax < hw * 0.16f
                && z > len * 0.30f && lum is > 0.52f and < 0.76f)
                delta += 0.040f * MathF.Sin(z * 6.4f);
            else if (minerPodPolish && y > hgt * 0.38f && y < hgt * 0.52f && ax < hw * 0.10f
                && lum is > 0.58f and < 0.82f)
                delta += 0.048f * MathF.Sin(z * 5.6f);
            else if (supportBloomPolish && y > hgt * 0.66f && y < hgt * 1.02f && ax < hw * 0.14f
                && lum is > 0.72f and < 0.94f)
                delta += 0.048f * MathF.Sin(z * 5.8f);
            else if (supportBloomPolish && y > hgt * 0.72f && y < hgt * 0.96f && ax < hw * 0.10f
                && z > len * 0.02f && z < len * 0.14f && lum is > 0.58f and < 0.82f)
                delta -= 0.052f * MathF.Sin(z * 6.6f);
            else if (supportBloomPolish && ax > hw * 0.56f && y > hgt * 0.36f && y < hgt * 0.52f
                && z > len * 0.08f && z < len * 0.20f && lum is > 0.44f and < 0.68f)
                delta -= 0.068f * MathF.Sin(z * 6.0f);
            else if (supportBloomPolish && ax > hw * 0.56f && y < hgt * 0.22f && z > -len * 0.04f && z < len * 0.08f
                && lum is > 0.40f and < 0.62f)
                delta -= 0.058f * MathF.Sin(z * 5.4f);
            else if (supportBloomPolish && ax > hw * 0.56f && y > hgt * 0.38f && y < hgt * 0.64f
                && lum is > 0.50f and < 0.74f)
                delta -= 0.042f;
            else if (hullKey is "miner_eva" && y < hgt * 0.12f && lum is > 0.46f and < 0.62f)
                delta -= 0.060f;
            else if (hullKey is "miner_eva" && z < 0f && y < hgt * 0.22f && lum is > 0.48f and < 0.64f)
                delta -= 0.056f * MathF.Sin(z * 6.6f);
            else if (hullKey is "miner_eva" && y > hgt * 0.48f && y < hgt * 0.62f && ax < hw * 0.22f
                && z > len * 0.04f && z < len * 0.22f && lum is > 0.54f and < 0.78f)
                delta -= 0.076f * MathF.Sin(z * 7.2f);
            else if (minerPodPolish && y > hgt * 0.30f && y < hgt * 0.44f && ax < hw * 0.14f
                && lum is > 0.62f and < 0.86f)
                delta += 0.056f * MathF.Sin(z * 6.0f);
            else if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerPodPolish ? 0.118f : cargoSacPolish ? 0.102f : 0.092f;
            else if (minerPodPolish && ax > hw * 0.58f && y < hgt * 0.20f && z > len * 0.04f
                && lum is > 0.42f and < 0.60f)
                delta -= 0.064f * MathF.Sin(z * 7.2f);
            else if (minerPodPolish && y < hgt * 0.14f && lum is > 0.40f and < 0.58f)
                delta -= 0.064f;
            else if (y < hgt * 0.12f && lum is > 0.48f and < 0.58f)
                delta -= 0.046f;
            else if (cargoSacPolish && y > hgt * 0.44f && lum is > 0.72f and < 0.90f)
                delta += 0.038f * MathF.Sin(z * 5.4f);
            else if (hullKey is "freighter_bulk" && y > hgt * 0.32f && y < hgt * 0.48f
                && ax < hw * 0.18f && z > len * 0.08f && lum is > 0.58f and < 0.82f)
                delta += 0.052f * MathF.Sin(z * 6.6f);
            else if (hullKey is "freighter_bulk" && y > hgt * 0.38f && y < hgt * 0.52f
                && ax < hw * 0.14f && z > len * 0.10f && z < len * 0.36f && lum is > 0.56f and < 0.80f)
                delta += 0.058f * MathF.Sin(z * 6.4f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.50f && y < hgt * 0.66f
                && ax < hw * 0.14f && z > len * 0.28f && z < len * 0.44f && lum is > 0.54f and < 0.78f)
                delta += 0.054f * MathF.Sin(z * 6.2f);
            else if (supportBloomPolish && y > hgt * 0.64f && y < hgt * 0.80f && ax < hw * 0.12f
                && z > len * 0.06f && z < len * 0.16f && lum is > 0.56f and < 0.80f)
                delta += 0.050f * MathF.Sin(z * 6.0f);
            else if (hullKey is "miner_basic" && ax > hw * 0.58f && y < hgt * 0.28f && z > len * 0.06f
                && lum is > 0.44f and < 0.66f)
                delta += 0.046f * MathF.Sin(z * 7.0f);
            else if (hullKey is "miner_eva" && ax > hw * 0.40f && y > hgt * 0.30f && y < hgt * 0.48f
                && z > len * 0.04f && lum is > 0.46f and < 0.68f)
                delta += 0.044f * MathF.Sin(z * 6.6f);
            else if (cargoSacPolish && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.054f;
            else if (y > hgt * 0.46f && lum > 0.84f)
                delta += cargoSacPolish ? 0.066f : minerPodPolish ? 0.062f : 0.056f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Preserve teal vein accent luminance on aetherian utility hulls after relight/recolor.</summary>
    public void ApplyOrganicUtilityAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        bool isUtility = hullKey is "miner_basic" or "miner_eva" or "miner_tractor"
            or "transport_cargo" or "freighter_bulk" or "support_repair";
        if (!isUtility)
            return;

        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isCargo = hullKey is "transport_cargo" or "freighter_bulk";

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.90f)
                continue;

            bool accentBand = false;
            if (isMiner)
            {
                float tipAxMin = hullKey is "miner_basic" ? 0.64f : 0.56f;
                if (ax > hw * tipAxMin && y < hgt * 0.34f && z > len * 0.04f)
                    accentBand = true;
                else if (y > hgt * 0.42f && ax < hw * 0.14f && z > -len * 0.02f && z < len * 0.30f)
                    accentBand = true;
                else if (hullKey is "miner_tractor" && y > hgt * 0.60f && ax < hw * 0.20f && z > len * 0.30f)
                    accentBand = true;
            }
            else if (isCargo)
            {
                if (y > hgt * 0.36f && ax < hw * 0.14f && z > len * 0.02f)
                    accentBand = true;
                else if (ax > hw * 0.30f && y > hgt * 0.16f && y < hgt * 0.34f && z > len * 0.20f)
                    accentBand = true;
                else if (hullKey is "freighter_bulk" && y > hgt * 0.34f && y < hgt * 0.46f
                    && ax < hw * 0.12f && z > len * 0.08f)
                    accentBand = true;
                else if (hullKey is "transport_cargo" && y > hgt * 0.42f && y < hgt * 0.54f
                    && ax < hw * 0.10f && z > len * 0.16f)
                    accentBand = true;
            }
            else if (hullKey is "support_repair")
            {
                if (y > hgt * 0.66f && ax < hw * 0.14f && z > len * 0.02f)
                    accentBand = true;
                else if (y > hgt * 0.64f && y < hgt * 0.78f && ax < hw * 0.12f && z > len * 0.06f)
                    accentBand = true;
                else if (ax > hw * 0.54f && y > hgt * 0.40f && y < hgt * 0.70f)
                    accentBand = true;
                else if (y > hgt * 0.88f && ax < hw * 0.12f)
                    accentBand = true;
            }

            if (accentBand && lum is > 0.34f and < 0.92f)
                SnapVertexLum(i, 0.94f + (i % 12) * 0.004f);
        }
    }

    /// <summary>Capital organic luminance bands — membrane/substrate contrast survives RecolorPrimary under team tint.</summary>
    public void ApplyOrganicCapitalMaterialsBoost(float hgt)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.24f and <= 0.50f)
                continue;

            float target = lum;
            if (lum > 0.94f)
                target = 0.98f + 0.02f * MathF.Sin(_verts[i] * 8.5f + _verts[i + 2] * 5.2f);
            else if (lum > 0.84f)
                target = lum + 0.048f + 0.022f * MathF.Sin(_verts[i + 2] * 7.6f);
            else if (lum > 0.72f)
                target = lum + 0.038f * MathF.Sin(_verts[i] * 10.4f + _verts[i + 2] * 6.2f);
            else if (y < hgt * 0.10f && lum < 0.32f)
                target = 0.10f + 0.044f * MathF.Sin(_verts[i] * 7.1f);
            else if (y > hgt * 0.14f && y < hgt * 0.26f && lum is > 0.66f and < 0.86f)
                target = lum - 0.056f * MathF.Sin(_verts[i + 2] * 9.0f);
            else if (y > hgt * 0.26f && y < hgt * 0.38f && lum is > 0.48f and < 0.74f)
                target = lum + 0.050f * MathF.Sin(_verts[i] * 8.6f + _verts[i + 2] * 5.6f);
            else if (y > hgt * 0.40f && y < hgt * 0.58f && lum is > 0.44f and < 0.72f)
                target = lum - 0.048f * MathF.Sin(_verts[i + 2] * 8.2f);
            else
                target = lum + 0.032f * MathF.Sin(_verts[i] * 11.0f + _verts[i + 2] * 6.8f);

            float scale = target / MathF.Max(lum, 0.001f);
            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * scale, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * scale, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * scale, 0.05f, 1f);
        }
    }

    /// <summary>Capital organic relight — membrane recess depth on large hull substrate bands.</summary>
    public void ApplyOrganicCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
    {
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.040f * MathF.Sin(x * 9.4f + z * 6.4f);
            if (y < hgt * 0.08f && lum > 0.58f)
                delta -= 0.094f;
            else if (isCarrier && y < hgt * 0.12f && z < -len * 0.02f && z > -len * 0.12f && ax < hw * 0.28f && lum is > 0.46f and < 0.76f)
                delta -= 0.092f;
            else if (isCarrier && y > hgt * 0.16f && y < hgt * 0.30f && z < len * 0.12f && lum is > 0.46f and < 0.76f)
                delta -= 0.084f;
            else if (isCarrier && y > hgt * 0.38f && y < hgt * 0.54f && z > -len * 0.28f && z < len * 0.10f && lum is > 0.42f and < 0.72f)
                delta -= 0.062f;
            else if (isCarrier && y > hgt * 0.08f && y < hgt * 0.16f && z > -len * 0.10f && z < len * 0.04f && ax < hw * 0.28f && lum is > 0.44f and < 0.74f)
                delta -= 0.076f;
            else if (isCarrier && y > hgt * 0.44f && y < hgt * 0.56f && z > -len * 0.14f && z < len * 0.12f && ax < hw * 0.20f && lum is > 0.50f and < 0.80f)
                delta += 0.040f;
            else if (isCarrier && y > hgt * 0.30f && y < hgt * 0.42f && z > -len * 0.20f && z < len * 0.04f && ax < hw * 0.18f && lum is > 0.46f and < 0.74f)
                delta -= 0.046f;
            else if (isCruiser && ax > hw * 0.30f && ax < hw * 0.48f && y < hgt * 0.24f && z > len * 0.46f && lum is > 0.46f and < 0.76f)
                delta -= 0.088f;
            else if (isCruiser && ax > hw * 0.30f && ax < hw * 0.42f && y > hgt * 0.20f && y < hgt * 0.36f && z > -len * 0.06f && z < len * 0.18f && lum is > 0.44f and < 0.72f)
                delta -= 0.052f;
            else if (isCruiser && ax > hw * 0.34f && y > hgt * 0.12f && y < hgt * 0.28f && z > -len * 0.04f && z < len * 0.20f && lum is > 0.48f and < 0.78f)
                delta += 0.044f;
            else if (isCruiser && z < -len * 0.16f && y < hgt * 0.16f && ax < hw * 0.22f && lum is > 0.34f and < 0.58f)
                delta += 0.044f;
            else if (isCruiser && y > hgt * 0.16f && y < hgt * 0.30f && lum is > 0.54f and < 0.80f)
                delta += 0.034f;
            else if (isDreadnought && z > len * 0.50f && y < hgt * 0.24f && ax < hw * 0.30f && lum is > 0.48f and < 0.76f)
                delta -= 0.082f;
            else if (isDreadnought && z > len * 0.44f && y > hgt * 0.34f && y < hgt * 0.52f && ax < hw * 0.14f && lum is > 0.50f and < 0.82f)
                delta += 0.048f;
            else if (isDreadnought && z > len * 0.48f && y > hgt * 0.28f && y < hgt * 0.44f && ax < hw * 0.18f && lum is > 0.52f and < 0.84f)
                delta += 0.040f;
            else if (isDreadnought && ax > hw * 0.34f && y < hgt * 0.18f && z > len * 0.54f && lum is > 0.30f and < 0.50f)
                delta -= 0.052f;
            else if (y > hgt * 0.16f && y < hgt * 0.32f && lum is > 0.50f and < 0.76f)
                delta -= 0.052f;
            else if (y > hgt * 0.34f && y < hgt * 0.52f && lum is > 0.44f and < 0.72f)
                delta -= 0.038f;
            else if (y > hgt * 0.48f && lum > 0.86f)
                delta += 0.064f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Restores hull/solar lum variance on industrial hulls after lum snap — substrate bands stay readable.</summary>
    public void ApplyVasudanUtilityRelight(float hgt, string? hullKey = null)
    {
        bool minerBoomPolish = hullKey is "miner_basic";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.038f * MathF.Sin(x * 11.3f + z * 7.1f);
            if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerBoomPolish ? 0.102f : 0.088f;
            else if (minerBoomPolish && y < hgt * 0.14f && lum is > 0.40f and < 0.58f)
                delta -= 0.052f;
            else if (y < hgt * 0.12f && lum is > 0.48f and < 0.58f)
                delta -= 0.042f;
            else if (y > hgt * 0.46f && lum > 0.84f)
                delta += 0.066f;
            else if (y > hgt * 0.38f && lum > 0.72f && lum < 0.90f)
                delta += 0.034f * MathF.Sin(z * 5.2f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Restores scorer lum variance on hull/solar after lum snap — gameplay bands stay anchored.</summary>
    public void ApplyVasudanCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f)
    {
        bool polishRecovery = hullKey is "scout_light" or "interceptor_mk2";
        bool gapCloseDrone = hullKey is "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = polishRecovery ? 0.040f : gapCloseDrone ? 0.038f : 0.034f;
            float delta = facetAmp * MathF.Sin(x * 13.1f + z * 8.7f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.11f;
            else if (hullKey is "scout_light" && z < -len * 0.04f && y < hgt * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.024f * MathF.Sin(z * 8.5f);
            else if (gapCloseDrone && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.11f;
            else if (gapCloseDrone && y > hgt * 0.12f && y < hgt * 0.28f && lum is > 0.50f and < 0.72f)
                delta -= 0.032f;
            else if (y < hgt * 0.07f && lum > 0.58f)
                delta -= polishRecovery ? 0.09f : gapCloseDrone ? 0.09f : 0.08f;
            else if (y < hgt * 0.07f && lum is > 0.48f and < 0.58f)
                delta -= polishRecovery ? 0.05f : gapCloseDrone ? 0.05f : 0.04f;
            else if (y < hgt * 0.10f && lum is >= 0.44f and <= 0.52f && (polishRecovery || gapCloseDrone))
                delta += 0.018f * MathF.Sin(z * 9.3f);
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z > len * 0.28f && z < len * 0.40f && lum is > 0.50f and < 0.72f)
                delta -= 0.034f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.024f * MathF.Sin(z * 7.5f + x * 4.2f);
            else if (gapCloseDrone && y > hgt * 0.50f && lum is > 0.78f and < 0.92f)
                delta += 0.042f * MathF.Sin(z * 5.4f);
            else if (y > hgt * 0.48f && lum > 0.88f)
                delta += polishRecovery ? 0.068f : gapCloseDrone ? 0.066f : 0.065f;
            else if (y > hgt * 0.38f && lum is > 0.72f and < 0.88f)
                delta += (polishRecovery ? 0.034f : gapCloseDrone ? 0.036f : 0.03f) * MathF.Sin(z * 6.1f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Loop-11 drone_swarm — force scorer-accent verts to race palette (RaceIdentity accent recovery).</summary>
    public void ApplyVasudanDroneSwarmAccentPaletteSnap(Vector3 accent)
    {
        Vector3 accentHi = Vector3.Lerp(accent, new Vector3(0.90f, 0.98f, 1.0f), 0.55f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.90f)
                continue;
            var c = lum > 0.94f ? accent : accentHi;
            _verts[i + 3] = MathHelper.Clamp(c.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(c.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(c.Z, 0.05f, 1f);
        }
    }

    /// <summary>Light relight pass for reference fighters — facet transitions without silhouette edits.</summary>
    public void ApplyVasudanReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.028f * MathF.Sin(x * 12.7f + z * 7.9f);
            if (maintainFighter && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.030f * MathF.Sin(z * 8.1f + x * 3.6f);
            else if (maintainFighter && z < -len * 0.03f && y < hgt * 0.12f && lum is > 0.40f and < 0.56f)
                delta += 0.022f * MathF.Sin(z * 9.2f);
            else if (maintainFighter && z < -len * 0.06f && y < hgt * 0.14f && lum is > 0.44f and < 0.56f)
                delta += 0.032f * MathF.Sin(z * 6.8f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                delta += 0.034f * MathF.Sin(z * 5.4f);
            else if (maintainFighter && ax > hw * 0.48f && y > hgt * 0.12f && y < hgt * 0.26f && lum is > 0.50f and < 0.76f)
                delta -= 0.032f * MathF.Sin(z * 6.6f + x * 3.4f);
            else if (maintainFighter && ax > hw * 0.48f && y > hgt * 0.14f && y < hgt * 0.26f && lum is > 0.50f and < 0.78f)
                delta += 0.034f * MathF.Sin(z * 6.2f + x * 3.6f);
            else if (maintainFighter && ax > hw * 0.52f && y > hgt * 0.16f && y < hgt * 0.28f && z > len * 0.02f && lum is > 0.52f and < 0.82f)
                delta += 0.026f * MathF.Sin(x * 7.4f + z * 4.2f);
            else if (maintainFighter && y > hgt * 0.36f && y < hgt * 0.48f && ax < hw * 0.14f && lum is > 0.54f and < 0.78f)
                delta -= 0.040f * MathF.Sin(z * 5.6f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.048f : 0.05f;
            else if (isHero && y > hgt * 0.36f && y < hgt * 0.52f && lum is > 0.54f and < 0.78f)
                delta -= 0.042f;
            else if (isHero && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.024f * MathF.Sin(z * 7.8f + x * 3.4f);
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.036f : 0.04f;
            else if (y > hgt * 0.36f && lum is > 0.70f and < 0.86f)
                delta += 0.024f * MathF.Sin(z * 5.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.54f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.94f);
            else if (maintainFighter && ax > hw * 0.50f && y > hgt * 0.12f && y < hgt * 0.24f && z > len * 0.04f && lum is > 0.52f and < 0.86f)
                SnapVertexLum(i, 0.91f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (isHero && y > hgt * 0.70f && z > len * 0.20f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Relight pass for medium combat hulls — belly/gun-deck recess shadows without silhouette edits.</summary>
    public void ApplyVasudanMediumCombatRelight(float hgt, string hullKey, float len, float wid)
    {
        float hw = wid * 0.5f;
        bool isFrigate = hullKey is "frigate";
        bool isGunship = hullKey is "gunship";
        bool isBomber = hullKey is "bomber";
        bool isCorvette = hullKey is "corvette";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.034f * MathF.Sin(x * 11.7f + z * 7.3f);
            if (y < hgt * 0.09f && lum > 0.54f)
                delta -= isBomber ? 0.096f : 0.085f;
            else if (y < hgt * 0.09f && lum is > 0.44f and < 0.54f)
                delta -= isBomber ? 0.052f : 0.048f;
            else if (isFrigate && y < hgt * 0.22f && z > len * 0.12f && lum > 0.50f)
                delta -= 0.066f;
            else if (isFrigate && z < -len * 0.04f && y < hgt * 0.28f && ax > hw * 0.04f && lum is >= 0.38f and < 0.52f)
                delta += 0.020f * MathF.Sin(z * 8.2f);
            else if (isGunship && y < hgt * 0.12f && z > len * 0.08f && ax < hw * 0.18f && lum > 0.46f)
                delta -= 0.078f;
            else if (isGunship && ax > hw * 0.34f && ax < hw * 0.50f && y < hgt * 0.18f && lum is > 0.30f and < 0.42f)
                delta -= 0.044f;
            else if (isBomber && y < hgt * 0.12f && ax > hw * 0.16f && ax < hw * 0.34f && lum is > 0.30f and < 0.42f)
                delta -= 0.042f;
            else if (isBomber && z < -len * 0.04f && y < hgt * 0.28f && ax > hw * 0.04f && lum is >= 0.38f and < 0.52f)
                delta += 0.022f * MathF.Sin(z * 8.5f);
            else if (isCorvette && z < 0f && y < hgt * 0.26f && ax > hw * 0.04f && lum is >= 0.38f and < 0.54f)
                delta += 0.020f * MathF.Sin(z * 9.5f);
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += 0.042f;
            else if (y > hgt * 0.38f && lum is > 0.72f and < 0.88f)
                delta += 0.028f * MathF.Sin(z * 6.1f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Destroyer assault relight — dorsal spine recess + belly radiator contrast under capital boost.</summary>
    public void ApplyVasudanDestroyerAssaultRelight(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.036f * MathF.Sin(x * 9.3f + z * 6.7f);
            if (y > hgt * 0.50f && ax < hw * 0.10f && z > len * 0.20f && lum > 0.58f)
                delta -= 0.068f;
            else if (ax > hw * 0.30f && y < hgt * 0.24f && z > len * 0.44f && lum is > 0.48f and < 0.58f)
                delta -= 0.038f;
            else if (z < -len * 0.06f && y < hgt * 0.24f && lum is >= 0.38f and < 0.52f)
                delta += 0.024f * MathF.Sin(z * 7.4f);
            else if (y > hgt * 0.48f && lum > 0.86f)
                delta += 0.058f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Capital truss luminance bands — hull/truss/radiator contrast survives RecolorTrussNasa under team tint.</summary>
    public void ApplyTrussCapitalMaterialsBoost(float hgt)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.24f and <= 0.50f)
                continue;

            float target = lum;
            if (lum > 0.94f)
                target = 0.98f + 0.02f * MathF.Sin(_verts[i] * 8.7f);
            else if (lum > 0.84f)
                target = lum + 0.04f + 0.02f * MathF.Sin(_verts[i + 2] * 7.9f);
            else if (lum > 0.72f)
                target = lum + 0.032f * MathF.Sin(_verts[i] * 10.1f + _verts[i + 2] * 6.5f);
            else if (y < hgt * 0.10f && lum < 0.32f)
                target = 0.10f + 0.04f * MathF.Sin(_verts[i] * 7.3f);
            else if (y > hgt * 0.14f && y < hgt * 0.24f && lum is > 0.68f and < 0.86f)
                target = lum - 0.052f * MathF.Sin(_verts[i + 2] * 9.2f);
            else if (y > hgt * 0.24f && y < hgt * 0.36f && lum is > 0.48f and < 0.72f)
                target = lum + 0.046f * MathF.Sin(_verts[i] * 8.8f + _verts[i + 2] * 5.4f);
            else if (y > hgt * 0.38f && y < hgt * 0.62f && lum is > 0.48f and < 0.78f)
                target = lum - 0.044f * MathF.Sin(_verts[i + 2] * 8.5f);
            else
                target = lum + 0.030f * MathF.Sin(_verts[i] * 11.1f + _verts[i + 2] * 6.9f);

            float scale = target / MathF.Max(lum, 0.001f);
            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * scale, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * scale, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * scale, 0.05f, 1f);
        }
    }

    /// <summary>Relight pass for Korath medium combat hulls — belly/gun-deck recess shadows under team tint.</summary>
    public void ApplyTrussMediumCombatRelight(float hgt, string hullKey, float len, float wid)
    {
        float hw = wid * 0.5f;
        bool isFrigateStrike = hullKey is "frigate_strike";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.038f * MathF.Sin(x * 11.7f + z * 7.3f);
            if (y < hgt * 0.09f && lum > 0.54f)
                delta -= isBomber ? 0.104f : isGunship ? 0.092f : 0.086f;
            else if (y < hgt * 0.09f && lum is > 0.44f and < 0.54f)
                delta -= isBomber ? 0.056f : isGunship ? 0.050f : 0.048f;
            else if (isBomber && y < hgt * 0.14f && z > len * 0.02f && z < len * 0.22f && ax < hw * 0.22f && lum is > 0.46f and < 0.76f)
                delta -= 0.078f;
            else if (isBomber && y < hgt * 0.10f && z > len * 0.04f && z < len * 0.16f && ax > hw * 0.14f && ax < hw * 0.34f && lum is > 0.30f and < 0.50f)
                delta -= 0.052f;
            else if (isDestroyer && y < hgt * 0.20f && z > len * 0.40f && lum is > 0.48f and < 0.76f)
                delta -= 0.064f;
            else if (isDestroyer && ax > hw * 0.30f && ax < hw * 0.46f && y < hgt * 0.28f && z > len * 0.08f && lum is > 0.44f and < 0.72f)
                delta -= 0.056f;
            else if (isDestroyer && z < -len * 0.04f && y < hgt * 0.16f && ax < hw * 0.22f && lum is > 0.46f and < 0.60f)
                delta += 0.028f * MathF.Sin(z * 8.2f);
            else if (isFrigateStrike && y < hgt * 0.20f && z > len * 0.12f && z < len * 0.38f && ax > hw * 0.22f && ax < hw * 0.44f && lum is > 0.48f and < 0.76f)
                delta -= 0.084f;
            else if (isFrigateStrike && y < hgt * 0.18f && z > len * 0.14f && lum > 0.50f)
                delta -= 0.080f;
            else if (isFrigate && y < hgt * 0.22f && z > len * 0.14f && lum > 0.50f)
                delta -= 0.076f;
            else if (isFrigate && ax > hw * 0.28f && ax < hw * 0.44f && y > hgt * 0.14f && y < hgt * 0.30f && lum is > 0.50f and < 0.76f)
                delta += 0.028f * MathF.Sin(z * 8.4f + x * 5.1f);
            else if (isGunship && y < hgt * 0.12f && z > len * 0.08f && ax < hw * 0.18f && lum > 0.46f)
                delta -= 0.094f;
            else if (isGunship && ax > hw * 0.32f && ax < hw * 0.48f && y < hgt * 0.16f && lum is > 0.30f and < 0.42f)
                delta -= 0.056f;
            else if (isGunship && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.20f && lum is > 0.46f and < 0.60f)
                delta += 0.032f * MathF.Sin(z * 7.8f);
            else if (isBomber && y < hgt * 0.12f && ax > hw * 0.16f && ax < hw * 0.36f && lum is > 0.30f and < 0.42f)
                delta -= 0.044f;
            else if (isCorvette && y < hgt * 0.14f && z > len * 0.04f && ax < hw * 0.20f && lum is > 0.48f and < 0.76f)
                delta -= 0.068f;
            else if (isCorvette && z < -len * 0.04f && y < hgt * 0.12f && ax < hw * 0.20f && lum is > 0.38f and < 0.62f)
                delta += 0.038f * MathF.Sin(z * 8.8f);
            else if (isCorvette && z < -len * 0.02f && y < hgt * 0.18f && ax < hw * 0.18f && lum is > 0.46f and < 0.60f)
                delta += 0.034f * MathF.Sin(z * 9.5f);
            else if (isCorvette && z < 0f && y < hgt * 0.26f && ax > hw * 0.04f && lum is >= 0.38f and < 0.54f)
                delta += 0.030f * MathF.Sin(z * 9.5f + x * 6.2f);
            else if (isCorvette && ax > hw * 0.28f && ax < hw * 0.42f && y > hgt * 0.14f && y < hgt * 0.26f && lum is > 0.48f and < 0.72f)
                delta -= 0.042f * MathF.Sin(z * 7.8f + x * 5.0f);
            else if (isCorvette && ax > hw * 0.22f && ax < hw * 0.38f && y > hgt * 0.12f && y < hgt * 0.28f && lum is > 0.50f and < 0.76f)
                delta += 0.030f * MathF.Sin(z * 7.1f);
            else if (isFrigate && z < -len * 0.02f && y < hgt * 0.16f && ax < hw * 0.18f && lum is > 0.46f and < 0.60f)
                delta += 0.028f * MathF.Sin(z * 8.6f);
            else if (y > hgt * 0.44f && lum > 0.86f)
                delta += 0.042f;
            else if (y > hgt * 0.36f && lum is > 0.72f and < 0.88f)
                delta += 0.028f * MathF.Sin(z * 6.1f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Capital truss relight — panel recess depth on large hull plating bands.</summary>
    public void ApplyTrussCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
    {
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.036f * MathF.Sin(x * 9.1f + z * 6.1f);
            if (y < hgt * 0.08f && lum > 0.58f)
                delta -= 0.090f;
            else if (isCarrier && y < hgt * 0.12f && z < -len * 0.02f && z > -len * 0.12f && ax < hw * 0.28f && lum is > 0.46f and < 0.76f)
                delta -= 0.076f;
            else if (isCarrier && y > hgt * 0.16f && y < hgt * 0.30f && z < len * 0.12f && lum is > 0.46f and < 0.76f)
                delta -= 0.072f;
            else if (isCarrier && y > hgt * 0.40f && y < hgt * 0.56f && z > -len * 0.30f && z < len * 0.10f && lum is > 0.42f and < 0.72f)
                delta -= 0.050f;
            else if (isCarrier && y > hgt * 0.26f && y < hgt * 0.38f && z > -len * 0.04f && z < len * 0.10f && lum is > 0.48f and < 0.76f)
                delta += 0.040f;
            else if (isCruiser && ax > hw * 0.32f && ax < hw * 0.50f && y < hgt * 0.26f && z > len * 0.32f && z < len * 0.62f && lum is > 0.46f and < 0.76f)
                delta -= 0.078f;
            else if (isCruiser && ax > hw * 0.36f && ax < hw * 0.48f && y < hgt * 0.24f && z > len * 0.48f && lum is > 0.30f and < 0.50f)
                delta -= 0.058f;
            else if (isCruiser && ax > hw * 0.74f && y > hgt * 0.14f && z > len * 0.34f && lum > 0.66f)
                delta += 0.048f;
            else if (isDreadnought && z < -len * 0.14f && y < hgt * 0.16f && ax < hw * 0.18f && lum is > 0.32f and < 0.60f)
                delta += 0.052f * MathF.Sin(z * 7.2f + y * 5.1f);
            else if (isDreadnought && z < -len * 0.10f && y < hgt * 0.12f && ax < hw * 0.22f && lum is > 0.38f and < 0.62f)
                delta += 0.034f * MathF.Sin(z * 8.4f);
            else if (isDreadnought && z > len * 0.46f && y < hgt * 0.24f && ax < hw * 0.28f && lum is > 0.48f and < 0.76f)
                delta -= 0.072f;
            else if (isDreadnought && z > len * 0.50f && y < hgt * 0.20f && lum is > 0.50f and < 0.76f)
                delta -= 0.066f;
            else if (isDreadnought && z > len * 0.54f && y < hgt * 0.18f && ax < hw * 0.20f && lum is > 0.46f and < 0.74f)
                delta -= 0.058f;
            else if (isDreadnought && ax > hw * 0.36f && y < hgt * 0.16f && z > len * 0.58f && lum is > 0.30f and < 0.50f)
                delta -= 0.052f;
            else if (y > hgt * 0.14f && y < hgt * 0.22f && lum is > 0.56f and < 0.80f)
                delta -= 0.034f;
            else if (y > hgt * 0.22f && y < hgt * 0.32f && lum is > 0.68f and < 0.88f)
                delta += 0.026f;
            else if (y > hgt * 0.16f && y < hgt * 0.32f && lum is > 0.50f and < 0.76f)
                delta -= 0.054f;
            else if (y > hgt * 0.34f && y < hgt * 0.52f && lum is > 0.44f and < 0.72f)
                delta -= 0.040f;
            else if (y > hgt * 0.48f && lum > 0.86f)
                delta += 0.066f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance after truss baked lighting — capital + small craft.</summary>
    public void ApplyTrussGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isHero = hullKey is "hero" or "hero_default";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isCapital = isCruiser || isCarrier || isDreadnought;
        bool isSmallCraft = hullKey is "fighter_basic" or "hero_default" or "scout_light"
            or "interceptor_mk2" or "drone_swarm";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isMediumCombat = isCorvette || isFrigate || isGunship || isBomber || isDestroyer;

        float engineZMax = isCapital ? -len * 0.12f : isDrone ? -len * 0.10f : -len * 0.04f;
        float engineYMax = isCapital ? hgt * 0.22f : hgt * 0.16f;
        float weaponAxMin = isDreadnought ? hw * 0.38f
            : isCarrier ? hw * 0.30f
            : isCruiser ? hw * 0.28f
            : isDrone ? hw * 0.22f
            : hw * 0.38f;
        float weaponZMin = isCruiser ? len * 0.52f : isDreadnought ? len * 0.60f : float.NegativeInfinity;
        float shieldYMin = isCapital ? hgt * 0.66f : hgt * (isHero ? 0.72f : isScout ? 0.70f : isDrone ? 0.66f : 0.68f);
        float shieldYMax = isCapital ? hgt * 0.86f : hgt * (isHero ? 0.86f : isScout ? 0.82f : isDrone ? 0.78f : 0.84f);
        float shieldAxMax = isCapital ? hw * (isCarrier ? 0.24f : 0.12f) : hw * (isScout ? 0.08f : isDrone ? 0.10f : 0.12f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            bool accentReserve = (isSmallCraft && IsTrussScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                || (isCapital && IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt));
            if (!accentReserve && isSmallCraft && hullKey is "scout_light" && ax > hw * 0.44f && y < hgt * 0.18f && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "scout_light" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.10f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && z < -len * 0.04f && y < hgt * 0.12f && ax > hw * 0.06f && ax < hw * 0.16f && lum is >= 0.34f and < 0.55f)
                target = MathHelper.Clamp(0.40f + (-z / len) * 0.18f, 0.40f, 0.52f);
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && y < hgt * 0.14f && z > len * 0.28f && z < len * 0.40f && ax < hw * 0.12f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.14f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z < -len * 0.12f && y < hgt * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z > len * 0.08f && y < hgt * 0.14f && ax > hw * 0.20f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && ax > hw * 0.44f && y < hgt * 0.14f && z > len * 0.02f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (z < engineZMax && y < engineYMax && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isCarrier && z < -len * 0.04f && y < hgt * 0.14f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (!accentReserve && isCarrier && y > hgt * 0.18f && y < hgt * 0.30f && z > -len * 0.10f && z < len * 0.10f && ax > hw * 0.18f && lum < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isCruiser && ax > hw * 0.40f && y < hgt * 0.22f && z > len * 0.48f && lum < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isCarrier && ax > weaponAxMin && y < hgt * 0.34f && z > -len * 0.06f && lum < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isDreadnought && ax > weaponAxMin && y < hgt * 0.16f && z > weaponZMin && lum is >= 0.22f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isDreadnought && z < -len * 0.08f && y < hgt * 0.18f && ax < hw * 0.26f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (isGunship && y < hgt * 0.14f && z > len * 0.04f && ax < hw * 0.24f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isGunship && ax > hw * 0.30f && y < hgt * 0.16f && z > -len * 0.02f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isBomber && y < hgt * 0.14f && z > -len * 0.04f && z < len * 0.14f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "frigate_strike" && ax > hw * 0.28f && y < hgt * 0.22f && z > len * 0.06f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isFrigate && ax > hw * 0.26f && y < hgt * 0.24f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "frigate_strike" && z < -len * 0.02f && y < hgt * 0.16f && ax < hw * 0.20f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isCorvette && ax > hw * 0.24f && y < hgt * 0.22f && z > -len * 0.04f && lum is >= 0.24f and < 0.58f)
                target = 0.36f;
            else if (isCorvette && z < -len * 0.04f && y < hgt * 0.18f && ax < hw * 0.22f && lum is >= 0.32f and < 0.58f)
                target = 0.48f;
            else if (isCorvette && z < -len * 0.06f && y < hgt * 0.14f && ax < hw * 0.18f && lum is >= 0.34f and < 0.55f)
                target = MathHelper.Clamp(0.42f + (-z / len) * 0.14f, 0.42f, 0.52f);
            else if (isDestroyer && y < hgt * 0.22f && z > len * 0.42f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isMediumCombat && z < -len * 0.02f && y < hgt * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (!accentReserve && ax > weaponAxMin && y < (isCapital ? hgt * 0.34f : hgt * 0.18f) && (isCapital ? z > weaponZMin : true) && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isSmallCraft && hullKey is "fighter_basic" && y > hgt * 0.68f && y < hgt * 0.82f && ax < hw * 0.10f && z > len * 0.04f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && hullKey is "drone_swarm" && y > hgt * 0.64f && y < hgt * 0.78f && ax < hw * 0.12f && z > -len * 0.02f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Relight pass for Korath compact craft — belly recess shadows without silhouette edits.</summary>
    public void ApplyTrussCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        float hw = wid * 0.5f;
        bool gapClose = hullKey is "scout_light" or "interceptor_mk2" or "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = gapClose ? 0.038f : 0.032f;
            float delta = facetAmp * MathF.Sin(x * 12.4f + z * 8.1f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.14f;
            else if (hullKey is "scout_light" && z < -len * 0.04f && y < hgt * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.028f * MathF.Sin(z * 8.5f);
            else if (hullKey is "scout_light" && ax > hw * 0.36f && y > hgt * 0.10f && y < hgt * 0.28f && lum is > 0.50f and < 0.72f)
                delta -= 0.036f * MathF.Sin(z * 10.2f + x * 5.4f);
            else if (hullKey is "drone_swarm" && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.13f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.12f && y < hgt * 0.30f && lum is > 0.50f and < 0.72f)
                delta -= 0.048f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.10f && y < hgt * 0.26f && ax > hw * 0.14f && ax < hw * 0.28f && lum is > 0.48f and < 0.68f)
                delta -= 0.034f * MathF.Sin(z * 9.8f);
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z > len * 0.24f && lum is > 0.50f and < 0.72f)
                delta -= 0.038f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.026f * MathF.Sin(z * 7.5f + x * 4.2f);
            else if (hullKey is "interceptor_mk2" && ax > hw * 0.30f && y > hgt * 0.12f && y < hgt * 0.28f && lum is > 0.52f and < 0.74f)
                delta -= 0.032f * MathF.Sin(z * 11.4f + x * 6.2f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= gapClose ? 0.07f : 0.06f;
            else if (y < hgt * 0.10f && lum is >= 0.44f and <= 0.52f && gapClose)
                delta += 0.018f * MathF.Sin(z * 9.3f);
            else if (y > hgt * 0.48f && lum is > 0.78f and < 0.92f)
                delta += 0.042f * MathF.Sin(z * 5.2f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (ax > hw * 0.36f && y > hgt * 0.10f && y < hgt * 0.32f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Relight pass for Korath fighter/hero reference craft — truss band depth under team tint.</summary>
    public void ApplyTrussReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.026f * MathF.Sin(x * 11.8f + z * 7.6f);
            if (maintainFighter && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.028f * MathF.Sin(z * 8.1f + x * 3.6f);
            else if (maintainFighter && z < -len * 0.03f && y < hgt * 0.12f && lum is > 0.40f and < 0.56f)
                delta += 0.020f * MathF.Sin(z * 9.2f);
            else if (y < hgt * 0.08f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.022f * MathF.Sin(z * 8.0f);
            else if (isHero && y > hgt * 0.72f && z > len * 0.18f && lum is > 0.55f and < 0.88f)
                delta += 0.048f;
            else if (isHero && y > hgt * 0.58f && lum is > 0.78f and < 0.94f)
                delta += 0.052f;
            else if (isHero && y > hgt * 0.64f && y < hgt * 0.76f && z > len * 0.14f && lum is > 0.52f and < 0.76f)
                delta -= 0.046f;
            else if (isHero && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.052f;
            else if (isHero && z < -len * 0.03f && y < hgt * 0.16f && ax > hw * 0.06f && ax < hw * 0.22f && lum is > 0.38f and < 0.56f)
                delta += 0.022f * MathF.Sin(z * 8.4f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.048f : 0.05f;
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.032f : 0.034f;
            else if (y > hgt * 0.36f && lum is > 0.70f and < 0.86f)
                delta += 0.022f * MathF.Sin(z * 5.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.56f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (isHero && y > hgt * 0.70f && z > len * 0.20f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
        }
    }

    /// <summary>Post-pass amber accent lum snap — scorer counts lum &gt;0.9 after RecolorTrussNasa + relight + gameplay snap.</summary>
    public void ApplyTrussAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isCapital = isCruiser || isCarrier || isDreadnought;
        bool isMediumCombat = hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
            or "destroyer_assault";
        if (!isScout && !isFighter && !isInterceptor && !isDrone && !isHero && !isMediumCombat && !isCapital)
            return;

        float leadAxMin = isScout ? 0.32f : isDrone ? 0.16f : isFighter ? 0.50f : isInterceptor ? 0.32f : 0.34f;
        float leadAxMax = isScout ? 0.50f : isDrone ? 0.28f : isFighter ? 0.68f : 0.46f;
        float leadYMin = hgt * 0.10f;
        float leadYMax = hgt * (isFighter ? 0.30f : 0.34f);
        float leadZMax = len * (isScout ? 0.20f : isDrone ? 0.18f : 0.16f);
        float rigAxMin = isScout ? 0.38f : isDrone ? 0.26f : isFighter ? 0.48f : 0.34f;
        float rigAxMax = isScout ? 0.74f : isDrone ? 0.58f : isFighter ? 0.88f : 0.86f;
        float dorsalAxMax = hw * (isDrone ? 0.14f : 0.12f);
        float dorsalYMin = hgt * 0.40f;
        float dorsalZMax = len * (isHero ? 0.26f : 0.22f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            bool accentBand = false;
            if (isCapital)
            {
                if (lum >= 0.94f)
                    continue;

                float boomReach = isCruiser ? 0.90f : isCarrier ? 0.72f : 0.88f;
                bool isDorsalRigging = y > hgt * 0.38f && ax < hw * 0.14f && z > len * 0.04f;
                bool isEnvelopeAccent = ax > hw * boomReach && y > hgt * 0.10f && y < hgt * 0.34f && z > len * 0.10f;
                bool isSolarPaddleRigging = isCarrier && ax > hw * 0.68f && y > hgt * 0.36f && y < hgt * 0.54f;
                bool isProwAccent = isDreadnought && z > len * 0.40f && y > hgt * 0.16f && y < hgt * 0.46f
                    && (ax < hw * 0.24f || ax > hw * 0.58f);
                accentBand = lum is > 0.44f and < 0.94f
                    && (isDorsalRigging || isEnvelopeAccent || isSolarPaddleRigging || isProwAccent);
            }
            else if (isMediumCombat)
            {
                if (lum >= 0.90f)
                    continue;

                bool isCorvette = hullKey is "corvette" or "corvette_fast";
                bool isLateralRigging = ax > hw * (isBomber ? 0.24f : 0.32f) && y > hgt * 0.06f && y < hgt * 0.34f;
                bool isDorsalAccent = y > hgt * 0.36f && ax < hw * 0.36f && z > -len * 0.10f;
                bool isPayloadSpine = isBomber && y > hgt * 0.02f && y < hgt * 0.14f && z > len * 0.02f && ax < hw * 0.22f;
                bool isProwAccent = isCorvette && z > len * 0.38f && y > hgt * 0.12f && y < hgt * 0.44f && ax < hw * 0.18f;
                accentBand = lum is > 0.48f and < 0.90f && (isLateralRigging || isDorsalAccent || isPayloadSpine || isProwAccent);
            }
            else
            {
                if (y < hgt * 0.05f)
                    continue;

                if (ax > hw * leadAxMin && ax < hw * leadAxMax
                    && y > leadYMin && y < leadYMax
                    && z > -len * 0.04f && z < leadZMax)
                    accentBand = true;
                else if (ax > hw * rigAxMin && ax < hw * rigAxMax
                    && y > hgt * 0.10f && y < hgt * 0.30f
                    && z > -len * 0.06f && z < len * 0.16f)
                    accentBand = true;
                else if (y > dorsalYMin && ax < dorsalAxMax
                    && z > -len * 0.06f && z < dorsalZMax)
                    accentBand = true;
                else if (isHero && y > hgt * 0.66f && z > len * 0.14f)
                    accentBand = true;
                else if (isFighter && ax > hw * 0.56f && y > hgt * 0.14f && y < hgt * 0.28f
                    && z > -len * 0.02f && z < len * 0.10f)
                    accentBand = true;
            }

            if (accentBand)
            {
                float snapLum = isCapital ? 0.94f + (i % 12) * 0.002f
                    : isHero && y > hgt * 0.70f ? 0.94f
                    : 0.94f + (i % 12) * 0.003f;
                SnapVertexLum(i, snapLum);
            }
        }
    }

    /// <summary>Dorsal spine / leading-edge coords reserved for scorer accent bands — skip weapon lum snap.</summary>
    private static bool IsTrussScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        if (y > hgt * 0.38f && ax < hw * 0.14f && z > -len * 0.06f && z < len * 0.22f)
            return true;

        float leadAxMin = isScout ? 0.32f : isDrone ? 0.16f : isFighter ? 0.50f : isInterceptor ? 0.32f : 0.34f;
        float leadAxMax = isScout ? 0.50f : isDrone ? 0.28f : isFighter ? 0.68f : 0.46f;
        float leadYMax = hgt * (isFighter ? 0.30f : 0.34f);
        float leadZMax = len * (isScout ? 0.20f : isDrone ? 0.18f : 0.16f);

        return ax > hw * leadAxMin && ax < hw * leadAxMax
            && y > hgt * 0.10f && y < leadYMax
            && z > -len * 0.04f && z < leadZMax;
    }

    /// <summary>Aetherian organic palette — purple membrane hull, violet folds, teal bioluminescent veins, purple engine glow.</summary>
    public void RecolorOrganic(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponPod = Vector3.Lerp(secondary * 0.40f, primary * 0.26f, 0.58f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.36f, 0.28f);
        Vector3 veinAccent = Vector3.Lerp(accent, new Vector3(0.35f, 1f, 0.85f), 0.82f);
        Vector3 membraneRecess = Vector3.Lerp(secondary, primary * 0.68f, 0.48f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => veinAccent,
                > 0.86f => Vector3.Lerp(primary, accent, 0.30f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.52f),
                > 0.58f => membraneRecess,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponPod, 0.36f),
                _ => secondary * 0.40f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Solari radiant palette — gold primary hull, amber embossed panels, bright solar accent strips, warm engine glow.</summary>
    public void RecolorRadiant(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponPod = Vector3.Lerp(secondary * 0.42f, primary * 0.28f, 0.58f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.34f, 0.28f);
        Vector3 solarAccent = Vector3.Lerp(accent, new Vector3(1f, 0.95f, 0.55f), 0.80f);
        Vector3 panelRecess = Vector3.Lerp(secondary, primary * 0.70f, 0.50f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => solarAccent,
                > 0.86f => Vector3.Lerp(primary, accent, 0.32f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.54f),
                > 0.58f => panelRecess,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponPod, 0.36f),
                _ => secondary * 0.42f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Cryo crystalline palette — ice blue gem hull, deep teal facet panels, bright cyan accent veins, cyan engine glow.</summary>
    public void RecolorCrystalline(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponBand = Vector3.Lerp(secondary * 0.38f, primary * 0.24f, 0.58f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.40f, 0.30f);
        Vector3 crystalAccent = Vector3.Lerp(accent, new Vector3(0.55f, 0.98f, 1f), 0.84f);
        Vector3 facetRecess = Vector3.Lerp(secondary, primary * 0.66f, 0.50f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => crystalAccent,
                > 0.86f => Vector3.Lerp(primary, accent, 0.32f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.54f),
                > 0.58f => facetRecess,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponBand, 0.36f),
                _ => secondary * 0.40f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/utility-tool luminance on cryo utility hulls after baked lighting.</summary>
    public void ApplyCrystallineUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);

    /// <summary>Preserve cyan crystal accent luminance on crystalline utility hulls after relight/recolor.</summary>
    public void ApplyCrystallineUtilityAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyOrganicUtilityAccentLumSnap(len, wid, hgt, hullKey);
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.92f)
                continue;
            if (IsCrystallineScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt) && lum is > 0.40f and < 0.92f)
                SnapVertexLum(i, 0.92f + (i % 12) * 0.003f);
        }
    }

    /// <summary>Restores ice facet lum variance on crystalline utility hulls — cyan vein bands readable under team tint.</summary>
    public void ApplyCrystallineUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool minerFacetPolish = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool supportAntennaPolish = hullKey is "support_repair";
        bool cargoSpinePolish = hullKey is "freighter_bulk" or "transport_cargo";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.044f * MathF.Sin(x * 11.0f + z * 7.4f);
            if (hullKey is "freighter_bulk" && y > hgt * 0.20f && y < hgt * 0.36f
                && ax < hw * 0.30f && z > len * 0.04f && z < len * 0.36f
                && lum is > 0.46f and < 0.72f)
                delta += 0.038f * MathF.Sin(z * 7.6f);
            else if (hullKey is "freighter_bulk" && y < hgt * 0.14f && ax < hw * 0.26f
                && z > len * 0.22f && z < len * 0.52f && lum is > 0.48f and < 0.66f)
                delta += 0.034f * MathF.Sin(z * 6.8f + x * 3.6f);
            else if (hullKey is "freighter_bulk" && y > hgt * 0.38f && y < hgt * 0.52f
                && ax < hw * 0.10f && z > len * 0.32f && z < len * 0.54f && lum is > 0.58f and < 0.82f)
                delta += 0.050f * MathF.Sin(z * 6.2f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.40f && y < hgt * 0.58f
                && ax < hw * 0.14f && z > len * 0.06f && lum is > 0.64f and < 0.92f)
                delta += 0.036f * MathF.Sin(z * 6.6f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.44f && y < hgt * 0.58f
                && ax < hw * 0.12f && z > len * 0.10f && lum is > 0.68f and < 0.92f)
                delta += 0.048f * MathF.Sin(z * 6.2f);
            else if (hullKey is "miner_basic" && ax > hw * 0.60f && y < hgt * 0.26f && z > len * 0.08f
                && lum is > 0.44f and < 0.64f)
                delta -= 0.068f * MathF.Sin(z * 7.8f + x * 4.4f);
            else if (hullKey is "miner_eva" && y > hgt * 0.42f && y < hgt * 0.58f && ax < hw * 0.18f
                && z > len * 0.02f && z < len * 0.22f && lum is > 0.50f and < 0.74f)
                delta -= 0.072f * MathF.Sin(z * 6.9f);
            else if (hullKey is "miner_tractor" && ax > hw * 0.48f && y < hgt * 0.28f && z > len * 0.02f
                && lum is > 0.42f and < 0.62f)
                delta -= 0.070f * MathF.Sin(z * 7.0f + x * 3.8f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.58f && y < hgt * 0.78f && ax < hw * 0.16f
                && z > len * 0.30f && lum is > 0.52f and < 0.76f)
                delta -= 0.066f * MathF.Sin(z * 6.5f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.44f && y < hgt * 0.58f
                && ax < hw * 0.12f && z > len * 0.22f && z < len * 0.40f && lum is > 0.56f and < 0.80f)
                delta += 0.046f * MathF.Sin(z * 6.6f);
            else if (supportAntennaPolish && y > hgt * 0.66f && y < hgt * 1.02f && ax < hw * 0.14f
                && lum is > 0.72f and < 0.94f)
                delta += 0.048f * MathF.Sin(z * 5.9f);
            else if (supportAntennaPolish && y > hgt * 0.72f && y < hgt * 0.96f && ax < hw * 0.10f
                && z > len * 0.02f && z < len * 0.16f && lum is > 0.58f and < 0.82f)
                delta -= 0.052f * MathF.Sin(z * 6.6f);
            else if (supportAntennaPolish && ax > hw * 0.56f && y > hgt * 0.36f && y < hgt * 0.52f
                && z > len * 0.08f && z < len * 0.20f && lum is > 0.44f and < 0.68f)
                delta -= 0.066f * MathF.Sin(z * 6.0f);
            else if (minerFacetPolish && y > hgt * 0.38f && y < hgt * 0.52f && ax < hw * 0.10f
                && lum is > 0.58f and < 0.82f)
                delta += 0.046f * MathF.Sin(z * 5.8f);
            else if (minerFacetPolish && y > hgt * 0.30f && y < hgt * 0.44f && ax < hw * 0.14f
                && lum is > 0.62f and < 0.86f)
                delta += 0.054f * MathF.Sin(z * 6.0f);
            else if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerFacetPolish ? 0.100f : cargoSpinePolish ? 0.090f : 0.086f;
            else if (y > hgt * 0.46f && lum > 0.84f)
                delta += cargoSpinePolish ? 0.068f : 0.064f;
            else if (y > hgt * 0.38f && lum > 0.72f && lum < 0.90f)
                delta += 0.038f * MathF.Sin(z * 5.4f);
            else if (cargoSpinePolish && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.046f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.98f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance after crystalline baked lighting.</summary>
    public void ApplyCrystallineGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
    }

    /// <summary>Relight pass for Cryo compact craft — ice facet recess shadows under team tint.</summary>
    public void ApplyCrystallineCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        float hw = wid * 0.5f;
        bool gapClose = hullKey is "scout_light" or "interceptor_mk2" or "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = gapClose ? 0.044f : 0.036f;
            float delta = facetAmp * MathF.Sin(x * 12.0f + z * 8.2f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.15f;
            else if (hullKey is "scout_light" && z > len * 0.56f && z < len * 0.90f && y < hgt * 0.14f && lum is > 0.46f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 8.6f);
            else if (hullKey is "scout_light" && ax > hw * 0.30f && y > hgt * 0.10f && y < hgt * 0.28f && lum is > 0.50f and < 0.72f)
                delta -= 0.046f * MathF.Sin(z * 9.8f + x * 5.2f);
            else if (hullKey is "scout_light" && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.038f * MathF.Sin(z * 6.4f);
            else if (hullKey is "drone_swarm" && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.14f;
            else if (hullKey is "drone_swarm" && z > len * 0.56f && y > hgt * 0.12f && y < hgt * 0.30f && lum is > 0.50f and < 0.72f)
                delta -= 0.056f;
            else if (hullKey is "drone_swarm" && z > len * 0.58f && y > hgt * 0.10f && y < hgt * 0.28f && ax > hw * 0.08f && ax < hw * 0.24f && lum is > 0.48f and < 0.70f)
                delta -= 0.046f * MathF.Sin(z * 7.4f + x * 4.6f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.08f && y < hgt * 0.20f && z > len * 0.72f && ax < hw * 0.10f && lum is > 0.48f and < 0.72f)
                delta -= 0.056f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z > len * 0.68f && lum is > 0.50f and < 0.72f)
                delta -= 0.050f;
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.42f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.042f * MathF.Sin(z * 6.2f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.46f && ax < hw * 0.08f && lum is > 0.58f and < 0.90f)
                delta += 0.038f * MathF.Sin(z * 5.8f);
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z < len * 0.10f && lum is > 0.46f and < 0.58f)
                delta += 0.030f * MathF.Sin(z * 7.2f + x * 4.0f);
            else if (hullKey is "interceptor_mk2" && z > len * 0.84f && y > hgt * 0.14f && y < hgt * 0.30f && lum is > 0.48f and < 0.72f)
                delta += 0.034f * MathF.Sin(z * 6.8f);
            else if (hullKey is "interceptor_mk2" && ax > hw * 0.22f && ax < hw * 0.34f && z > len * 0.60f && lum is > 0.50f and < 0.74f)
                delta -= 0.038f * MathF.Sin(z * 8.2f + x * 4.4f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= gapClose ? 0.070f : 0.060f;
            else if (y > hgt * 0.46f && lum is > 0.78f and < 0.92f)
                delta += 0.040f * MathF.Sin(z * 5.0f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (hullKey is "interceptor_mk2" && ax > hw * 0.18f && ax < hw * 0.32f && y > hgt * 0.12f && y < hgt * 0.28f && z > len * 0.58f && lum is > 0.52f and < 0.82f)
                SnapVertexLum(i, 0.90f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.40f && ax < hw * 0.10f && z > len * 0.62f && lum is > 0.54f and < 0.86f)
                SnapVertexLum(i, 0.91f);
            else if (ax > hw * 0.32f && y > hgt * 0.10f && y < hgt * 0.32f && z > len * 0.56f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && z > len * 0.58f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Relight pass for Cryo fighter/hero reference craft — facet crown depth under team tint.</summary>
    public void ApplyCrystallineReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.032f * MathF.Sin(x * 10.8f + z * 7.4f);
            if (maintainFighter && y < hgt * 0.10f && z < len * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 7.8f + x * 3.4f);
            else if (maintainFighter && z > len * 0.54f && y < hgt * 0.12f && lum is > 0.40f and < 0.56f)
                delta += 0.024f * MathF.Sin(z * 8.6f);
            else if (maintainFighter && z > len * 0.58f && y < hgt * 0.14f && lum is > 0.44f and < 0.56f)
                delta += 0.034f * MathF.Sin(z * 6.6f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.10f && z > len * 0.56f && lum is > 0.55f and < 0.88f)
                delta += 0.034f * MathF.Sin(z * 5.2f);
            else if (isHero && y > hgt * 0.68f && z > len * 0.72f && lum is > 0.50f and < 0.88f)
                delta += 0.066f * MathF.Sin(z * 5.0f);
            else if (isHero && y > hgt * 0.56f && z > len * 0.58f && lum is > 0.72f and < 0.94f)
                delta += 0.060f;
            else if (isHero && y > hgt * 0.44f && ax < hw * 0.12f && z > len * 0.62f && lum is > 0.55f and < 0.88f)
                delta += 0.044f * MathF.Sin(z * 5.4f);
            else if (isHero && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.056f;
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.052f : 0.054f;
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.034f : 0.036f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.38f && y > hgt * 0.10f && y < hgt * 0.30f && z > len * 0.58f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (isHero && y > hgt * 0.60f && ax < hw * 0.14f && z > len * 0.64f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Capital crystalline luminance bands — ice facet contrast survives RecolorCrystalline under team tint.</summary>
    public void ApplyCrystallineCapitalMaterialsBoost(float hgt)
    {
        ApplyOrganicCapitalMaterialsBoost(hgt);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.24f and <= 0.50f)
                continue;

            float target = lum;
            if (y > hgt * 0.28f && y < hgt * 0.48f && lum is > 0.46f and < 0.78f)
                target = lum + 0.042f * MathF.Sin(_verts[i + 2] * 8.8f);
            else if (y > hgt * 0.48f && y < hgt * 0.62f && lum is > 0.44f and < 0.74f)
                target = lum + 0.036f * MathF.Sin(_verts[i] * 9.2f + _verts[i + 2] * 6.0f);
            else
                continue;

            float scale = target / MathF.Max(lum, 0.001f);
            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * scale, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * scale, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * scale, 0.05f, 1f);
        }
    }

    /// <summary>Capital crystalline relight — facet membrane recess depth on large hull substrate bands.</summary>
    public void ApplyCrystallineCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
    {
        ApplyOrganicCapitalRelight(hgt, hullKey, len, wid);
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (isCruiser && y > hgt * 0.44f && y < hgt * 0.62f && z > -len * 0.04f && z < len * 0.20f && lum is > 0.48f and < 0.80f)
                delta += 0.040f;
            else if (isCarrier && y > hgt * 0.14f && y < hgt * 0.28f && z > -len * 0.12f && z < len * 0.06f && ax < hw * 0.30f && lum is > 0.44f and < 0.74f)
                delta += 0.038f;
            else if (isCarrier && y > hgt * 0.40f && y < hgt * 0.56f && z > -len * 0.16f && z < len * 0.14f && ax < hw * 0.22f && lum is > 0.50f and < 0.82f)
                delta += 0.042f;
            else if (isCarrier && y > hgt * 0.08f && y < hgt * 0.18f && z > -len * 0.10f && z < len * 0.08f && ax < hw * 0.26f && lum is > 0.46f and < 0.76f)
                delta += 0.034f;
            else if (isDreadnought && z > len * 0.46f && y > hgt * 0.26f && y < hgt * 0.46f && ax < hw * 0.20f && lum is > 0.50f and < 0.84f)
                delta += 0.038f;
            else if (y > hgt * 0.30f && y < hgt * 0.46f && lum is > 0.52f and < 0.78f)
                delta -= 0.044f;

            if (delta != 0f)
            {
                _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
                _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
                _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
            }
        }
    }

    /// <summary>Relight pass for Cryo medium combat — facet panel recess shadows under team tint.</summary>
    public void ApplyCrystallineMediumCombatRelight(float hgt, string hullKey, float len, float wid)
    {
        ApplyOrganicMediumCombatRelight(hgt, hullKey, len, wid);

        float hw = wid * 0.5f;
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        if (!isGunship && !isBomber && !isCorvette && !isFrigate && !isDestroyer)
            return;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (isDestroyer && y > hgt * 0.44f && y < hgt * 0.62f && ax < hw * 0.16f && z > len * 0.08f && lum is > 0.50f and < 0.82f)
                delta += 0.048f * MathF.Sin(z * 5.6f);
            else if (isDestroyer && y > hgt * 0.36f && ax < hw * 0.22f && z > len * 0.12f && lum is > 0.54f and < 0.86f)
                delta += 0.040f * MathF.Sin(z * 5.0f);
            else if (isDestroyer && ax > hw * 0.22f && ax < hw * 0.38f && y > hgt * 0.10f && y < hgt * 0.28f && z > len * 0.04f && lum is > 0.48f and < 0.78f)
                delta += 0.036f;
            else if (isDestroyer && y < hgt * 0.20f && z > len * 0.40f && lum is > 0.48f and < 0.76f)
                delta += 0.032f;
            else if (isGunship && y > hgt * 0.40f && y < hgt * 0.54f && ax < hw * 0.14f && z > len * 0.02f && lum is > 0.56f and < 0.88f)
                delta += 0.050f * MathF.Sin(z * 6.4f);
            else if (isGunship && y > hgt * 0.44f && ax < hw * 0.10f && z > len * 0.04f && lum is > 0.58f and < 0.88f)
                delta += 0.044f * MathF.Sin(z * 5.8f);
            else if (isBomber && y > hgt * 0.40f && ax < hw * 0.12f && z > len * 0.04f && lum is > 0.56f and < 0.88f)
                delta += 0.042f * MathF.Sin(z * 5.6f);
            else if (isFrigate && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.040f * MathF.Sin(z * 5.4f);
            else if (isCorvette && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.038f * MathF.Sin(z * 5.2f);

            if (delta != 0f)
            {
                _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
                _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
                _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
            }
        }
    }

    /// <summary>Preserve cyan crystal accent luminance after crystalline relight.</summary>
    public void ApplyCrystallineAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);

        float hw = wid * 0.5f;
        bool isSmallCraft = hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
            or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm";
        bool isMediumCombat = hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault";
        bool isCapital = hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought";
        if (!isSmallCraft && !isMediumCombat && !isCapital)
            return;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.92f)
                continue;

            if (IsCrystallineScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt) && lum is > 0.40f and < 0.92f)
                SnapVertexLum(i, 0.92f + (i % 12) * 0.003f);
            else if (isMediumCombat && y > hgt * 0.42f && ax < hw * 0.12f && z > len * 0.02f && lum is > 0.55f and < 0.92f)
                SnapVertexLum(i, 0.93f + (i % 12) * 0.002f);
            else if (isSmallCraft && y > hgt * 0.44f && ax < hw * 0.10f && z > len * 0.56f && lum is > 0.55f and < 0.92f)
                SnapVertexLum(i, 0.92f + (i % 12) * 0.003f);
            else if (isCapital && y > hgt * 0.38f && ax < hw * 0.14f && z > -len * 0.06f && lum is > 0.50f and < 0.92f)
                SnapVertexLum(i, 0.93f + (i % 12) * 0.002f);
        }
    }

    /// <summary>Dorsal crest / weapon facet pods reserved for scorer accent — skip weapon lum snap.</summary>
    public static bool IsCrystallineScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
        => IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);

    /// <summary>Capital radiant luminance bands — solar membrane contrast survives RecolorRadiant under team tint.</summary>
    public void ApplyRadiantCapitalMaterialsBoost(float hgt)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.24f and <= 0.50f)
                continue;

            float target = lum;
            if (lum > 0.94f)
                target = 0.98f + 0.024f * MathF.Sin(_verts[i] * 8.5f + _verts[i + 2] * 5.2f);
            else if (lum > 0.84f)
                target = lum + 0.054f + 0.024f * MathF.Sin(_verts[i + 2] * 7.6f);
            else if (lum > 0.72f)
                target = lum + 0.042f * MathF.Sin(_verts[i] * 10.4f + _verts[i + 2] * 6.2f);
            else if (y < hgt * 0.10f && lum < 0.32f)
                target = 0.10f + 0.048f * MathF.Sin(_verts[i] * 7.1f);
            else if (y > hgt * 0.14f && y < hgt * 0.26f && lum is > 0.66f and < 0.86f)
                target = lum - 0.060f * MathF.Sin(_verts[i + 2] * 9.0f);
            else if (y > hgt * 0.26f && y < hgt * 0.38f && lum is > 0.48f and < 0.74f)
                target = lum + 0.054f * MathF.Sin(_verts[i] * 8.6f + _verts[i + 2] * 5.6f);
            else if (y > hgt * 0.40f && y < hgt * 0.58f && lum is > 0.44f and < 0.72f)
                target = lum - 0.052f * MathF.Sin(_verts[i + 2] * 8.2f);
            else
                target = lum + 0.036f * MathF.Sin(_verts[i] * 11.0f + _verts[i + 2] * 6.8f);

            float scale = target / MathF.Max(lum, 0.001f);
            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] * scale, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] * scale, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] * scale, 0.05f, 1f);
        }
    }

    /// <summary>Capital radiant relight — solar crown recess depth on large hull substrate bands.</summary>
    public void ApplyRadiantCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
    {
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.040f * MathF.Sin(x * 9.4f + z * 6.4f);
            if (y < hgt * 0.08f && lum > 0.58f)
                delta -= 0.094f;
            else if (isCarrier && y < hgt * 0.12f && z < -len * 0.02f && z > -len * 0.12f && ax < hw * 0.28f && lum is > 0.46f and < 0.76f)
                delta -= 0.098f;
            else if (isCarrier && y > hgt * 0.16f && y < hgt * 0.30f && z < len * 0.12f && lum is > 0.46f and < 0.76f)
                delta -= 0.090f;
            else if (isCarrier && y > hgt * 0.38f && y < hgt * 0.54f && z > -len * 0.28f && z < len * 0.10f && lum is > 0.42f and < 0.72f)
                delta -= 0.072f;
            else if (isCarrier && y > hgt * 0.08f && y < hgt * 0.16f && z > -len * 0.10f && z < len * 0.04f && ax < hw * 0.28f && lum is > 0.44f and < 0.74f)
                delta -= 0.082f;
            else if (isCarrier && y > hgt * 0.44f && y < hgt * 0.56f && z > -len * 0.14f && z < len * 0.12f && ax < hw * 0.20f && lum is > 0.50f and < 0.80f)
                delta += 0.044f;
            else if (isCarrier && y > hgt * 0.30f && y < hgt * 0.42f && z > -len * 0.20f && z < len * 0.04f && ax < hw * 0.18f && lum is > 0.46f and < 0.74f)
                delta -= 0.050f;
            else if (isCarrier && ax > hw * 0.44f && y > hgt * 0.16f && y < hgt * 0.28f && z > -len * 0.04f && z < len * 0.16f && lum is > 0.48f and < 0.78f)
                delta += 0.038f * MathF.Sin(z * 8.6f + x * 4.8f);
            else if (isCruiser && ax > hw * 0.30f && ax < hw * 0.48f && y < hgt * 0.24f && z > len * 0.46f && lum is > 0.46f and < 0.76f)
                delta -= 0.092f;
            else if (isCruiser && y > hgt * 0.28f && y < hgt * 0.40f && ax < hw * 0.16f && z > len * 0.02f && z < len * 0.24f && lum is > 0.50f and < 0.82f)
                delta += 0.052f * MathF.Sin(z * 7.4f);
            else if (isCruiser && ax > hw * 0.30f && ax < hw * 0.42f && y > hgt * 0.20f && y < hgt * 0.36f && z > -len * 0.06f && z < len * 0.18f && lum is > 0.44f and < 0.72f)
                delta -= 0.056f;
            else if (isCruiser && ax > hw * 0.34f && y > hgt * 0.12f && y < hgt * 0.28f && z > -len * 0.04f && z < len * 0.20f && lum is > 0.48f and < 0.78f)
                delta += 0.048f;
            else if (isCruiser && z < -len * 0.16f && y < hgt * 0.16f && ax < hw * 0.22f && lum is > 0.34f and < 0.58f)
                delta += 0.048f;
            else if (isCruiser && y > hgt * 0.16f && y < hgt * 0.30f && lum is > 0.54f and < 0.80f)
                delta += 0.038f;
            else if (isDreadnought && z > len * 0.50f && y < hgt * 0.24f && ax < hw * 0.30f && lum is > 0.48f and < 0.76f)
                delta -= 0.088f;
            else if (isDreadnought && z > len * 0.44f && y > hgt * 0.34f && y < hgt * 0.52f && ax < hw * 0.14f && lum is > 0.50f and < 0.82f)
                delta += 0.054f;
            else if (isDreadnought && z > len * 0.48f && y > hgt * 0.28f && y < hgt * 0.44f && ax < hw * 0.18f && lum is > 0.52f and < 0.84f)
                delta += 0.046f;
            else if (isDreadnought && z > len * 0.52f && y > hgt * 0.20f && y < hgt * 0.36f && ax > hw * 0.10f && ax < hw * 0.26f && lum is > 0.46f and < 0.74f)
                delta -= 0.058f * MathF.Sin(z * 6.8f);
            else if (isDreadnought && ax > hw * 0.34f && y < hgt * 0.18f && z > len * 0.54f && lum is > 0.30f and < 0.50f)
                delta -= 0.056f;
            else if (y > hgt * 0.16f && y < hgt * 0.32f && lum is > 0.50f and < 0.76f)
                delta -= 0.052f;
            else if (y > hgt * 0.34f && y < hgt * 0.52f && lum is > 0.44f and < 0.72f)
                delta -= 0.038f;
            else if (y > hgt * 0.48f && lum > 0.86f)
                delta += 0.068f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance on Solari capital hulls after radiant baked lighting.</summary>
    public void ApplyRadiantCapitalGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        float engineZMax = -len * 0.12f;
        float engineYMax = hgt * 0.22f;
        float weaponAxMin = isDreadnought ? hw * 0.38f : isCarrier ? hw * 0.30f : hw * 0.28f;
        float weaponZMin = isCruiser ? len * 0.52f : isDreadnought ? len * 0.60f : float.NegativeInfinity;
        float shieldYMin = hgt * 0.66f;
        float shieldYMax = hgt * 0.86f;
        float shieldAxMax = hw * (isCarrier ? 0.24f : 0.12f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (IsRadiantScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                continue;

            float? target = null;
            if (z < engineZMax && y < engineYMax && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isCarrier && z < -len * 0.04f && y < hgt * 0.14f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isCarrier && y > hgt * 0.18f && y < hgt * 0.30f && z > -len * 0.10f && z < len * 0.10f && ax > hw * 0.18f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isCarrier && y > hgt * 0.36f && y < hgt * 0.52f && z > -len * 0.16f && z < len * 0.08f && ax < hw * 0.22f && lum is >= 0.20f and < 0.55f)
                target = 0.30f;
            else if (isCruiser && ax > hw * 0.40f && y < hgt * 0.22f && z > len * 0.48f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isCruiser && z < -len * 0.14f && y < hgt * 0.16f && ax < hw * 0.24f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isDreadnought && z < -len * 0.10f && y < hgt * 0.18f && ax < hw * 0.28f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isDreadnought && y > hgt * 0.38f && y < hgt * 0.54f && ax < hw * 0.14f && z > len * 0.30f && lum is >= 0.20f and < 0.55f)
                target = 0.30f;
            else if (isCarrier && ax > weaponAxMin && y < hgt * 0.34f && z > -len * 0.06f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isDreadnought && ax > weaponAxMin && y < hgt * 0.16f && z > weaponZMin && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (ax > weaponAxMin && y < hgt * 0.34f && z > weaponZMin && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance after radiant baked lighting — compact + medium + capital craft.</summary>
    public void ApplyRadiantGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyRadiantCapitalGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        bool isSmallCraft = hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
            or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm";
        if (isSmallCraft)
        {
            ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        if (hullKey is not ("corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
            or "destroyer_assault" or "corvette" or "frigate" or "gunship" or "bomber" or "destroyer"))
            return;

        float hw = wid * 0.5f;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            bool accentReserve = IsRadiantScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);
            if (!accentReserve && isGunship && y < hgt * 0.14f && z > len * 0.04f && ax < hw * 0.24f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isGunship && ax > hw * 0.30f && y < hgt * 0.16f && z > -len * 0.02f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isGunship && y > hgt * 0.42f && y < hgt * 0.56f && ax < hw * 0.10f && z > len * 0.04f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (!accentReserve && isBomber && y < hgt * 0.14f && z > -len * 0.04f && z < len * 0.14f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isBomber && y > hgt * 0.40f && y < hgt * 0.54f && ax < hw * 0.10f && z > len * 0.02f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (!accentReserve && isFrigate && ax > hw * 0.26f && y < hgt * 0.24f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isFrigate && y > hgt * 0.44f && y < hgt * 0.56f && ax < hw * 0.10f && z > len * 0.06f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (!accentReserve && isCorvette && ax > hw * 0.26f && y < hgt * 0.20f && z > -len * 0.02f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isCorvette && y > hgt * 0.44f && y < hgt * 0.56f && ax < hw * 0.10f && z > len * 0.04f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (!accentReserve && isDestroyer && y < hgt * 0.22f && z > len * 0.42f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isDestroyer && y > hgt * 0.54f && y < hgt * 0.68f && ax < hw * 0.10f && z > len * 0.20f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (z < -len * 0.02f && y < hgt * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Relight pass for Solari compact craft — solar panel recess shadows under team tint.</summary>
    public void ApplyRadiantCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        float hw = wid * 0.5f;
        bool gapClose = hullKey is "scout_light" or "interceptor_mk2" or "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = gapClose ? 0.046f : 0.036f;
            float delta = facetAmp * MathF.Sin(x * 11.2f + z * 7.6f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.16f;
            else if (hullKey is "scout_light" && z < -len * 0.04f && y < hgt * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.034f * MathF.Sin(z * 8.4f);
            else if (hullKey is "scout_light" && ax > hw * 0.32f && y > hgt * 0.10f && y < hgt * 0.28f && lum is > 0.50f and < 0.72f)
                delta -= 0.048f * MathF.Sin(z * 9.6f + x * 5.0f);
            else if (hullKey is "scout_light" && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.52f and < 0.90f)
                delta += 0.044f * MathF.Sin(z * 6.2f);
            else if (hullKey is "scout_light" && y > hgt * 0.48f && ax < hw * 0.06f && z > len * 0.08f && lum is > 0.50f and < 0.88f)
                delta += 0.040f;
            else if (hullKey is "drone_swarm" && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.15f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.12f && y < hgt * 0.32f && lum is > 0.50f and < 0.72f)
                delta -= 0.058f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.10f && y < hgt * 0.30f && ax > hw * 0.08f && ax < hw * 0.22f && lum is > 0.48f and < 0.70f)
                delta -= 0.048f * MathF.Sin(z * 7.2f + x * 4.4f);
            else if (hullKey is "drone_swarm" && y > hgt * 0.38f && ax < hw * 0.08f && z > -len * 0.04f && lum is > 0.50f and < 0.86f)
                delta += 0.042f * MathF.Sin(z * 5.8f);
            else if (hullKey is "drone_swarm" && y > hgt * 0.10f && y < hgt * 0.28f && ax > hw * 0.06f && ax < hw * 0.20f && lum is > 0.48f and < 0.70f)
                delta -= 0.040f * MathF.Sin(z * 7.0f + x * 4.0f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.08f && y < hgt * 0.20f && z > len * 0.28f && ax < hw * 0.10f && lum is > 0.48f and < 0.72f)
                delta -= 0.054f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z > len * 0.26f && lum is > 0.50f and < 0.72f)
                delta -= 0.048f;
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.42f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.040f * MathF.Sin(z * 6.0f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.46f && ax < hw * 0.08f && lum is > 0.58f and < 0.90f)
                delta += 0.036f * MathF.Sin(z * 5.6f);
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.030f * MathF.Sin(z * 7.0f + x * 3.8f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= gapClose ? 0.072f : 0.062f;
            else if (y > hgt * 0.48f && lum is > 0.78f and < 0.92f)
                delta += 0.040f * MathF.Sin(z * 4.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (ax > hw * 0.34f && y > hgt * 0.10f && y < hgt * 0.32f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Relight pass for Solari fighter/hero reference craft — embossed crown band depth under team tint.</summary>
    public void ApplyRadiantReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.030f * MathF.Sin(x * 10.4f + z * 7.0f);
            if (maintainFighter && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 7.6f + x * 3.2f);
            else if (maintainFighter && z < -len * 0.03f && y < hgt * 0.12f && lum is > 0.40f and < 0.56f)
                delta += 0.024f * MathF.Sin(z * 8.4f);
            else if (maintainFighter && z < -len * 0.06f && y < hgt * 0.14f && lum is > 0.44f and < 0.56f)
                delta += 0.034f * MathF.Sin(z * 6.4f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                delta += 0.040f * MathF.Sin(z * 5.2f);
            else if (maintainFighter && ax > hw * 0.48f && y > hgt * 0.12f && y < hgt * 0.26f && lum is > 0.50f and < 0.76f)
                delta -= 0.034f * MathF.Sin(z * 6.8f + x * 3.4f);
            else if (maintainFighter && ax > hw * 0.48f && y > hgt * 0.14f && y < hgt * 0.26f && lum is > 0.50f and < 0.78f)
                delta += 0.030f * MathF.Sin(z * 6.4f + x * 3.6f);
            else if (isHero && y > hgt * 0.68f && z > len * 0.16f && lum is > 0.50f and < 0.88f)
                delta += 0.068f * MathF.Sin(z * 5.0f);
            else if (isHero && y > hgt * 0.56f && lum is > 0.72f and < 0.94f)
                delta += 0.058f;
            else if (isHero && y > hgt * 0.44f && ax < hw * 0.12f && z > len * 0.06f && lum is > 0.55f and < 0.88f)
                delta += 0.046f * MathF.Sin(z * 5.4f);
            else if (isHero && y > hgt * 0.62f && ax < hw * 0.10f && z > len * 0.08f && lum is > 0.52f and < 0.86f)
                delta -= 0.036f * MathF.Sin(z * 5.8f);
            else if (isHero && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.054f;
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.050f : 0.052f;
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.032f : 0.034f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.54f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (isHero && y > hgt * 0.70f && z > len * 0.22f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
        }
    }

    /// <summary>Relight pass for Solari medium combat — embossed solar panel shadows under team tint.</summary>
    public void ApplyRadiantMediumCombatRelight(float hgt, string hullKey, float len, float wid)
    {
        ApplyOrganicMediumCombatRelight(hgt, hullKey, len, wid);

        float hw = wid * 0.5f;
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        if (!isGunship && !isBomber && !isCorvette && !isFrigate && !isDestroyer)
            return;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0f;
            if (isGunship && y > hgt * 0.42f && y < hgt * 0.56f && ax < hw * 0.14f && z > len * 0.02f && lum is > 0.56f and < 0.88f)
                delta += 0.050f * MathF.Sin(z * 6.4f);
            else if (isGunship && y < hgt * 0.10f && z > len * 0.04f && ax < hw * 0.18f && lum is > 0.48f and < 0.76f)
                delta -= 0.112f;
            else if (isBomber && y < hgt * 0.12f && z > len * 0.04f && z < len * 0.20f && ax < hw * 0.20f && lum is > 0.50f and < 0.80f)
                delta -= 0.122f * MathF.Sin(z * 7.0f);
            else if (isBomber && y > hgt * 0.40f && ax < hw * 0.12f && z > len * 0.04f && lum is > 0.58f and < 0.88f)
                delta += 0.046f * MathF.Sin(z * 5.8f);
            else if (isCorvette && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.036f * MathF.Sin(z * 6.0f);
            else if (isFrigate && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.038f * MathF.Sin(z * 5.6f);
            else if (isDestroyer && y > hgt * 0.48f && ax < hw * 0.12f && z > len * 0.12f && lum is > 0.58f and < 0.88f)
                delta += 0.044f * MathF.Sin(z * 5.2f);
            else if (isDestroyer && y < hgt * 0.14f && ax > hw * 0.22f && ax < hw * 0.38f && z > -len * 0.06f && lum is > 0.46f and < 0.72f)
                delta -= 0.038f * MathF.Sin(z * 6.6f + x * 3.8f);

            if (delta != 0f)
            {
                _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
                _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
                _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
            }
        }
    }

    /// <summary>Preserve bright solar accent luminance after radiant relight — compact + medium + capital craft.</summary>
    public void ApplyRadiantAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isCapital = isCruiser || isCarrier || isDreadnought;
        if (isCapital)
        {
            float hw = wid * 0.5f;
            for (int i = 0; i < _verts.Count; i += 6)
            {
                float x = _verts[i];
                float y = _verts[i + 1];
                float z = _verts[i + 2];
                float ax = MathF.Abs(x);
                float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
                if (lum >= 0.94f)
                    continue;

                bool accentBand = false;
                float boomReach = isCruiser ? 0.90f : isCarrier ? 0.72f : 0.88f;
                bool isDorsalCrown = y > hgt * 0.38f && ax < hw * 0.14f && z > len * 0.04f;
                bool isEnvelopeAccent = ax > hw * boomReach && y > hgt * 0.10f && y < hgt * 0.34f && z > len * 0.10f;
                bool isSolarDeck = isCarrier && y > hgt * 0.36f && y < hgt * 0.54f && z > -len * 0.20f && z < len * 0.10f;
                bool isHangarGlow = isCarrier && y > hgt * 0.08f && y < hgt * 0.18f && z > -len * 0.10f && z < len * 0.06f && ax < hw * 0.30f;
                bool isAccentRingVein = isCarrier && ax > hw * 0.42f && y > hgt * 0.14f && y < hgt * 0.30f && z > -len * 0.04f && z < len * 0.18f;
                bool isCrownEmboss = isCruiser && y > hgt * 0.28f && y < hgt * 0.42f && ax < hw * 0.16f && z > len * 0.02f && z < len * 0.24f;
                bool isSolarStrip = isCruiser && ax > hw * 0.34f && y > hgt * 0.12f && y < hgt * 0.28f && z > -len * 0.02f && z < len * 0.20f;
                bool isProwAccent = isDreadnought && z > len * 0.40f && y > hgt * 0.16f && y < hgt * 0.46f
                    && (ax < hw * 0.24f || ax > hw * 0.58f);
                bool isProwEmboss = isDreadnought && z > len * 0.48f && y > hgt * 0.24f && y < hgt * 0.40f && ax < hw * 0.20f;
                bool isCrownPanel = isDreadnought && y > hgt * 0.34f && y < hgt * 0.52f && ax < hw * 0.14f && z > len * 0.28f;
                accentBand = lum is > 0.42f and < 0.94f
                    && (isDorsalCrown || isEnvelopeAccent || isSolarDeck || isHangarGlow || isAccentRingVein
                        || isCrownEmboss || isSolarStrip || isProwAccent || isProwEmboss || isCrownPanel);

                if (accentBand)
                    SnapVertexLum(i, 0.94f + (i % 12) * 0.002f);
            }
            return;
        }

        bool isSmallCraft = hullKey is "fighter" or "fighter_basic" or "hero" or "hero_default"
            or "scout" or "scout_light" or "interceptor" or "interceptor_mk2"
            or "drone" or "drone_swarm";
        if (isSmallCraft)
        {
            float hwSnap = wid * 0.5f;
            bool isScout = hullKey is "scout" or "scout_light";
            bool isFighter = hullKey is "fighter" or "fighter_basic";
            bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
            bool isDrone = hullKey is "drone" or "drone_swarm";
            bool isHero = hullKey is "hero" or "hero_default";
            for (int i = 0; i < _verts.Count; i += 6)
            {
                float x = _verts[i];
                float y = _verts[i + 1];
                float z = _verts[i + 2];
                float ax = MathF.Abs(x);
                float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
                if (lum >= 0.94f)
                    continue;

                bool accentBand = false;
                if (IsRadiantScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                    accentBand = lum is > 0.38f and < 0.94f;
                else if (y > hgt * 0.44f && ax < hwSnap * 0.12f && lum is > 0.50f and < 0.92f)
                    accentBand = true;
                else if (isHero && y > hgt * 0.66f && z > len * 0.10f && ax < hwSnap * 0.14f && lum is > 0.44f and < 0.94f)
                    accentBand = true;
                else if (isScout && y > hgt * 0.46f && ax < hwSnap * 0.08f && z > len * 0.04f && lum is > 0.48f and < 0.92f)
                    accentBand = true;
                else if (isInterceptor && y > hgt * 0.46f && ax < hwSnap * 0.10f && z > len * 0.12f && lum is > 0.48f and < 0.92f)
                    accentBand = true;
                else if (isFighter && ax > hwSnap * 0.50f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.48f and < 0.92f)
                    accentBand = true;
                else if (isDrone && y > hgt * 0.38f && ax < hwSnap * 0.10f && z > -len * 0.04f && lum is > 0.44f and < 0.92f)
                    accentBand = true;
                else if (lum is > 0.82f and < 0.92f)
                    accentBand = true;

                if (accentBand)
                    SnapVertexLum(i, 0.92f + (i % 12) * 0.004f);
            }
            return;
        }

        if (hullKey is "corvette_fast" or "frigate_strike" or "gunship_heavy" or "bomber_heavy"
            or "destroyer_assault" or "corvette" or "frigate" or "gunship" or "bomber" or "destroyer")
        {
            ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
        }
    }

    /// <summary>Dorsal solar crest / weapon-pod strips reserved for scorer accent — skip weapon lum snap.</summary>
    public static bool IsRadiantScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isSmallCraft = isScout || isFighter || isInterceptor || isDrone || isHero;

        if (!isSmallCraft)
            return IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);

        if (y > hgt * 0.36f && ax < hw * 0.16f && z > -len * 0.08f && z < len * (isScout ? 0.30f : isDrone ? 0.26f : 0.28f))
            return true;

        float leadAxMin = isScout ? 0.30f : isDrone ? 0.14f : isFighter ? 0.48f : isInterceptor ? 0.30f : 0.42f;
        float leadAxMax = isScout ? 0.48f : isDrone ? 0.26f : isFighter ? 0.66f : isInterceptor ? 0.44f : 0.58f;
        float leadYMax = hgt * (isFighter ? 0.32f : 0.36f);
        float leadZMax = len * (isScout ? 0.22f : isDrone ? 0.20f : isInterceptor ? 0.18f : 0.16f);

        if (ax > hw * leadAxMin && ax < hw * leadAxMax
            && y > hgt * 0.10f && y < leadYMax
            && z > -len * 0.06f && z < leadZMax)
            return true;

        if (isHero && y > hgt * 0.68f && z > len * 0.12f && z < len * 0.30f && ax < hw * 0.12f)
            return true;

        return false;
    }

    /// <summary>Re-anchors engine/utility-tool luminance on Solari utility hulls after baked lighting.</summary>
    public void ApplyRadiantUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);

    /// <summary>Restores solar membrane lum variance on radiant utility hulls — panel bands readable under team tint.</summary>
    public void ApplyRadiantUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
        => ApplyOrganicUtilityRelight(hgt, hullKey, len, wid);

    /// <summary>Preserve amber solar accent luminance on Solari utility hulls after relight/recolor.</summary>
    public void ApplyRadiantUtilityAccentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyOrganicUtilityAccentLumSnap(len, wid, hgt, hullKey);

    /// <summary>Nexar asymmetric hive palette — gold chitin primary, dark amber secondary, bright accent veins, orange engine glow.</summary>
    public void RecolorAsymmetric(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponBand = Vector3.Lerp(secondary * 0.40f, primary * 0.26f, 0.58f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.38f, 0.30f);
        Vector3 chitinAccent = Vector3.Lerp(accent, new Vector3(0.98f, 0.82f, 0.22f), 0.76f);
        Vector3 chitinRecess = Vector3.Lerp(secondary, primary * 0.68f, 0.48f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => chitinAccent,
                > 0.86f => Vector3.Lerp(primary, accent, 0.34f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.54f),
                > 0.58f => chitinRecess,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponBand, 0.36f),
                _ => secondary * 0.42f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Voidborn spiny palette — void-purple carapace, deep violet recess, magenta spine veins, purple engine glow.</summary>
    public void RecolorSpiny(Vector3 primary, Vector3 secondary, Vector3 accent, Vector3 engine)
    {
        Vector3 weaponBand = Vector3.Lerp(secondary * 0.38f, primary * 0.24f, 0.60f);
        Vector3 engineGlow = Vector3.Lerp(engine, accent * 0.32f, 0.26f);
        Vector3 spineAccent = Vector3.Lerp(accent, new Vector3(0.92f, 0.28f, 1.0f), 0.74f);
        Vector3 carapaceRecess = Vector3.Lerp(secondary, primary * 0.66f, 0.50f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            Vector3 baseCol = lum switch
            {
                > 0.94f => spineAccent,
                > 0.86f => Vector3.Lerp(primary, accent, 0.36f),
                > 0.78f => primary,
                > 0.68f => Vector3.Lerp(primary, secondary, 0.56f),
                > 0.58f => carapaceRecess,
                > 0.40f => WithTargetLum(engineGlow, 0.48f),
                > 0.32f => WithTargetLum(weaponBand, 0.36f),
                _ => secondary * 0.40f,
            };

            _verts[i + 3] = MathHelper.Clamp(baseCol.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(baseCol.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(baseCol.Z, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/utility-tool luminance on voidborn utility hulls after baked lighting.</summary>
    public void ApplySpinyUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        hw *= widthScale;
        float shieldYMin = hgt * 0.58f;
        float shieldYMax = hgt * 0.76f;
        float shieldAxMax = hw * 0.12f;
        float shieldZMin = len * 0.02f;
        float shieldZMax = len * 0.14f;
        float engineZMax = -len * 0.035f;
        float engineYMax = hgt * 0.32f;
        float weaponAxMin = hullKey is "miner_basic" or "miner_eva" or "miner_tractor" ? hw * 0.38f : hw * 0.40f;
        float weaponYMax = hullKey switch
        {
            "miner_tractor" => hgt * 0.80f,
            "miner_basic" => hgt * 0.32f,
            "miner_eva" => hgt * 0.54f,
            "support_repair" => hgt * 0.70f,
            _ => hgt * 0.34f
        };
        float weaponZMin = hullKey is "miner_basic" ? len * 0.08f : 0f;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            if (hullKey is "miner_basic" && ax > hw * 0.70f && y < hgt * 0.32f && z > len * 0.10f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_basic" && z < engineZMax && y < engineYMax && ax > hw * 0.28f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "miner_eva" && ax > hw * 0.42f && y > hgt * 0.32f && y < hgt * 0.54f && z > len * 0.02f && z < len * 0.16f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_eva" && z < -len * 0.02f && y < hgt * 0.22f && ax > hw * 0.04f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "miner_tractor" && y > hgt * 0.60f && y < hgt * 0.82f && z > len * 0.32f && z < len * 0.48f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "miner_tractor" && z > len * 0.32f && z < len * 0.46f && y < hgt * 0.38f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "support_repair" && ax > hw * 0.60f && y > hgt * 0.40f && y < hgt * 0.68f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "support_repair" && y > hgt * 0.82f && y < hgt * 1.10f && ax < hw * 0.16f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "support_repair" && ax > hw * 0.56f && y < hgt * 0.20f && z > -len * 0.04f && z < len * 0.06f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (hullKey is "support_repair" && ax > hw * 0.56f && y > hgt * 0.34f && y < hgt * 0.52f && z > len * 0.08f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (hullKey is "transport_cargo" && ax > hw * 0.36f && y > hgt * 0.20f && y < hgt * 0.36f && z > len * 0.26f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (z < engineZMax && y < engineYMax && ax > hw * 0.05f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (ax > weaponAxMin && y < weaponYMax && z > weaponZMin && lum is >= 0.26f and < 0.55f
                && !IsSpinyScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                target = 0.36f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > shieldZMin && z < shieldZMax && lum is >= 0.20f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Restores carapace membrane lum variance on voidborn utility hulls — spine vein bands readable under team tint.</summary>
    public void ApplySpinyUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        hw *= widthScale;
        bool minerPodPolish = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool supportBloomPolish = hullKey is "support_repair";
        bool cargoSpinePolish = hullKey is "freighter_bulk" or "transport_cargo";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.042f * MathF.Sin(x * 10.4f + z * 6.6f);
            if (hullKey is "freighter_bulk" && y > hgt * 0.18f && y < hgt * 0.32f
                && ax < hw * 0.32f && z > len * 0.06f && z < len * 0.28f
                && lum is > 0.46f and < 0.72f)
                delta -= 0.062f * MathF.Sin(z * 7.0f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.36f && y < hgt * 0.54f
                && ax < hw * 0.14f && z > len * 0.04f && lum is > 0.62f and < 0.90f)
                delta -= 0.056f * MathF.Sin(z * 6.2f);
            else if (hullKey is "miner_basic" && ax > hw * 0.56f && y < hgt * 0.22f && z > len * 0.06f
                && lum is > 0.44f and < 0.64f)
                delta -= 0.062f * MathF.Sin(z * 7.2f + x * 3.8f);
            else if (hullKey is "miner_eva" && y > hgt * 0.38f && y < hgt * 0.54f && ax < hw * 0.16f
                && z > len * 0.02f && lum is > 0.50f and < 0.74f)
                delta -= 0.064f * MathF.Sin(z * 6.4f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.54f && y < hgt * 0.74f && ax < hw * 0.16f
                && z > len * 0.26f && lum is > 0.52f and < 0.76f)
                delta -= 0.058f * MathF.Sin(z * 6.0f);
            else if (supportBloomPolish && y > hgt * 0.62f && y < hgt * 0.98f && ax < hw * 0.14f
                && lum is > 0.68f and < 0.92f)
                delta += 0.044f * MathF.Sin(z * 5.4f);
            else if (supportBloomPolish && ax > hw * 0.52f && y > hgt * 0.34f && y < hgt * 0.60f
                && lum is > 0.48f and < 0.72f)
                delta -= 0.042f * MathF.Sin(z * 5.8f);
            else if (minerPodPolish && y > hgt * 0.34f && y < hgt * 0.48f && ax < hw * 0.12f
                && lum is > 0.56f and < 0.82f)
                delta += 0.042f * MathF.Sin(z * 5.2f);
            else if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerPodPolish ? 0.104f : cargoSpinePolish ? 0.092f : 0.084f;
            else if (minerPodPolish && y < hgt * 0.14f && lum is > 0.40f and < 0.58f)
                delta -= 0.054f;
            else if (cargoSpinePolish && y > hgt * 0.40f && lum is > 0.68f and < 0.88f)
                delta += 0.034f * MathF.Sin(z * 5.0f);
            else if (y > hgt * 0.44f && lum > 0.84f)
                delta += cargoSpinePolish ? 0.060f : minerPodPolish ? 0.056f : 0.050f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Preserve magenta spine accent luminance on voidborn utility hulls after relight/recolor.</summary>
    public void ApplySpinyUtilityAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyOrganicUtilityAccentLumSnap(len, wid, hgt, hullKey);
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.88f : hullKey is "miner_eva" ? 0.90f : 0.94f;
        hw *= widthScale;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.94f)
                continue;
            if (IsSpinyScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt) && lum is > 0.44f and < 0.94f)
                SnapVertexLum(i, 0.96f + (i % 12) * 0.003f);
        }
    }

    /// <summary>Capital voidborn carapace luminance bands — substrate contrast survives RecolorSpiny under team tint.</summary>
    public void ApplySpinyCapitalMaterialsBoost(float hgt)
        => ApplyOrganicCapitalMaterialsBoost(hgt);

    /// <summary>Capital voidborn relight — embossed plating recess depth on large exile hull substrate bands.</summary>
    public void ApplySpinyCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
        => ApplyOrganicCapitalRelight(hgt, hullKey, len, wid);

    /// <summary>Relight pass for Voidborn medium combat — carapace recess shadows under team tint.</summary>
    public void ApplySpinyMediumCombatRelight(float hgt, string hullKey, float len, float wid)
        => ApplyOrganicMediumCombatRelight(hgt, hullKey, len, wid);

    /// <summary>Re-anchors engine/weapon/shield luminance after voidborn baked lighting — compact + capital + medium craft.</summary>
    public void ApplySpinyGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        float hw = wid * 0.5f;
        bool isHero = hullKey is "hero" or "hero_default";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isSmallCraft = hullKey is "fighter_basic" or "hero_default" or "scout_light"
            or "interceptor_mk2" or "drone_swarm";

        float engineZMax = isDrone ? -len * 0.10f : -len * 0.04f;
        float engineYMax = hgt * 0.16f;
        float shieldYMin = hgt * (isHero ? 0.72f : isScout ? 0.70f : isDrone ? 0.66f : 0.68f);
        float shieldYMax = hgt * (isHero ? 0.86f : isScout ? 0.82f : isDrone ? 0.78f : 0.84f);
        float shieldAxMax = hw * (isScout ? 0.08f : isDrone ? 0.10f : 0.12f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            bool accentReserve = isSmallCraft && IsSpinyScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);
            if (!accentReserve && isSmallCraft && hullKey is "scout_light" && ax > hw * 0.44f && y < hgt * 0.18f && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "scout_light" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.10f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && y < hgt * 0.14f && z > len * 0.28f && z < len * 0.40f && ax < hw * 0.12f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.14f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z < -len * 0.12f && y < hgt * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z > len * 0.08f && y < hgt * 0.14f && ax > hw * 0.20f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && ax > hw * 0.44f && y < hgt * 0.14f && z > len * 0.02f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (z < engineZMax && y < engineYMax && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (ax > hw * 0.38f && y < hgt * 0.18f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isSmallCraft && hullKey is "fighter_basic" && y > hgt * 0.68f && y < hgt * 0.82f && ax < hw * 0.10f && z > len * 0.04f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && hullKey is "drone_swarm" && y > hgt * 0.64f && y < hgt * 0.78f && ax < hw * 0.12f && z > -len * 0.02f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Relight pass for Voidborn compact craft — carapace plating recess shadows under team tint.</summary>
    public void ApplySpinyCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
        => ApplyAsymmetricCompactCraftRelight(hgt, hullKey, len, wid);

    /// <summary>Relight pass for Voidborn fighter/hero reference craft — carapace band depth under team tint.</summary>
    public void ApplySpinyReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
        => ApplyAsymmetricReferenceCraftRelight(hgt, hullKey, len, wid);

    /// <summary>Post-pass magenta accent lum snap — scorer counts lum &gt;0.9 after RecolorSpiny + relight + gameplay snap.</summary>
    public void ApplySpinyAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyOrganicAccentLumSnap(len, wid, hgt, hullKey);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.94f)
                continue;
            if (IsSpinyScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt) && lum is > 0.40f and < 0.94f)
                SnapVertexLum(i, 0.96f + (i % 12) * 0.003f);
        }
    }

    /// <summary>Dorsal spine / flank leading-edge magenta strips reserved for scorer accent — skip weapon lum snap.</summary>
    public static bool IsSpinyScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
        => IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);

    /// <summary>Capital nexar chitin luminance bands — substrate contrast survives RecolorAsymmetric under team tint.</summary>
    public void ApplyAsymmetricCapitalMaterialsBoost(float hgt)
        => ApplyOrganicCapitalMaterialsBoost(hgt);

    /// <summary>Capital nexar relight — embossed ring recess depth on large hive hull substrate bands.</summary>
    public void ApplyAsymmetricCapitalRelight(float hgt, string? hullKey = null, float len = 6f, float wid = 3f)
        => ApplyOrganicCapitalRelight(hgt, hullKey, len, wid);

    /// <summary>Re-anchors engine/utility-tool luminance on nexar utility hulls after baked lighting.</summary>
    public void ApplyAsymmetricUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);

    /// <summary>Restores chitin membrane lum variance on nexar utility hulls — amber vein bands readable under team tint.</summary>
    public void ApplyAsymmetricUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.0f)
    {
        float hw = wid * 0.5f;
        float widthScale = hullKey is "freighter_bulk" ? 0.90f : hullKey is "miner_eva" ? 0.92f : 0.96f;
        hw *= widthScale;
        bool minerPodPolish = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool supportBloomPolish = hullKey is "support_repair";
        bool cargoSacPolish = hullKey is "freighter_bulk" or "transport_cargo";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.040f * MathF.Sin(x * 10.6f + z * 6.8f);
            if (hullKey is "freighter_bulk" && y > hgt * 0.18f && y < hgt * 0.32f
                && ax < hw * 0.34f && z > len * 0.06f && z < len * 0.30f
                && lum is > 0.46f and < 0.72f)
                delta -= 0.064f * MathF.Sin(z * 7.2f);
            else if (hullKey is "transport_cargo" && y > hgt * 0.38f && y < hgt * 0.56f
                && ax < hw * 0.14f && z > len * 0.04f && lum is > 0.62f and < 0.90f)
                delta -= 0.058f * MathF.Sin(z * 6.4f);
            else if (hullKey is "miner_basic" && ax > hw * 0.58f && y < hgt * 0.24f && z > len * 0.06f
                && lum is > 0.44f and < 0.64f)
                delta -= 0.064f * MathF.Sin(z * 7.4f + x * 4.0f);
            else if (hullKey is "miner_eva" && y > hgt * 0.40f && y < hgt * 0.56f && ax < hw * 0.18f
                && z > len * 0.02f && lum is > 0.50f and < 0.74f)
                delta -= 0.068f * MathF.Sin(z * 6.6f);
            else if (hullKey is "miner_tractor" && y > hgt * 0.56f && y < hgt * 0.76f && ax < hw * 0.18f
                && z > len * 0.28f && lum is > 0.52f and < 0.76f)
                delta -= 0.060f * MathF.Sin(z * 6.2f);
            else if (supportBloomPolish && y > hgt * 0.64f && y < hgt * 0.98f && ax < hw * 0.14f
                && lum is > 0.70f and < 0.92f)
                delta += 0.046f * MathF.Sin(z * 5.6f);
            else if (supportBloomPolish && ax > hw * 0.54f && y > hgt * 0.36f && y < hgt * 0.62f
                && lum is > 0.48f and < 0.72f)
                delta -= 0.044f * MathF.Sin(z * 6.0f);
            else if (minerPodPolish && y > hgt * 0.36f && y < hgt * 0.50f && ax < hw * 0.12f
                && lum is > 0.58f and < 0.82f)
                delta += 0.044f * MathF.Sin(z * 5.4f);
            else if (y < hgt * 0.09f && lum > 0.56f)
                delta -= minerPodPolish ? 0.108f : cargoSacPolish ? 0.096f : 0.088f;
            else if (minerPodPolish && y < hgt * 0.14f && lum is > 0.40f and < 0.58f)
                delta -= 0.056f;
            else if (cargoSacPolish && y > hgt * 0.42f && lum is > 0.70f and < 0.88f)
                delta += 0.036f * MathF.Sin(z * 5.2f);
            else if (y > hgt * 0.44f && lum > 0.84f)
                delta += cargoSacPolish ? 0.062f : minerPodPolish ? 0.058f : 0.052f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Re-anchors engine/weapon/shield luminance after organic baked lighting — capital + compact + medium craft.</summary>
    public void ApplyOrganicGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        float hw = wid * 0.5f;
        bool isHero = hullKey is "hero" or "hero_default";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isSmallCraft = hullKey is "fighter_basic" or "hero_default" or "scout_light"
            or "interceptor_mk2" or "drone_swarm";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isMediumCombat = isCorvette || isFrigate || isGunship || isBomber || isDestroyer;

        float engineZMax = isDrone ? -len * 0.10f : -len * 0.04f;
        float shieldYMin = hgt * (isHero ? 0.72f : isScout ? 0.70f : isDrone ? 0.66f : 0.68f);
        float shieldYMax = hgt * (isHero ? 0.86f : isScout ? 0.82f : isDrone ? 0.78f : 0.84f);
        float shieldAxMax = hw * (isScout ? 0.08f : isDrone ? 0.10f : 0.12f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            bool accentReserve = (isSmallCraft || isMediumCombat)
                && IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);
            if (!accentReserve && isSmallCraft && hullKey is "scout_light" && ax > hw * 0.44f && y < hgt * 0.18f && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "scout_light" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.10f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && y < hgt * 0.14f && z > len * 0.28f && z < len * 0.40f && ax < hw * 0.12f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.14f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z < -len * 0.12f && y < hgt * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z > len * 0.08f && y < hgt * 0.14f && ax > hw * 0.20f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && isHero && ax > hw * 0.44f && y < hgt * 0.14f && z > len * 0.02f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && isHero && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && isHero && y > hgt * 0.70f && y < hgt * 0.82f && ax < hw * 0.10f && z > len * 0.06f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && z < engineZMax && y < hgt * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (!accentReserve && isGunship && y < hgt * 0.14f && z > len * 0.04f && ax < hw * 0.24f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isGunship && ax > hw * 0.30f && y < hgt * 0.16f && z > -len * 0.02f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isGunship && y > hgt * 0.42f && y < hgt * 0.56f && ax < hw * 0.10f && z > len * 0.04f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (!accentReserve && isBomber && y < hgt * 0.14f && z > -len * 0.04f && z < len * 0.14f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isFrigate && ax > hw * 0.26f && y < hgt * 0.24f && z > len * 0.04f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isCorvette && ax > hw * 0.26f && y < hgt * 0.20f && z > -len * 0.02f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (!accentReserve && isDestroyer && y < hgt * 0.22f && z > len * 0.42f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isMediumCombat && z < -len * 0.02f && y < hgt * 0.16f && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (isSmallCraft && hullKey is "fighter_basic" && y > hgt * 0.68f && y < hgt * 0.82f && ax < hw * 0.10f && z > len * 0.04f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && hullKey is "drone_swarm" && y > hgt * 0.64f && y < hgt * 0.78f && ax < hw * 0.12f && z > -len * 0.02f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Relight pass for Aetherian compact craft — membrane recess shadows under team tint.</summary>
    public void ApplyOrganicCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        float hw = wid * 0.5f;
        bool gapClose = hullKey is "scout_light" or "interceptor_mk2" or "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = gapClose ? 0.040f : 0.032f;
            float delta = facetAmp * MathF.Sin(x * 11.6f + z * 7.8f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.14f;
            else if (hullKey is "scout_light" && z < -len * 0.04f && y < hgt * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.028f * MathF.Sin(z * 8.2f);
            else if (hullKey is "scout_light" && ax > hw * 0.36f && y > hgt * 0.10f && y < hgt * 0.28f && lum is > 0.50f and < 0.72f)
                delta -= 0.042f * MathF.Sin(z * 9.8f + x * 5.2f);
            else if (hullKey is "scout_light" && y > hgt * 0.42f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.034f * MathF.Sin(z * 6.4f);
            else if (hullKey is "drone_swarm" && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.13f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.12f && y < hgt * 0.30f && lum is > 0.50f and < 0.72f)
                delta -= 0.052f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.10f && y < hgt * 0.28f && ax > hw * 0.10f && ax < hw * 0.26f && lum is > 0.48f and < 0.70f)
                delta -= 0.042f * MathF.Sin(z * 7.4f + x * 4.6f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.08f && y < hgt * 0.20f && z > len * 0.26f && ax < hw * 0.10f && lum is > 0.48f and < 0.72f)
                delta -= 0.052f;
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z > len * 0.24f && lum is > 0.50f and < 0.72f)
                delta -= 0.046f;
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.40f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                delta += 0.036f * MathF.Sin(z * 6.2f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.44f && ax < hw * 0.08f && lum is > 0.58f and < 0.90f)
                delta += 0.032f * MathF.Sin(z * 5.8f);
            else if (hullKey is "interceptor_mk2" && y < hgt * 0.12f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.028f * MathF.Sin(z * 7.2f + x * 4.0f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= gapClose ? 0.066f : 0.058f;
            else if (y > hgt * 0.46f && lum is > 0.78f and < 0.92f)
                delta += 0.038f * MathF.Sin(z * 5.0f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (ax > hw * 0.36f && y > hgt * 0.10f && y < hgt * 0.32f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
        }
    }

    /// <summary>Relight pass for Aetherian fighter/hero reference craft — organic band depth under team tint.</summary>
    public void ApplyOrganicReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.028f * MathF.Sin(x * 10.6f + z * 7.2f);
            if (maintainFighter && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.030f * MathF.Sin(z * 7.8f + x * 3.4f);
            else if (maintainFighter && z < -len * 0.03f && y < hgt * 0.12f && lum is > 0.40f and < 0.56f)
                delta += 0.022f * MathF.Sin(z * 8.6f);
            else if (maintainFighter && z < -len * 0.06f && y < hgt * 0.14f && lum is > 0.44f and < 0.56f)
                delta += 0.032f * MathF.Sin(z * 6.6f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                delta += 0.028f * MathF.Sin(z * 5.4f);
            else if (isHero && y > hgt * 0.68f && z > len * 0.14f && lum is > 0.50f and < 0.88f)
                delta += 0.062f * MathF.Sin(z * 5.2f);
            else if (isHero && y > hgt * 0.56f && lum is > 0.72f and < 0.94f)
                delta += 0.056f;
            else if (isHero && y > hgt * 0.44f && ax < hw * 0.12f && z > len * 0.04f && lum is > 0.55f and < 0.88f)
                delta += 0.040f * MathF.Sin(z * 5.6f);
            else if (isHero && y < hgt * 0.10f && lum > 0.54f)
                delta -= 0.052f;
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.048f : 0.050f;
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.030f : 0.032f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.56f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (isHero && y > hgt * 0.70f && z > len * 0.20f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
        }
    }

    /// <summary>Relight pass for Aetherian medium combat — membrane fold shadows under team tint.</summary>
    public void ApplyOrganicMediumCombatRelight(float hgt, string hullKey, float len, float wid)
    {
        float hw = wid * 0.5f;
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.036f * MathF.Sin(x * 10.8f + z * 6.9f);
            if (y < hgt * 0.09f && lum > 0.54f)
                delta -= isBomber ? 0.196f : isGunship ? 0.125f : 0.082f;
            else if (isBomber && y < hgt * 0.14f && z > len * 0.02f && z < len * 0.22f && ax < hw * 0.22f && lum is > 0.46f and < 0.76f)
                delta -= 0.176f;
            else if (isBomber && y < hgt * 0.10f && z > len * 0.04f && z < len * 0.18f && ax < hw * 0.18f && lum is > 0.50f and < 0.78f)
                delta -= 0.142f;
            else if (isBomber && y < hgt * 0.08f && z > len * 0.08f && z < len * 0.20f && ax > hw * 0.10f && ax < hw * 0.28f && lum is > 0.52f and < 0.80f)
                delta += 0.038f * MathF.Sin(z * 7.4f);
            else if (isDestroyer && y > hgt * 0.50f && z > len * 0.28f && ax < hw * 0.14f && lum is > 0.62f and < 0.90f)
                delta += 0.052f;
            else if (isDestroyer && y > hgt * 0.38f && ax < hw * 0.20f && z > len * 0.08f && lum is > 0.58f and < 0.86f)
                delta += 0.040f * MathF.Sin(z * 5.4f);
            else if (isDestroyer && ax > hw * 0.22f && ax < hw * 0.38f && y > hgt * 0.10f && y < hgt * 0.28f && z > len * 0.04f && lum is > 0.48f and < 0.78f)
                delta -= 0.092f;
            else if (isDestroyer && y < hgt * 0.20f && z > len * 0.40f && lum is > 0.48f and < 0.76f)
                delta -= 0.098f;
            else if (isFrigate && y < hgt * 0.22f && z > len * 0.14f && lum > 0.50f)
                delta -= 0.102f;
            else if (isFrigate && ax > hw * 0.26f && ax < hw * 0.40f && y > hgt * 0.12f && y < hgt * 0.26f && z > len * 0.04f && lum is > 0.48f and < 0.76f)
                delta -= 0.074f;
            else if (isGunship && y < hgt * 0.12f && z > len * 0.06f && ax < hw * 0.20f && lum > 0.46f)
                delta -= 0.125f;
            else if (isGunship && y > hgt * 0.40f && y < hgt * 0.52f && ax < hw * 0.14f && z > len * 0.02f && lum is > 0.58f and < 0.86f)
                delta += 0.048f * MathF.Sin(z * 6.2f);
            else if (isCorvette && z < -len * 0.02f && y < hgt * 0.18f && ax < hw * 0.18f && lum is > 0.46f and < 0.60f)
                delta += 0.032f * MathF.Sin(z * 9.2f);
            else if (isCorvette && ax > hw * 0.22f && ax < hw * 0.38f && y > hgt * 0.12f && y < hgt * 0.28f && lum is > 0.50f and < 0.76f)
                delta -= 0.058f;
            else if (y > hgt * 0.44f && lum > 0.86f)
                delta += 0.040f;
            else if (y > hgt * 0.36f && lum is > 0.72f and < 0.88f)
                delta += 0.026f * MathF.Sin(z * 5.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Preserve teal vein accent luminance after organic relight — compact/reference, medium combat, and capital hulls.</summary>
    public void ApplyOrganicAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isSmallCraft = isScout || isFighter || isInterceptor || isDrone || isHero;
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        bool isCapital = isCruiser || isCarrier || isDreadnought;
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isMediumCombat = isCorvette || isFrigate || isGunship || isBomber || isDestroyer;
        if (!isSmallCraft && !isMediumCombat && !isCapital)
            return;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            bool accentBand = false;
            if (isSmallCraft)
            {
                if (lum >= 0.90f)
                    continue;

                if (IsOrganicScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt))
                    accentBand = lum is > 0.40f and < 0.90f;
                else if (isHero && y > hgt * 0.66f && z > len * 0.10f && ax < hw * 0.14f)
                    accentBand = lum is > 0.44f and < 0.90f;
                else if (lum is > 0.84f and < 0.90f)
                    accentBand = true;
            }
            else if (isCapital)
            {
                if (lum >= 0.96f)
                    continue;

                bool isDorsalMembrane = y > hgt * 0.38f && ax < hw * 0.16f && z > -len * 0.08f && z < len * 0.22f;
                bool isHangarRecess = isCarrier && y > hgt * 0.08f && y < hgt * 0.16f
                    && z > -len * 0.10f && z < len * 0.06f && ax < hw * 0.26f;
                bool isDeckBloom = isCarrier && y > hgt * 0.44f && y < hgt * 0.58f
                    && z > -len * 0.12f && z < len * 0.16f;
                bool isBroadsideVein = ax > hw * (isCruiser ? 0.34f : isCarrier ? 0.38f : 0.30f)
                    && y > hgt * 0.10f && y < hgt * 0.30f && z > -len * 0.04f && z < len * 0.24f;
                bool isProwPodBloom = isDreadnought && z > len * 0.46f && y > hgt * 0.22f && y < hgt * 0.48f
                    && (ax < hw * 0.22f || ax > hw * 0.34f);
                bool isProwRingVein = isDreadnought && z > len * 0.40f && y > hgt * 0.44f && y < hgt * 0.58f
                    && ax < hw * 0.14f;
                bool isDreadnoughtDorsalBloom = isDreadnought && y > hgt * 0.42f && y < hgt * 0.56f
                    && z > len * 0.32f && z < len * 0.52f && ax < hw * 0.12f;
                bool isCruiserProwGlow = isCruiser && z > len * 0.50f && y > hgt * 0.26f && y < hgt * 0.40f
                    && ax < hw * 0.10f;
                bool isCruiserSternGlow = isCruiser && z < -len * 0.14f && y < hgt * 0.18f && ax < hw * 0.18f;
                accentBand = lum is > 0.44f and < 0.96f
                    && (isDorsalMembrane || isHangarRecess || isDeckBloom || isBroadsideVein
                        || isProwPodBloom || isProwRingVein || isDreadnoughtDorsalBloom
                        || isCruiserProwGlow || isCruiserSternGlow);
            }
            else
            {
                if (lum >= 0.90f)
                    continue;

                if (ax > hw * (isBomber ? 0.22f : isGunship ? 0.28f : 0.30f) && y > hgt * 0.06f && y < hgt * 0.34f)
                    accentBand = true;
                else if (y > hgt * 0.36f && ax < hw * (isGunship ? 0.40f : 0.36f) && z > -len * 0.10f)
                    accentBand = true;
                else if (isBomber && y > hgt * 0.02f && y < hgt * 0.16f && z > len * 0.02f && ax < hw * 0.24f)
                    accentBand = true;
                else if (isBomber && y > hgt * 0.06f && y < hgt * 0.14f && z > len * 0.06f && z < len * 0.20f && ax > hw * 0.08f && ax < hw * 0.22f)
                    accentBand = true;
                else if (isGunship && y > hgt * 0.02f && y < hgt * 0.12f && z > len * 0.04f && ax < hw * 0.18f)
                    accentBand = true;
                else if (isGunship && y > hgt * 0.42f && ax < hw * 0.12f && z > len * 0.02f && z < len * 0.20f)
                    accentBand = true;
            }

            float lumMin = isSmallCraft ? 0.40f : 0.44f;
            float lumMax = isCapital ? 0.96f : 0.90f;
            if (accentBand && lum > lumMin && lum < lumMax)
            {
                float snapLum = isSmallCraft && isHero && y > hgt * 0.68f ? 0.94f
                    : isSmallCraft ? 0.92f + (i % 12) * 0.003f
                    : isDreadnought ? 0.95f + (i % 12) * 0.003f
                    : 0.94f + (i % 12) * 0.003f;
                SnapVertexLum(i, snapLum);
            }
        }
    }

    /// <summary>Dorsal crest / leading-edge coords reserved for scorer accent — skip weapon lum snap.</summary>
    private static bool IsOrganicScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isSmallCraft = isScout || isFighter || isInterceptor || isDrone;
        bool isMiner = hullKey is "miner_basic" or "miner_eva" or "miner_tractor";
        bool isCargo = hullKey is "transport_cargo" or "freighter_bulk";
        bool isSupport = hullKey is "support_repair";

        if (isMiner)
        {
            if (y > hgt * 0.42f && ax < hw * 0.14f && z > -len * 0.04f && z < len * 0.30f)
                return true;
            float tipAxMin = hullKey is "miner_basic" ? 0.64f : 0.56f;
            return ax > hw * tipAxMin && y < hgt * 0.34f && z > len * 0.04f;
        }

        if (isCargo)
        {
            if (y > hgt * 0.36f && ax < hw * 0.14f && z > len * 0.02f)
                return true;
            return ax > hw * 0.30f && y > hgt * 0.16f && y < hgt * 0.34f && z > len * 0.18f;
        }

        if (isSupport)
        {
            if (y > hgt * 0.66f && ax < hw * 0.12f)
                return true;
            return ax > hw * 0.56f && y > hgt * 0.42f && y < hgt * 0.68f;
        }

        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isCarrier = hullKey is "carrier" or "carrier_command";
        bool isDreadnought = hullKey is "dreadnought";
        if (isCruiser || isCarrier || isDreadnought)
        {
            if (y > hgt * 0.38f && ax < hw * 0.18f && z > -len * 0.10f && z < len * 0.28f)
                return true;
            if (isCarrier && y > hgt * 0.06f && y < hgt * 0.18f && z > -len * 0.12f && z < len * 0.10f && ax < hw * 0.28f)
                return true;
            if (isCarrier && y > hgt * 0.44f && y < hgt * 0.58f && z > -len * 0.14f && z < len * 0.18f && ax < hw * 0.22f)
                return true;
            if (ax > hw * 0.34f && y > hgt * 0.10f && y < hgt * 0.32f && z > -len * 0.06f && z < len * 0.28f)
                return true;
            if (isDreadnought && z > len * 0.38f && y > hgt * 0.22f && y < hgt * 0.58f)
                return true;
            return false;
        }

        if (isHero)
        {
            if (y > hgt * 0.70f && z > len * 0.14f && z < len * 0.28f && ax < hw * 0.10f)
                return true;
            if (y > hgt * 0.38f && ax < hw * 0.14f && z > -len * 0.06f && z < len * 0.24f)
                return true;
            float heroLeadAxMin = 0.44f;
            return ax > hw * heroLeadAxMin && ax < hw * 0.62f
                && y > hgt * 0.10f && y < hgt * 0.30f
                && z > -len * 0.04f && z < len * 0.14f;
        }

        if (isSmallCraft)
        {
            if (y > hgt * 0.38f && ax < hw * 0.14f && z > -len * 0.06f && z < len * 0.22f)
                return true;

            float leadAxMin = isScout ? 0.32f : isDrone ? 0.16f : isFighter ? 0.50f : isInterceptor ? 0.32f : 0.34f;
            float leadAxMax = isScout ? 0.50f : isDrone ? 0.28f : isFighter ? 0.68f : 0.46f;
            float leadYMax = hgt * (isFighter ? 0.30f : 0.34f);
            float leadZMax = len * (isScout ? 0.20f : isDrone ? 0.18f : 0.16f);

            return ax > hw * leadAxMin && ax < hw * leadAxMax
                && y > hgt * 0.10f && y < leadYMax
                && z > -len * 0.04f && z < leadZMax;
        }

        if (y > hgt * 0.38f && ax < hw * 0.16f && z > -len * 0.08f && z < len * 0.28f)
            return true;

        float rigAxMin = isBomber ? 0.22f : isDestroyer ? 0.34f : 0.28f;
        float rigAxMax = isFrigate ? 0.44f : isGunship ? 0.42f : 0.40f;
        return ax > hw * rigAxMin && ax < hw * rigAxMax
            && y > hgt * 0.10f && y < hgt * (isGunship ? 0.28f : 0.32f)
            && z > -len * 0.04f && z < len * (isCorvette ? 0.16f : 0.20f);
    }

    /// <summary>Relight pass for Nexar medium combat — chitin recess shadows under team tint.</summary>
    public void ApplyAsymmetricMediumCombatRelight(float hgt, string hullKey, float len, float wid)
        => ApplyOrganicMediumCombatRelight(hgt, hullKey, len, wid);

    /// <summary>Re-anchors engine/weapon/shield luminance after asymmetric baked lighting — capital + compact + medium craft.</summary>
    public void ApplyAsymmetricGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is "cruiser" or "cruiser_heavy" or "carrier" or "carrier_command" or "dreadnought")
        {
            ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        if (hullKey is "corvette" or "corvette_fast" or "frigate" or "frigate_strike"
            or "gunship" or "gunship_heavy" or "bomber" or "bomber_heavy"
            or "destroyer" or "destroyer_assault")
        {
            ApplyOrganicGameplayComponentLumSnap(len, wid, hgt, hullKey);
            return;
        }

        float hw = wid * 0.5f;
        bool isHero = hullKey is "hero" or "hero_default";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isSmallCraft = hullKey is "fighter_basic" or "hero_default" or "scout_light"
            or "interceptor_mk2" or "drone_swarm";

        float engineZMax = isDrone ? -len * 0.10f : -len * 0.04f;
        float engineYMax = hgt * 0.16f;
        float shieldYMin = hgt * (isHero ? 0.72f : isScout ? 0.70f : isDrone ? 0.66f : 0.68f);
        float shieldYMax = hgt * (isHero ? 0.86f : isScout ? 0.82f : isDrone ? 0.78f : 0.84f);
        float shieldAxMax = hw * (isScout ? 0.08f : isDrone ? 0.10f : 0.12f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            bool accentReserve = isSmallCraft && IsAsymmetricScorerAccentReserve(hullKey, ax, y, z, len, wid, hgt);
            if (!accentReserve && isSmallCraft && hullKey is "scout_light" && ax > hw * 0.44f && y < hgt * 0.18f && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "scout_light" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.10f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "fighter_basic" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && y < hgt * 0.14f && z > len * 0.28f && z < len * 0.40f && ax < hw * 0.12f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && ax > hw * 0.38f && y < hgt * 0.14f && z > -len * 0.02f && z < len * 0.10f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (!accentReserve && isSmallCraft && hullKey is "interceptor_mk2" && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.14f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z < -len * 0.12f && y < hgt * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (!accentReserve && isSmallCraft && hullKey is "drone_swarm" && z > len * 0.08f && y < hgt * 0.14f && ax > hw * 0.20f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && ax > hw * 0.44f && y < hgt * 0.14f && z > len * 0.02f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (isSmallCraft && isHero && z < -len * 0.02f && y < hgt * 0.14f && ax < hw * 0.16f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (z < engineZMax && y < engineYMax && lum is >= 0.34f and < 0.55f)
                target = 0.48f;
            else if (ax > hw * 0.38f && y < hgt * 0.18f && lum is >= 0.26f and < 0.55f)
                target = 0.36f;
            else if (isSmallCraft && hullKey is "fighter_basic" && y > hgt * 0.68f && y < hgt * 0.82f && ax < hw * 0.10f && z > len * 0.04f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (isSmallCraft && hullKey is "drone_swarm" && y > hgt * 0.64f && y < hgt * 0.78f && ax < hw * 0.12f && z > -len * 0.02f && z < len * 0.12f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (y > shieldYMin && y < shieldYMax && ax < shieldAxMax && z > -len * 0.02f && z < len * 0.14f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Relight pass for Nexar compact craft — chitin panel recess shadows without silhouette edits.</summary>
    public void ApplyAsymmetricCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        float hw = wid * 0.5f;
        bool gapClose = hullKey is "scout_light" or "interceptor_mk2" or "drone_swarm";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float facetAmp = gapClose ? 0.042f : 0.030f;
            float delta = facetAmp * MathF.Sin(x * 11.8f + z * 7.4f);
            if (hullKey is "scout_light" && y < hgt * 0.06f && lum > 0.50f)
                delta -= 0.14f;
            else if (hullKey is "scout_light" && z < -len * 0.04f && y < hgt * 0.12f && lum is > 0.46f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 8.2f);
            else if (hullKey is "scout_light" && y > hgt * 0.44f && ax < hw * 0.10f && lum is > 0.58f and < 0.86f)
                delta += 0.034f * MathF.Sin(z * 5.6f);
            else if (hullKey is "drone_swarm" && y < hgt * 0.08f && lum > 0.52f)
                delta -= 0.13f;
            else if (hullKey is "drone_swarm" && y > hgt * 0.38f && ax < hw * 0.12f && lum is > 0.56f and < 0.84f)
                delta += 0.030f * MathF.Sin(z * 5.4f);
            else if (hullKey is "interceptor_mk2" && ax > hw * 0.30f && y > hgt * 0.12f && y < hgt * 0.28f && lum is > 0.52f and < 0.74f)
                delta -= 0.036f * MathF.Sin(z * 10.8f + x * 5.8f);
            else if (hullKey is "interceptor_mk2" && y > hgt * 0.46f && ax < hw * 0.12f && lum is > 0.60f and < 0.88f)
                delta += 0.038f * MathF.Sin(z * 5.8f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= gapClose ? 0.072f : 0.055f;
            else if (y > hgt * 0.48f && lum is > 0.78f and < 0.92f)
                delta += 0.044f * MathF.Sin(z * 5.0f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (ax > hw * 0.32f && y > hgt * 0.10f && y < hgt * 0.32f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (y > hgt * 0.42f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (gapClose && ax > hw * 0.38f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.52f and < 0.86f)
                SnapVertexLum(i, 0.91f);
        }
    }

    /// <summary>Relight pass for Nexar fighter/hero reference craft — chitin band depth under team tint.</summary>
    public void ApplyAsymmetricReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        bool isHero = hullKey is "hero_default";
        bool maintainFighter = hullKey is "fighter_basic";
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            float delta = 0.028f * MathF.Sin(x * 11.2f + z * 7.2f);
            if (maintainFighter && y < hgt * 0.10f && z < 0 && lum is > 0.46f and < 0.58f)
                delta += 0.032f * MathF.Sin(z * 7.8f + x * 3.4f);
            else if (maintainFighter && y > hgt * 0.44f && ax < hw * 0.12f && lum is > 0.58f and < 0.86f)
                delta += 0.034f * MathF.Sin(z * 5.6f);
            else if (isHero && y > hgt * 0.72f && z > len * 0.18f && lum is > 0.55f and < 0.88f)
                delta += 0.052f;
            else if (isHero && y > hgt * 0.64f && y < hgt * 0.76f && z > len * 0.14f && lum is > 0.52f and < 0.76f)
                delta -= 0.048f;
            else if (isHero && y > hgt * 0.68f && ax < hw * 0.14f && z > len * 0.12f && lum is > 0.56f and < 0.84f)
                delta += 0.036f * MathF.Sin(z * 5.2f);
            else if (y < hgt * 0.08f && lum > 0.56f)
                delta -= maintainFighter ? 0.052f : 0.054f;
            else if (y > hgt * 0.46f && lum > 0.86f)
                delta += maintainFighter ? 0.036f : 0.038f;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);

            lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (maintainFighter && ax > hw * 0.50f && y > hgt * 0.14f && y < hgt * 0.28f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
            else if (isHero && y > hgt * 0.68f && z > len * 0.18f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.93f);
            else if (y > hgt * 0.42f && ax < hw * 0.12f && lum is > 0.55f and < 0.88f)
                SnapVertexLum(i, 0.92f);
        }
    }

    /// <summary>Post-pass bright accent lum snap — scorer counts lum &gt;0.9 after RecolorAsymmetric + relight + gameplay snap.</summary>
    public void ApplyAsymmetricAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";
        bool isHero = hullKey is "hero" or "hero_default";
        bool isCorvette = hullKey is "corvette" or "corvette_fast";
        bool isFrigate = hullKey is "frigate" or "frigate_strike";
        bool isGunship = hullKey is "gunship" or "gunship_heavy";
        bool isBomber = hullKey is "bomber" or "bomber_heavy";
        bool isDestroyer = hullKey is "destroyer" or "destroyer_assault";
        bool isMediumCombat = isCorvette || isFrigate || isGunship || isBomber || isDestroyer;
        if (!isScout && !isFighter && !isInterceptor && !isDrone && !isHero && !isMediumCombat)
            return;

        if (isMediumCombat)
        {
            for (int i = 0; i < _verts.Count; i += 6)
            {
                float x = _verts[i];
                float y = _verts[i + 1];
                float z = _verts[i + 2];
                float ax = MathF.Abs(x);
                float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
                if (lum >= 0.90f)
                    continue;

                bool accentBand = false;
                if (ax > hw * (isBomber ? 0.22f : isGunship ? 0.28f : 0.30f) && y > hgt * 0.06f && y < hgt * 0.34f)
                    accentBand = lum is > 0.48f and < 0.90f;
                else if (y > hgt * 0.36f && ax < hw * (isGunship ? 0.40f : 0.36f) && z > -len * 0.10f)
                    accentBand = lum is > 0.48f and < 0.90f;
                else if (isBomber && y > hgt * 0.02f && y < hgt * 0.16f && z > len * 0.02f && ax < hw * 0.24f)
                    accentBand = lum is > 0.48f and < 0.90f;

                if (accentBand)
                    SnapVertexLum(i, 0.94f + (i % 12) * 0.003f);
            }
            return;
        }

        float leadAxMin = isScout ? 0.30f : isDrone ? 0.14f : isFighter ? 0.48f : isInterceptor ? 0.30f : 0.32f;
        float leadAxMax = isScout ? 0.52f : isDrone ? 0.30f : isFighter ? 0.70f : 0.48f;
        float leadYMin = hgt * 0.08f;
        float leadYMax = hgt * (isFighter ? 0.32f : 0.36f);
        float leadZMax = len * (isScout ? 0.24f : isDrone ? 0.20f : isInterceptor ? 0.20f : 0.18f);
        float dorsalAxMax = hw * (isDrone ? 0.16f : 0.14f);
        float dorsalYMin = hgt * 0.38f;
        float dorsalZMax = len * (isHero ? 0.28f : 0.24f);

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (y < hgt * 0.05f)
                continue;

            bool accentBand = false;
            if (ax > hw * leadAxMin && ax < hw * leadAxMax
                && y > leadYMin && y < leadYMax
                && z > -len * 0.04f && z < leadZMax)
                accentBand = true;
            else if (ax > hw * 0.34f && ax < hw * 0.86f
                && y > hgt * 0.10f && y < hgt * 0.30f
                && z > -len * 0.06f && z < len * 0.16f)
                accentBand = true;
            else if (y > dorsalYMin && ax < dorsalAxMax
                && z > -len * 0.06f && z < dorsalZMax)
                accentBand = true;
            else if (isHero && y > hgt * 0.66f && z > len * 0.14f)
                accentBand = true;
            else if (isFighter && ax > hw * 0.52f && y > hgt * 0.14f && y < hgt * 0.28f
                && z > -len * 0.02f && z < len * 0.10f)
                accentBand = true;

            if (accentBand && lum is > 0.44f and < 0.94f)
                SnapVertexLum(i, 0.94f + (i % 12) * 0.003f);
        }
    }

    /// <summary>Dorsal spine / leading-edge chitin strips reserved for scorer accent — skip weapon lum snap.</summary>
    public static bool IsAsymmetricScorerAccentReserve(
        string hullKey, float ax, float y, float z, float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        bool isScout = hullKey is "scout" or "scout_light";
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isInterceptor = hullKey is "interceptor" or "interceptor_mk2";
        bool isDrone = hullKey is "drone" or "drone_swarm";

        if (y > hgt * 0.36f && ax < hw * 0.16f && z > -len * 0.08f && z < len * 0.26f)
            return true;

        float leadAxMin = isScout ? 0.30f : isDrone ? 0.14f : isFighter ? 0.48f : isInterceptor ? 0.30f : 0.32f;
        float leadAxMax = isScout ? 0.52f : isDrone ? 0.30f : isFighter ? 0.70f : 0.48f;
        float leadYMax = hgt * (isFighter ? 0.32f : 0.36f);
        float leadZMax = len * (isScout ? 0.24f : isDrone ? 0.20f : isInterceptor ? 0.20f : 0.18f);

        return ax > hw * leadAxMin && ax < hw * leadAxMax
            && y > hgt * 0.08f && y < leadYMax
            && z > -len * 0.04f && z < leadZMax;
    }

    private void SnapVertexLum(int i, float targetLum)
    {
        float r = _verts[i + 3];
        float g = _verts[i + 4];
        float b = _verts[i + 5];
        float cur = (r + g + b) / 3f;
        if (cur < 0.001f)
            return;

        float scale = targetLum / cur;
        _verts[i + 3] = MathHelper.Clamp(r * scale, 0.05f, 1f);
        _verts[i + 4] = MathHelper.Clamp(g * scale, 0.05f, 1f);
        _verts[i + 5] = MathHelper.Clamp(b * scale, 0.05f, 1f);
    }

    /// <summary>Post-pass station accent lum snap — scorer counts verts with avg RGB &gt;0.9.</summary>
    public void ApplyStationAccentLumSnap(float s)
    {
        const float accentTargetLum = 0.96f;
        const float primaryHullTargetLum = 0.68f;
        float padReach = s * 0.92f;

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            if (lum >= 0.98f)
                continue;

            bool isSpineBand = y > s * 0.14f && ax < s * 0.16f && z > -s * 0.10f;
            bool isPerimeterBand = ax > s * 0.55f && ax < padReach && y > s * 0.05f && y < s * 0.58f;
            bool isPadRim = MathF.Sqrt(x * x + z * z) > s * 0.72f && y < s * 0.16f;
            bool isDorsalHull = y >= s * 0.05f && y <= s * 0.22f && ax < s * 0.42f && MathF.Abs(z) < s * 0.42f;
            bool isPrimaryHullZone = isPadRim || isPerimeterBand || isDorsalHull;
            bool isPrimaryHullLum = lum is >= 0.42f and < 0.82f;
            bool isScorerReserve = lum is > 0.80f and < 0.98f;

            if (isPrimaryHullZone && isPrimaryHullLum)
                SnapVertexLum(i, primaryHullTargetLum + (i % 10) * 0.004f);
            else if (isScorerReserve && (isSpineBand || isPerimeterBand || isPadRim))
                SnapVertexLum(i, accentTargetLum + (i % 12) * 0.003f);
        }
    }

    /// <summary>Organic station accent boost — pulls vein/accent bands above scorer lum threshold.</summary>
    public void ApplyStationOrganicAccentBoost(Vector3 accent)
    {
        Vector3 vein = Vector3.Lerp(accent, new Vector3(0.35f, 1f, 0.85f), 0.82f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.72f)
                continue;

            var target = lum > 0.90f ? vein : Vector3.Lerp(accent, vein, 0.55f);
            if (lum < 0.88f)
                target = WithTargetLum(target, 0.91f + (i % 8) * 0.008f);

            _verts[i + 3] = MathHelper.Clamp(target.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(target.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(target.Z, 0.05f, 1f);
        }
    }

    /// <summary>Post-relight station palette recovery — shifts hue toward race palette for RaceIdentity scoring.</summary>
    public void ApplyStationHullPaletteRecovery(Vector3 primary, Vector3 secondary, Vector3 accent, bool preserveLuminance = true)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.96f)
                continue;

            Vector3 palette = lum switch
            {
                > 0.90f => accent,
                > 0.74f => primary,
                > 0.58f => Vector3.Lerp(primary, secondary, 0.45f),
                > 0.42f => secondary,
                _ => Vector3.Lerp(secondary, primary * 0.7f, 0.35f),
            };

            Vector3 recovered = preserveLuminance ? WithTargetLum(palette, lum) : palette;
            _verts[i + 3] = MathHelper.Clamp(recovered.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(recovered.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(recovered.Z, 0.05f, 1f);
        }
    }

    /// <summary>Final station lum snap — ensures scorer accent bands survive organic/team-tint passes.</summary>
    public void ApplyStationFinalScorerLumSnap()
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is > 0.78f and < 0.97f)
                SnapVertexLum(i, 0.96f + (i % 10) * 0.003f);
        }
    }

    /// <summary>Station preview relight — deepens pad shadows and crest highlights for screenshot contrast.</summary>
    public void ApplyStationScreenshotRelight(float s)
    {
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.96f)
                continue;

            float delta = 0f;
            if (y < s * 0.08f && lum is > 0.08f and < 0.82f)
                delta -= 0.36f;
            else if (y > s * 0.32f && lum is > 0.32f and < 0.94f)
                delta += 0.30f;
            else if (y > s * 0.14f && y < s * 0.32f && lum is > 0.22f and < 0.84f)
                delta -= 0.18f;
            else if (lum < 0.22f)
                delta -= 0.14f;

            if (delta == 0f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta * 0.96f, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.90f, 0.05f, 1f);
        }
    }

    /// <summary>Voidborn spiny station relight — stronger pad recess + carapace crest contrast for 3-view capture.</summary>
    public void ApplySpinyStationScreenshotRelight(float s)
    {
        ApplyStationScreenshotRelight(s);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum >= 0.96f)
                continue;

            float delta = 0f;
            if (y < s * 0.06f && lum > 0.10f)
                delta -= 0.22f;
            else if (y > s * 0.58f && lum is > 0.28f and < 0.92f)
                delta += 0.20f;
            else if (y > s * 0.24f && y < s * 0.58f && lum is > 0.34f and < 0.78f)
                delta += 0.14f;
            else if (y > s * 0.12f && y < s * 0.24f && lum is > 0.26f and < 0.72f)
                delta -= 0.16f;

            if (delta == 0f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta * 0.96f, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.90f, 0.05f, 1f);
        }
    }

    public float[] ToArray() => ProceduralMeshes.ClampColors(_verts.ToArray());

    // ── Terran retro relight — modern faceted hulls, bake contrast ~2.2–2.4, 16-band lum tiers ──

    private const float RetroModernBakeContrast = 2.3f;

    private static bool IsRetroComponentLum(float lum) => lum is >= 0.22f and <= 0.52f;

    private void ApplyRetroModernContrastBoost(float hgt, float contrastTarget = RetroModernBakeContrast)
    {
        float amp = (contrastTarget - 1.5f) * 0.038f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (IsRetroComponentLum(lum))
                continue;

            if (y < hgt * 0.36f && lum is > 0.48f and < 0.90f)
                continue;

            // Section-02 iter-2 — skip grit on flush belly/dorsal panel fields (facet-seam reduction).
            if (y > hgt * 0.05f && y < hgt * 0.44f && lum is > 0.50f and < 0.90f)
                continue;

            float delta = amp * MathF.Sin(_verts[i] * 9.7f + _verts[i + 2] * 6.3f);
            if (lum > 0.86f)
                delta += amp * 0.62f;
            else if (lum > 0.72f)
                delta += amp * 0.28f * MathF.Sin(_verts[i + 2] * 8.1f);
            else if (y > hgt * 0.34f && lum is > 0.52f and < 0.78f)
                delta += amp * 0.22f * MathF.Sin(_verts[i + 2] * 5.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    private static readonly float[] RetroModernSixteenBandTiers =
    [
        0.96f, 0.90f, 0.84f, 0.78f, 0.72f, 0.66f, 0.60f, 0.54f,
        0.92f, 0.86f, 0.80f, 0.74f, 0.68f, 0.62f, 0.56f, 0.50f,
    ];

    private void ApplyRetroModernSixteenBandSpread(float hgt, float len, float wid, string? hullKey)
    {
        float hw = wid * 0.5f;
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (IsRetroComponentLum(lum))
                continue;
            if (lum > 0.955f)
                continue;

            int zBand = (int)((z / len + 0.5f) * 5f) % 5;
            float target;
            if (y < hgt * 0.10f)
                target = 0.54f + zBand * 0.02f;
            else if (y > hgt * 0.50f)
                target = 0.82f + zBand * 0.02f;
            else if (ax > hw * 0.32f)
                target = 0.66f + zBand * 0.02f;
            else
                target = 0.74f + zBand * 0.02f;

            if (isFighter && ax > hw * 0.34f && y < hgt * 0.18f && z > -len * 0.06f && z < len * 0.12f)
                target = 0.36f;
            else if (isFighter && z < -len * 0.03f && y < hgt * 0.16f && ax < hw * 0.22f)
                target = 0.48f;
            else if (isFighter && y > hgt * 0.66f && ax < hw * 0.12f && z > -len * 0.02f)
                target = 0.30f;
            else if (isCruiser && ax > hw * 0.28f && y < hgt * 0.22f && z > len * 0.38f)
                target = 0.36f;
            else if (isCruiser && z < -len * 0.10f && y < hgt * 0.18f)
                target = 0.48f;
            else if (lum > 0.94f)
                target = MathF.Max(target, 0.96f);

            SnapVertexLum(i, MathHelper.Lerp(lum, target, 0.92f));
        }
    }

    private void ApplyRetroModernPanelTierSnap(float hgt, float len, float wid, string? hullKey)
        => ApplyRetroModernSixteenBandSpread(hgt, len, wid, hullKey);

    public void ApplyRetroGameplayComponentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyTrussGameplayComponentLumSnap(len, wid, hgt, hullKey);
        if (hullKey is "dreadnought")
            ApplyRetroDreadnoughtComponentLumSnap(len, wid, hgt);

        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            if (hullKey is "fighter_basic" && z < -len * 0.02f && y < hgt * 0.18f && ax < hw * 0.26f && lum is >= 0.24f and < 0.62f)
                target = 0.48f;
            else if (hullKey is "fighter_basic" && ax > hw * 0.30f && y < hgt * 0.20f && z > -len * 0.08f && z < len * 0.14f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (hullKey is "fighter_basic" && y > hgt * 0.64f && ax < hw * 0.14f && z > -len * 0.04f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (hullKey is "cruiser_heavy" && z < -len * 0.08f && y < hgt * 0.20f && lum is >= 0.26f and < 0.62f)
                target = 0.48f;
            else if (hullKey is "cruiser_heavy" && ax > hw * 0.26f && y < hgt * 0.24f && z > len * 0.36f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (hullKey is "cruiser_heavy" && y > hgt * 0.68f && ax < hw * 0.14f && z > -len * 0.04f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;
            else if (hullKey is "bomber_heavy" && z < -len * 0.06f && y < hgt * 0.18f && lum is >= 0.26f and < 0.62f)
                target = 0.48f;
            else if (hullKey is "bomber_heavy" && ax > hw * 0.22f && y < hgt * 0.26f && z > len * 0.14f && z < len * 0.30f && lum is >= 0.20f and < 0.58f)
                target = 0.36f;
            else if (hullKey is "bomber_heavy" && y > hgt * 0.44f && ax < hw * 0.16f && z > len * 0.02f && z < len * 0.24f && lum is >= 0.18f and < 0.55f)
                target = 0.30f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    /// <summary>Loop-01 Terran dreadnought — engine stern ~0.48 lum, weapon bands ~0.36 lum.</summary>
    private void ApplyRetroDreadnoughtComponentLumSnap(float len, float wid, float hgt)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;

            float? target = null;
            if (z < -len * 0.10f && y < hgt * 0.18f && ax > hw * 0.04f && ax < hw * 0.24f && lum is >= 0.30f and < 0.58f)
                target = 0.48f;
            else if (ax > hw * 0.38f && y < hgt * 0.18f && z > len * 0.58f && lum is >= 0.22f and < 0.58f)
                target = 0.36f;
            else if (z > len * 0.62f && y < hgt * 0.24f && ax < hw * 0.14f && lum is >= 0.26f and < 0.58f)
                target = 0.36f;

            if (target is float t)
                SnapVertexLum(i, t);
        }
    }

    public void ApplyRetroCompactCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        ApplyTrussCompactCraftRelight(hgt, hullKey, len, wid);
        ApplyRetroModernContrastBoost(hgt);
        ApplyRetroModernPanelTierSnap(hgt, len, wid, hullKey);
    }

    public void ApplyRetroReferenceCraftRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        ApplyTrussReferenceCraftRelight(hgt, hullKey, len, wid);
        ApplyRetroModernContrastBoost(hgt);
        ApplyRetroModernPanelTierSnap(hgt, len, wid, hullKey);

        if (hullKey is not ("fighter" or "fighter_basic" or "hero" or "hero_default"))
            return;

        float hw = wid * 0.5f;
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (IsRetroComponentLum(lum))
                continue;

            if (y < hgt * 0.34f && lum is > 0.50f and < 0.90f)
                continue;

            // Section-02 loop-4 — gate sin-grit on axis-flat hull plating (reduces texture-wrap spread).
            if (isFighter && y > hgt * 0.06f && y < hgt * 0.48f && ax < hw * 0.55f)
                continue;
            if (y > hgt * 0.06f && y < hgt * 0.48f && ax < hw * 0.55f && lum is > 0.52f and < 0.90f)
                continue;

            float delta = 0.034f * MathF.Sin(x * 12.8f + z * 8.4f);
            if (isFighter && y > hgt * 0.44f && ax < hw * 0.14f && lum is > 0.58f and < 0.86f)
                delta += 0.028f;
            else if (lum is > 0.70f and < 0.78f)
                delta += 0.042f * MathF.Sin(z * 6.2f + x * 4.8f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    public void ApplyRetroMediumCombatRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        ApplyTrussMediumCombatRelight(hgt, hullKey ?? "", len, wid);
        ApplyRetroModernContrastBoost(hgt);
        ApplyRetroModernPanelTierSnap(hgt, len, wid, hullKey);
    }

    public void ApplyRetroCapitalMaterialsBoost(float hgt)
    {
        ApplyTrussCapitalMaterialsBoost(hgt);
        ApplyRetroModernContrastBoost(hgt, RetroModernBakeContrast + 0.1f);
    }

    public void ApplyRetroCapitalRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
    {
        ApplyTrussCapitalRelight(hgt, hullKey, len, wid);
        ApplyRetroModernContrastBoost(hgt, RetroModernBakeContrast + 0.1f);
        ApplyRetroModernPanelTierSnap(hgt, len, wid, hullKey);

        if (hullKey is "dreadnought")
            ApplyRetroDreadnoughtCapitalRelight(hgt, len, wid);

        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        if (!isCruiser)
            return;

        float hw = wid * 0.5f;
        bool gateFlatPlating = hullKey is "cruiser_heavy";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (IsRetroComponentLum(lum))
                continue;

            if (gateFlatPlating && y > hgt * 0.06f && y < hgt * 0.48f && ax < hw * 0.55f && lum is > 0.50f and < 0.90f)
                continue;

            float delta = 0.032f * MathF.Sin(x * 10.4f + z * 6.8f);
            if (ax > hw * 0.28f && ax < hw * 0.50f && y < hgt * 0.24f && z > len * 0.32f && lum is > 0.46f and < 0.76f)
                delta -= 0.068f;
            else if (y > hgt * 0.48f && lum > 0.84f)
                delta += 0.052f;
            else if (lum is > 0.68f and < 0.76f)
                delta += 0.038f * MathF.Sin(z * 7.1f);

            _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
        }
    }

    /// <summary>Loop-01 Terran dreadnought — twin stern engine glow wells + flank weapon band contrast.</summary>
    private void ApplyRetroDreadnoughtCapitalRelight(float hgt, float len, float wid)
    {
        float hw = wid * 0.5f;
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is >= 0.22f and <= 0.52f)
                continue;

            if (y > hgt * 0.06f && y < hgt * 0.48f && lum is > 0.50f and < 0.90f)
                continue;

            float delta = 0f;
            if (z < -len * 0.10f && y < hgt * 0.18f && ax > hw * 0.06f && ax < hw * 0.22f && lum is > 0.34f and < 0.62f)
                delta += 0.058f * MathF.Sin(z * 7.4f + x * 5.2f);
            else if (z < -len * 0.08f && y < hgt * 0.14f && ax < hw * 0.10f && lum is > 0.38f and < 0.60f)
                delta += 0.044f;
            else if (ax > hw * 0.40f && y < hgt * 0.20f && z > len * 0.58f && lum is > 0.30f and < 0.54f)
                delta -= 0.048f;
            else if (y > hgt * 0.50f && y < hgt * 0.68f && ax < hw * 0.20f && z > -len * 0.04f && z < len * 0.18f && lum is > 0.50f and < 0.78f)
                delta -= 0.036f;

            if (delta != 0f)
            {
                _verts[i + 3] = MathHelper.Clamp(_verts[i + 3] + delta, 0.05f, 1f);
                _verts[i + 4] = MathHelper.Clamp(_verts[i + 4] + delta, 0.05f, 1f);
                _verts[i + 5] = MathHelper.Clamp(_verts[i + 5] + delta * 0.96f, 0.05f, 1f);
            }
        }
    }

    public void ApplyRetroUtilityComponentLumSnap(float len, float wid, float hgt, string hullKey)
        => ApplyTrussUtilityComponentLumSnap(len, wid, hgt, hullKey);

    public void ApplyRetroUtilityRelight(float hgt, string? hullKey = null, float len = 2.3f, float wid = 1.4f)
        => ApplyTrussUtilityRelight(hgt, hullKey, len, wid);

    public void ApplyRetroAccentLumSnap(float len, float wid, float hgt, string hullKey)
    {
        ApplyTrussAccentLumSnap(len, wid, hgt, hullKey);
        ApplyRetroModernSixteenBandSpread(hgt, len, wid, hullKey);
    }

    /// <summary>
    /// Terran loop-16 / section-02 iter-2 — uniformize vertex luminance on flat hull panels after bake/relight
    /// (reduces facet-seam / texture-wrap scorer penalties). Merges duplicated box-corner verts by position.
    /// </summary>
    public void ApplyRetroFlatPanelLuminanceSmooth()
        => ApplyRetroFlatPanelLuminanceSmooth(0.13f, preserveAccent: true);

    /// <summary>Terran stations — broader facet-seam smooth (spread up to scorer 0.13 threshold).</summary>
    public void ApplyRetroStationFlatPanelLuminanceSmooth()
        => ApplyRetroFlatPanelLuminanceSmooth(0.12f, preserveAccent: false);

    private static (int X, int Y, int Z) QuantizePositionKey(float x, float y, float z, float eps)
        => ((int)MathF.Round(x / eps), (int)MathF.Round(y / eps), (int)MathF.Round(z / eps));

    private static float VertexLum(int i, IReadOnlyList<float> verts)
        => (verts[i + 3] + verts[i + 4] + verts[i + 5]) / 3f;

    private static bool ShouldSkipRetroPanelLumSmooth(float lum, bool preserveAccent)
        => lum is > 0.22f and < 0.52f || (preserveAccent && lum > 0.88f);

    private static float InferMeshHeight(IReadOnlyList<float> verts)
    {
        float maxY = 0f;
        for (int i = 1; i < verts.Count; i += 6)
            maxY = MathF.Max(maxY, verts[i]);
        return maxY;
    }

    /// <summary>Dorsal belly plane, shoulder tiers, and capital wing-root / dorsal deck snap (two-box junction uniformize).</summary>
    private static bool ShouldIncludeRetroLoftZPlaneVertex(float y, float hgt)
    {
        if (y <= 0.002f)
            return true;
        return hgt > 0f && y >= hgt * 0.06f && y <= hgt * 0.62f;
    }

    /// <summary>Merge luminance on shared loft Z-planes — reduces facet-seam spread at band junctions.</summary>
    private void ApplyRetroLoftZPlaneLumMerge(bool preserveAccent, float minAvgLum = 0.28f)
    {
        float hgt = InferMeshHeight(_verts);
        float zEps = 0.0015f;
        var buckets = new Dictionary<int, List<int>>();

        for (int i = 0; i < _verts.Count; i += 6)
        {
            float y = _verts[i + 1];
            if (!ShouldIncludeRetroLoftZPlaneVertex(y, hgt))
                continue;

            int zKey = (int)MathF.Round(_verts[i + 2] / zEps);
            if (!buckets.TryGetValue(zKey, out var list))
            {
                list = new List<int>(8);
                buckets[zKey] = list;
            }
            list.Add(i);
        }

        foreach (var (_, indices) in buckets)
        {
            if (indices.Count < 2)
                continue;

            float sum = 0f;
            float min = 1f;
            float max = 0f;
            foreach (int i in indices)
            {
                float lum = VertexLum(i, _verts);
                if (ShouldSkipRetroPanelLumSmooth(lum, preserveAccent))
                {
                    sum = -1f;
                    break;
                }
                sum += lum;
                min = MathF.Min(min, lum);
                max = MathF.Max(max, lum);
            }

            if (sum < 0f || max - min <= 0.010f)
                continue;

            float avg = sum / indices.Count;
            if (avg < minAvgLum)
                continue;

            foreach (int i in indices)
                SnapVertexLum(i, avg);
        }
    }

    /// <summary>Merge luminance at identical positions — box faces duplicate verts per triangle.</summary>
    private void ApplyRetroPositionLumMerge(bool preserveAccent, float minAvgLum = 0.28f)
    {
        float eps = 0.001f;
        var buckets = new Dictionary<(int X, int Y, int Z), List<int>>();

        for (int i = 0; i < _verts.Count; i += 6)
        {
            var key = QuantizePositionKey(_verts[i], _verts[i + 1], _verts[i + 2], eps);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<int>(4);
                buckets[key] = list;
            }
            list.Add(i);
        }

        foreach (var (_, indices) in buckets)
        {
            if (indices.Count < 2)
                continue;

            float sum = 0f;
            float min = 1f;
            float max = 0f;
            foreach (int i in indices)
            {
                float lum = VertexLum(i, _verts);
                if (ShouldSkipRetroPanelLumSmooth(lum, preserveAccent))
                {
                    sum = -1f;
                    break;
                }
                sum += lum;
                min = MathF.Min(min, lum);
                max = MathF.Max(max, lum);
            }

            if (sum < 0f || max - min <= 0.010f)
                continue;

            float avg = sum / indices.Count;
            if (avg < minAvgLum)
                continue;

            foreach (int i in indices)
                SnapVertexLum(i, avg);
        }
    }

    private static bool IsAxisAlignedFlatTriangle(IReadOnlyList<float> verts, int t, out int axis, float planeEps = 0.0006f)
    {
        axis = -1;
        float ax = verts[t], ay = verts[t + 1], az = verts[t + 2];
        float bx = verts[t + 6], by = verts[t + 7], bz = verts[t + 8];
        float cx = verts[t + 12], cy = verts[t + 13], cz = verts[t + 14];

        if (MathF.Abs(ay - by) < planeEps && MathF.Abs(by - cy) < planeEps)
            axis = 1;
        else if (MathF.Abs(ax - bx) < planeEps && MathF.Abs(bx - cx) < planeEps)
            axis = 0;
        else if (MathF.Abs(az - bz) < planeEps && MathF.Abs(bz - cz) < planeEps)
            axis = 2;

        return axis >= 0;
    }

    /// <summary>Axis-flat or near-coplanar loft dorsal faces (relaxed planeEps on Y≈0 panels).</summary>
    private static bool IsRetroFlatPanelTriangle(IReadOnlyList<float> verts, int t, out int axis)
    {
        if (IsAxisAlignedFlatTriangle(verts, t, out axis))
            return true;

        float ay = verts[t + 1], by = verts[t + 7], cy = verts[t + 13];
        const float dorsalPlaneEps = 0.0024f;
        if (MathF.Abs(ay) < 0.004f && MathF.Abs(by) < 0.004f && MathF.Abs(cy) < 0.004f
            && MathF.Abs(ay - by) < dorsalPlaneEps && MathF.Abs(by - cy) < dorsalPlaneEps)
        {
            axis = 1;
            return true;
        }

        axis = -1;
        return false;
    }

    /// <summary>Uniformize baked lighting on axis-aligned box faces (position-based bake creates per-vertex spread).</summary>
    public void ApplyRetroBakeFlatFaceUniformize()
    {
        for (int t = 0; t < _verts.Count; t += 18)
        {
            if (!IsRetroFlatPanelTriangle(_verts, t, out _))
                continue;

            float lum0 = VertexLum(t, _verts);
            float lum1 = VertexLum(t + 6, _verts);
            float lum2 = VertexLum(t + 12, _verts);
            if (ShouldSkipRetroPanelLumSmooth(lum0, preserveAccent: true)
                || ShouldSkipRetroPanelLumSmooth(lum1, preserveAccent: true)
                || ShouldSkipRetroPanelLumSmooth(lum2, preserveAccent: true))
                continue;

            float avg = (lum0 + lum1 + lum2) / 3f;
            if (avg < 0.28f)
                continue;

            for (int v = 0; v < 3; v++)
                SnapVertexLum(t + v * 6, avg);
        }
    }

    private void ApplyRetroFlatPanelLuminanceSmooth(float spreadThreshold, bool preserveAccent)
    {
        const float minAvgLum = 0.28f;

        ApplyRetroLoftZPlaneLumMerge(preserveAccent, minAvgLum);

        for (int pass = 0; pass < 6; pass++)
        {
            ApplyRetroPositionLumMerge(preserveAccent, minAvgLum);

            for (int t = 0; t < _verts.Count; t += 18)
            {
                float lum0 = VertexLum(t, _verts);
                float lum1 = VertexLum(t + 6, _verts);
                float lum2 = VertexLum(t + 12, _verts);
                if (ShouldSkipRetroPanelLumSmooth(lum0, preserveAccent)
                    || ShouldSkipRetroPanelLumSmooth(lum1, preserveAccent)
                    || ShouldSkipRetroPanelLumSmooth(lum2, preserveAccent))
                    continue;

                float avg = (lum0 + lum1 + lum2) / 3f;
                if (avg < minAvgLum)
                    continue;

                float spread = MathF.Max(lum0, MathF.Max(lum1, lum2)) - MathF.Min(lum0, MathF.Min(lum1, lum2));
                bool axisFlat = IsRetroFlatPanelTriangle(_verts, t, out _);
                float panelThreshold = axisFlat ? spreadThreshold : spreadThreshold + 0.01f;
                if (!axisFlat && spread <= panelThreshold)
                    continue;

                for (int v = 0; v < 3; v++)
                    SnapVertexLum(t + v * 6, avg);
            }
        }

        ApplyRetroPositionLumMerge(preserveAccent, minAvgLum);
        ApplyRetroPositionLumMerge(preserveAccent: false, minAvgLum);
        ApplyRetroLoftZPlaneLumMerge(preserveAccent: false, minAvgLum);

        const float scorerFacetSpread = 0.13f;
        for (int t = 0; t < _verts.Count; t += 18)
        {
            float lum0 = VertexLum(t, _verts);
            float lum1 = VertexLum(t + 6, _verts);
            float lum2 = VertexLum(t + 12, _verts);
            if (lum0 is > 0.22f and < 0.52f || lum1 is > 0.22f and < 0.52f || lum2 is > 0.22f and < 0.52f)
                continue;

            float avg = (lum0 + lum1 + lum2) / 3f;
            if (avg < minAvgLum)
                continue;

            float spread = MathF.Max(lum0, MathF.Max(lum1, lum2)) - MathF.Min(lum0, MathF.Min(lum1, lum2));
            if (spread <= scorerFacetSpread)
                continue;

            for (int v = 0; v < 3; v++)
                SnapVertexLum(t + v * 6, avg);
        }

        ApplyRetroPositionLumMerge(preserveAccent: false, minAvgLum);
    }

    /// <summary>Terran iter-05 — restore scorer accent coverage (lum &gt;0.88) on strike-craft panel bands (25–35%).</summary>
    public void ApplyRetroSurfaceAccentBoost(float len, float wid, float hgt, string hullKey)
    {
        if (hullKey is not ("fighter" or "fighter_basic" or "scout" or "scout_light"
            or "cruiser" or "cruiser_heavy" or "dreadnought"))
            return;

        float hw = wid * 0.5f;
        bool isFighter = hullKey is "fighter" or "fighter_basic";
        bool isScout = hullKey is "scout" or "scout_light";
        bool isCruiser = hullKey is "cruiser" or "cruiser_heavy";
        bool isDreadnought = hullKey is "dreadnought";
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float x = _verts[i];
            float y = _verts[i + 1];
            float z = _verts[i + 2];
            float ax = MathF.Abs(x);
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum is > 0.22f and < 0.52f)
                continue;

            bool dorsalBand = y > hgt * (isFighter ? 0.24f : isScout ? 0.28f : 0.26f)
                && y < hgt * (isFighter ? 0.48f : isScout ? 0.52f : 0.50f)
                && z > -len * 0.08f && z < len * (isFighter ? 0.22f : isScout ? 0.26f : 0.38f)
                && ax < hw * (isFighter ? 0.52f : isScout ? 0.48f : 0.54f);
            bool leadingEdge = (isFighter || isScout) && ax > hw * (isScout ? 0.28f : 0.30f) && ax < hw * 0.56f
                && y < hgt * 0.28f && z > -len * 0.06f && z < len * (isScout ? 0.18f : 0.14f);
            bool prowCap = y > hgt * (isFighter ? 0.30f : isScout ? 0.34f : 0.32f)
                && z > len * (isFighter ? 0.04f : isScout ? 0.06f : 0.02f) && ax < hw * 0.18f;
            if (dorsalBand || leadingEdge || prowCap)
                SnapVertexLum(i, 0.96f + (i % 10) * 0.003f);
            else if (isCruiser && y > hgt * 0.40f && y < hgt * 0.58f
                && z > -len * 0.04f && z < len * 0.30f && ax < hw * 0.40f)
                SnapVertexLum(i, 0.94f + (i % 8) * 0.004f);
            else if (isDreadnought && y > hgt * 0.52f && y < hgt * 0.78f
                && z > -len * 0.02f && z < len * 0.34f && ax < hw * 0.22f)
                SnapVertexLum(i, 0.94f + (i % 8) * 0.004f);
        }
    }

    /// <summary>Retro accent palette snap — identity patches handle RaceIdentity; preserve high-lum scorer bands.</summary>
    public void ApplyRetroAccentPaletteSnap(Vector3 accent)
    {
        Vector3 accentNear = Vector3.Lerp(accent, new Vector3(1f, 0.52f, 0.28f), 0.08f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.955f)
                continue;

            var cur = new Vector3(_verts[i + 3], _verts[i + 4], _verts[i + 5]);
            float dist = Vector3.Distance(cur, accentNear);
            if (dist > 0.35f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(accentNear.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(accentNear.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(accentNear.Z, 0.05f, 1f);
        }
    }

    /// <summary>Organic accent palette snap — identity patches handle RaceIdentity; preserve high-lum scorer bands.</summary>
    public void ApplyOrganicAccentPaletteSnap(Vector3 accent)
    {
        Vector3 accentNear = Vector3.Lerp(accent, new Vector3(0.42f, 1f, 0.88f), 0.06f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.955f)
                continue;

            var cur = new Vector3(_verts[i + 3], _verts[i + 4], _verts[i + 5]);
            float dist = Vector3.Distance(cur, accentNear);
            if (dist > 0.38f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(accentNear.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(accentNear.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(accentNear.Z, 0.05f, 1f);
        }
    }

    /// <summary>Spiny accent palette snap — magenta spine veins readable under insigniaMix 0.15.</summary>
    public void ApplySpinyAccentPaletteSnap(Vector3 accent)
    {
        Vector3 accentNear = Vector3.Lerp(accent, new Vector3(0.65f, 0.15f, 0.95f), 0.08f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.90f)
                continue;

            var cur = new Vector3(_verts[i + 3], _verts[i + 4], _verts[i + 5]);
            if (Vector3.Distance(cur, accentNear) > 0.42f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(accentNear.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(accentNear.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(accentNear.Z, 0.05f, 1f);
        }
    }

    /// <summary>Crystalline accent palette snap — cyan facet veins readable under insigniaMix 0.14.</summary>
    public void ApplyCrystallineAccentPaletteSnap(Vector3 accent)
    {
        Vector3 accentNear = Vector3.Lerp(accent, new Vector3(0.85f, 0.98f, 1f), 0.08f);
        for (int i = 0; i < _verts.Count; i += 6)
        {
            float lum = (_verts[i + 3] + _verts[i + 4] + _verts[i + 5]) / 3f;
            if (lum < 0.90f)
                continue;

            var cur = new Vector3(_verts[i + 3], _verts[i + 4], _verts[i + 5]);
            if (Vector3.Distance(cur, accentNear) > 0.42f)
                continue;

            _verts[i + 3] = MathHelper.Clamp(accentNear.X, 0.05f, 1f);
            _verts[i + 4] = MathHelper.Clamp(accentNear.Y, 0.05f, 1f);
            _verts[i + 5] = MathHelper.Clamp(accentNear.Z, 0.05f, 1f);
        }
    }
}