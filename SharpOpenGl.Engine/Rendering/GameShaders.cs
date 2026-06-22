namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// GLSL sources shared by desktop (330 core) and browser (300 es) renderers.
/// Race textures use model-space UVs so plating moves with the hull.
/// Team colors appear as insignia and aura only.
/// </summary>
public static class GameShaders
{
    /// <summary>Shared GLSL for procedural race hull plating (indices 0–7).</summary>
    public const string RaceTextureGlsl = @"
float raceUvScale(int raceIdx) {
    float scales[8] = float[8](0.14, 0.12, 0.16, 0.13, 0.15, 0.12, 0.14, 0.15);
    return scales[clamp(raceIdx, 0, 7)];
}

float raceHash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float racePanel(vec2 uv, float scale, float thickness) {
    vec2 g = fract(uv * scale);
    float edge = min(min(g.x, 1.0 - g.x), min(g.y, 1.0 - g.y));
    return smoothstep(thickness, thickness + 0.12, edge);
}

float raceSoftNoise(vec2 uv) {
    return 0.5 + 0.5 * sin(uv.x * 5.5) * cos(uv.y * 4.0);
}

float raceRivet(vec2 uv, float scale) {
    vec2 cell = floor(uv * scale);
    vec2 f = fract(uv * scale);
    float d = length(f - 0.5);
    return smoothstep(0.2, 0.07, d) * step(0.78, raceHash(cell));
}

vec3 raceFakeNormal(vec3 localPos) {
    float e = 0.06;
    float h  = sin(localPos.x * 2.4 + localPos.z * 1.8) * 0.1;
    float hx = sin((localPos.x + e) * 2.4 + localPos.z * 1.8) * 0.1 - h;
    float hz = sin(localPos.x * 2.4 + (localPos.z + e) * 1.8) * 0.1 - h;
    return normalize(vec3(-hx, 1.0, -hz));
}

vec3 raceLighting(vec3 albedo, vec3 localPos) {
    vec3 n = raceFakeNormal(localPos);
    vec3 lightDir = normalize(vec3(0.42, 0.9, 0.28));
    float diff = 0.6 + 0.4 * max(dot(n, lightDir), 0.0);
    vec3 halfDir = normalize(lightDir + vec3(0.0, 0.12, 0.75));
    float spec = pow(max(dot(n, halfDir), 0.0), 22.0) * 0.06;
    float rim = pow(1.0 - max(dot(n, vec3(0.0, 1.0, 0.0)), 0.0), 3.0) * 0.04;
    return albedo * diff + vec3(spec) + albedo * rim;
}

vec3 sampleRaceTexture(int raceIdx, vec2 uv, vec3 localPos) {
    float noise = raceSoftNoise(uv);
    float panel = racePanel(uv, 2.8, 0.1);
    float seam = racePanel(uv, 5.5, 0.05);
    float rivet = raceRivet(uv, 4.0);
    float tint = 0.96 + 0.04 * noise;

    if (raceIdx == 0) {
        float grid = racePanel(uv, 3.2, 0.07);
        vec3 base = vec3(0.84 + 0.08 * grid, 0.86 + 0.06 * panel, 0.9 + 0.05 * seam);
        return mix(base, base * 1.04, rivet * 0.08) * tint;
    }
    if (raceIdx == 1) {
        float keel = smoothstep(0.42, 0.58, fract(uv.y * 2.4 + uv.x * 0.15));
        vec3 base = vec3(0.5 + 0.1 * panel, 0.42 + 0.08 * seam, 0.34 + 0.06 * noise);
        return mix(base, base * vec3(1.06, 0.94, 0.8), keel * 0.12) * tint;
    }
    if (raceIdx == 2) {
        float block = racePanel(uv, 2.2, 0.11);
        float wear = smoothstep(0.35, 0.65, raceHash(floor(uv * 4.0)));
        vec3 base = vec3(0.74 + 0.1 * block, 0.56 + 0.08 * panel, 0.52 + 0.06 * seam);
        return mix(base, base * 0.94, wear * 0.1 + rivet * 0.03) * tint;
    }
    if (raceIdx == 3) {
        float wave = 0.5 + 0.5 * sin(uv.x * 3.5 + uv.y * 2.5);
        vec3 base = vec3(0.64 + 0.1 * wave, 0.7 + 0.08 * panel, 0.76 + 0.06 * seam);
        return base * tint;
    }
    if (raceIdx == 4) {
        float frame = racePanel(uv, 3.5, 0.07);
        vec3 base = vec3(0.78 + 0.1 * frame, 0.68 + 0.1 * panel, 0.54 + 0.08 * seam);
        return base * tint;
    }
    if (raceIdx == 5) {
        float band = racePanel(uv, 2.6, 0.09);
        vec3 base = vec3(0.88 + 0.08 * band, 0.78 + 0.1 * panel, 0.5 + 0.12 * seam);
        return base * tint;
    }
    if (raceIdx == 6) {
        float rift = racePanel(uv, 4.0, 0.08);
        vec3 base = vec3(0.56 + 0.12 * rift, 0.6 + 0.08 * panel, 0.74 + 0.06 * seam);
        return base * tint;
    }
    float facet = racePanel(uv, 3.2, 0.08);
    vec3 frost = vec3(0.7 + 0.12 * facet, 0.82 + 0.08 * panel, 0.9 + 0.05 * seam);
    return frost * tint;
}";

    public const string TeamVisualGlsl = @"
float teamInsigniaMask(vec2 uv, vec3 localPos) {
    float spine = smoothstep(0.48, 0.52, fract(uv.y * 2.0 + 0.5));
    float band = smoothstep(0.35, 0.65, fract(uv.y * 1.2));
    float dorsal = smoothstep(0.06, 0.02, abs(localPos.x)) * band;
    return clamp(max(spine * 0.35, dorsal * 0.5), 0.0, 1.0);
}

vec3 applyTeamInsignia(vec3 baseColor, vec2 uv, vec3 localPos, vec3 teamTint) {
    float mask = teamInsigniaMask(uv, localPos);
    vec3 mark = teamTint * (0.85 + 0.15 * raceSoftNoise(uv * 2.0));
    return mix(baseColor, mark, mask * 0.75);
}

vec3 applyTeamHullAura(vec3 color, vec3 localPos, vec3 teamTint) {
    vec3 n = raceFakeNormal(localPos);
    vec3 viewBias = normalize(vec3(0.12, 1.0, 0.18));
    float rim = pow(1.0 - max(dot(n, viewBias), 0.0), 2.0);
    float crest = pow(max(localPos.y * 0.06 + 0.35, 0.0), 1.4) * 0.1;
    return color + teamTint * (rim * 0.3 + crest);
}";

    private const string SharedFragmentMain = @"
void main()
{
    vec3 color = fragColor;
    float alpha = 1.0;

    if (raceTextureIndex >= 0) {
        float uvScale = raceUvScale(raceTextureIndex);
        vec2 uv = vLocalPos.xz * uvScale;
        vec3 tex = sampleRaceTexture(raceTextureIndex, uv, vLocalPos);
        color = color * tex;
        color = applyTeamInsignia(color, uv, vLocalPos, teamTint);
        color = applyTeamHullAura(color, vLocalPos, teamTint);
        color = raceLighting(color, vLocalPos);
    } else if (overrideColor.a > 0.0 && overrideColor.a < 0.9) {
        float mask = dot(vVertexMask, vec3(0.333));
        color = overrideColor.rgb;
        alpha = overrideColor.a * mask;
    } else {
        if (overrideColor.a > 0.0)
            color = overrideColor.rgb;
        else
            color = raceLighting(color, vLocalPos);
    }
    outputColor = vec4(clamp(color, 0.0, 1.0), alpha);
}";

    public const string DesktopVertex = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;
uniform float pointSize;

out vec3 fragColor;
out vec3 vWorldPos;
out vec3 vLocalPos;
out vec3 vVertexMask;

void main()
{
    vec4 world = model * vec4(aPosition, 1.0);
    gl_Position = projection * view * world;
    gl_PointSize = max(pointSize, 1.0);
    vWorldPos = world.xyz;
    vLocalPos = aPosition;
    vVertexMask = aColor;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}";

    public const string DesktopFragment = @"
#version 330 core
in vec3 fragColor;
in vec3 vWorldPos;
in vec3 vLocalPos;
in vec3 vVertexMask;

uniform int raceTextureIndex;
uniform vec3 teamTint;
uniform vec4 overrideColor;

out vec4 outputColor;

" + RaceTextureGlsl + TeamVisualGlsl + SharedFragmentMain;

    public const string WebVertex = @"
#version 300 es
precision mediump float;
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;
uniform float pointSize;

out vec3 fragColor;
out vec3 vWorldPos;
out vec3 vLocalPos;
out vec3 vVertexMask;

void main()
{
    vec4 world = model * vec4(aPosition, 1.0);
    gl_Position = projection * view * world;
    gl_PointSize = max(pointSize, 1.0);
    vWorldPos = world.xyz;
    vLocalPos = aPosition;
    vVertexMask = aColor;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}";

    public const string WebFragment = @"
#version 300 es
precision mediump float;
in vec3 fragColor;
in vec3 vWorldPos;
in vec3 vLocalPos;
in vec3 vVertexMask;

uniform int raceTextureIndex;
uniform vec3 teamTint;
uniform vec4 overrideColor;

out vec4 outputColor;

" + RaceTextureGlsl + TeamVisualGlsl + SharedFragmentMain;
}