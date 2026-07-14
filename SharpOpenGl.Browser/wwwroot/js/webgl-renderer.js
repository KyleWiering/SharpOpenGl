// WebGL2 backend matching desktop GameShaders (projection/view/model/overrideColor/race textures).
window.sharpGl = (() => {
    let gl = null;
    let program = null;
    let locProjection = null;
    let locView = null;
    let locModel = null;
    let locColor = null;
    let locPointSize = null;
    let locRaceTextureIndex = null;
    let locTeamTint = null;
    let locComponentTextureIndex = null;
    let meshes = {};
    let nextMeshId = 1;

    const raceTextureGlsl = `float raceUvScale(int raceIdx) {
    float scales[8] = float[8](0.14, 0.26, 0.16, 0.13, 0.15, 0.12, 0.14, 0.15);
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
    float diff = 0.52 + 0.48 * max(dot(n, lightDir), 0.0);
    vec3 halfDir = normalize(lightDir + vec3(0.0, 0.12, 0.75));
    float spec = pow(max(dot(n, halfDir), 0.0), 18.0) * 0.10;
    float rim = pow(1.0 - max(dot(n, vec3(0.0, 1.0, 0.0)), 0.0), 3.0) * 0.04;
    return albedo * diff + vec3(spec) + albedo * rim;
}


float raceTechHex(vec2 uv, float scale) {
    vec2 g = fract(uv * scale);
    vec2 id = floor(uv * scale);
    vec2 p = g - 0.5;
    p.x *= 0.866025;
    float hexDist = length(p);
    float hex = smoothstep(0.42, 0.36, hexDist);
    float inner = smoothstep(0.28, 0.22, hexDist);
    float stagger = mod(id.x + id.y, 2.0) * 0.5;
    float cell = racePanel(vec2(g.x + stagger, g.y), scale * 0.5, 0.08);
    return max(hex * (1.0 - inner * 0.4), cell * 0.35);
}

float raceCircuit(vec2 uv, float scale) {
    vec2 g = fract(uv * scale);
    float hLine = smoothstep(0.04, 0.0, abs(g.y - 0.5));
    float vLine = smoothstep(0.04, 0.0, abs(g.x - 0.5));
    float node = smoothstep(0.18, 0.08, length(g - 0.5)) * step(0.65, raceHash(floor(uv * scale)));
    float branch = smoothstep(0.03, 0.0, abs(fract(uv.x * scale * 2.0 + uv.y * scale) - 0.5));
    return clamp(max(hLine, vLine) * 0.7 + node * 0.9 + branch * 0.4, 0.0, 1.0);
}

float raceCarbonWeave(vec2 uv, float scale) {
    float warp = sin(uv.x * scale * 3.14159 + uv.y * scale * 1.8) * 0.5 + 0.5;
    float weft = sin(uv.y * scale * 3.14159 + uv.x * scale * 1.8) * 0.5 + 0.5;
    float weave = warp * weft;
    float fiber = racePanel(uv, scale * 2.4, 0.06);
    return weave * 0.55 + fiber * 0.45;
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
        float macroPanel = racePanel(uv, 2.2, 0.11);
        float microPanel = racePanel(uv, 7.0, 0.045);
        float seam = racePanel(uv, 4.2, 0.04);
        float rivet = raceRivet(uv, 5.8);
        float scratch = raceSoftNoise(uv * 3.5) * racePanel(uv, 14.0, 0.025);
        float wear = raceHash(floor(uv * 2.6));
        float streak = smoothstep(0.32, 0.68, fract(uv.y * 1.6 + uv.x * 0.22));
        float oil = smoothstep(0.15, 0.0, abs(localPos.y)) * 0.25;
        vec3 gunmetal = vec3(0.30 + 0.14 * macroPanel, 0.33 + 0.11 * seam, 0.38 + 0.09 * microPanel);
        vec3 worn = vec3(0.20 + 0.10 * wear, 0.22 + 0.08 * wear, 0.26 + 0.06 * wear);
        vec3 base = mix(gunmetal, worn, streak * 0.50 + scratch * 0.35 + oil);
        base = mix(base, base * vec3(1.10, 1.04, 0.98), rivet * 0.08);
        base = mix(base, base * 0.88, racePanel(uv, 1.4, 0.14) * 0.48);
        return base * (0.90 + 0.10 * raceSoftNoise(uv)) * tint;
    }
    if (raceIdx == 2) {
        float insulation = racePanel(uv, 2.2, 0.11);
        float girderX = racePanel(uv, 4.8, 0.04);
        float girderY = racePanel(vec2(uv.y, uv.x), 4.8, 0.04);
        float lattice = max(girderX, girderY);
        float solarZone = smoothstep(0.12, 0.5, localPos.y);
        float solarGrid = racePanel(uv, 7.5, 0.035) * solarZone;
        float radiator = smoothstep(0.28, 0.06, abs(localPos.x));
        float wear = smoothstep(0.38, 0.62, raceHash(floor(uv * 3.2)));
        vec3 base = vec3(0.90 + 0.05 * insulation, 0.89 + 0.04 * insulation, 0.87 + 0.03 * insulation);
        vec3 gold = vec3(0.78 + 0.1 * solarGrid, 0.65 + 0.08 * solarGrid, 0.32 + 0.06 * solarGrid);
        vec3 radiatorBand = vec3(0.2 + 0.05 * lattice, 0.19 + 0.04 * lattice, 0.17 + 0.03 * lattice);
        vec3 aluminum = vec3(0.84 + 0.05 * lattice, 0.83 + 0.04 * lattice, 0.8 + 0.03 * lattice);
        base = mix(base, gold, solarGrid * 0.6);
        base = mix(base, radiatorBand, radiator * 0.7);
        base = mix(base, aluminum, lattice * 0.3);
        return mix(base, base * 0.95, rivet * 0.08 + wear * 0.05) * tint;
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
}`;

    const componentTextureGlsl = `float componentUvScale(int kind) {
    float scales[3] = float[3](0.22, 0.28, 0.18);
    return scales[clamp(kind, 0, 2)];
}

vec3 sampleComponentTexture(int kind, vec2 uv, vec3 localPos) {
    float noise = raceSoftNoise(uv);
    float panel = racePanel(uv, 3.4, 0.09);
    float seam = racePanel(uv, 6.0, 0.045);
    float tint = 0.94 + 0.06 * noise;

    if (kind == 0) {
        float rings = racePanel(uv, 1.8, 0.06);
        float core = smoothstep(0.35, 0.0, length(localPos.xz) * 0.55);
        float vent = racePanel(vec2(uv.y, uv.x), 5.0, 0.04);
        float nozzle = racePanel(uv, 2.4, 0.05) * smoothstep(0.0, 0.25, -localPos.z);
        vec3 housing = vec3(0.22 + 0.08 * panel, 0.24 + 0.06 * seam, 0.28 + 0.05 * noise);
        vec3 heat = vec3(0.95 + 0.05 * core, 0.42 + 0.35 * core + 0.08 * nozzle, 0.08 + 0.12 * rings);
        vec3 base = mix(housing, heat, core * 0.82 + rings * 0.18 + nozzle * 0.12);
        return mix(base, base * 1.08, vent * 0.12) * tint;
    }
    if (kind == 1) {
        float barrel = racePanel(uv, 4.2, 0.05);
        float emitter = smoothstep(0.42, 0.48, fract(uv.x * 3.0 + uv.y * 0.5));
        float housing = racePanel(uv, 2.6, 0.08);
        float recess = racePanel(vec2(uv.x * 1.4, uv.y), 3.2, 0.06);
        vec3 metal = vec3(0.38 + 0.1 * housing, 0.4 + 0.08 * panel, 0.44 + 0.06 * seam);
        vec3 glow = vec3(0.55 + 0.25 * emitter, 0.78 + 0.15 * barrel, 1.0);
        vec3 base = mix(metal, glow, emitter * 0.55 + barrel * 0.2 + recess * 0.15);
        return base * tint;
    }
    float hex = racePanel(uv, 3.8, 0.07);
    float hex2 = racePanel(uv * 1.6, 5.2, 0.05);
    float pulse = 0.55 + 0.45 * sin(length(localPos.xz) * 4.5 - localPos.y * 2.0);
    float arc = smoothstep(0.3, 0.0, abs(fract(uv.y * 2.2 + 0.25) - 0.5));
    float dome = smoothstep(0.5, 0.0, length(localPos.xz) * 0.4 + localPos.y * 0.15);
    vec3 frame = vec3(0.28 + 0.08 * hex, 0.34 + 0.1 * panel, 0.42 + 0.08 * seam);
    vec3 energy = vec3(0.25 + 0.2 * pulse + 0.1 * dome, 0.72 + 0.2 * arc + 0.08 * hex2, 0.95 + 0.05 * hex);
    vec3 base = mix(frame, energy, pulse * 0.45 + arc * 0.35 + dome * 0.2);
    return base * tint;
}

vec3 applyComponentZoneBlends(vec3 color, vec2 uv, vec3 localPos, vec3 vertexCol) {
    float lum = dot(vertexCol, vec3(0.333));
    float eng = smoothstep(0.44, 0.50, lum) * (1.0 - smoothstep(0.52, 0.56, lum));
    float wpn = smoothstep(0.33, 0.37, lum) * (1.0 - smoothstep(0.40, 0.44, lum));
    float shd = smoothstep(0.28, 0.32, lum) * (1.0 - smoothstep(0.33, 0.37, lum));
    if (eng > 0.01) {
        vec3 t = sampleComponentTexture(0, uv * 1.35, localPos);
        color = mix(color, color * t, eng * 0.9);
    }
    if (wpn > 0.01) {
        vec3 t = sampleComponentTexture(1, uv * 1.5, localPos);
        color = mix(color, color * t, wpn * 0.85);
    }
    if (shd > 0.01) {
        vec3 t = sampleComponentTexture(2, uv * 1.6, localPos);
        color = mix(color, color * t, shd * 0.88);
    }
    return color;
}`;

    const teamVisualGlsl = `float teamInsigniaMask(vec2 uv, vec3 localPos) {
    float spine = smoothstep(0.48, 0.52, fract(uv.y * 2.0 + 0.5));
    float band = smoothstep(0.35, 0.65, fract(uv.y * 1.2));
    float dorsal = smoothstep(0.06, 0.02, abs(localPos.x)) * band;
    return clamp(max(spine * 0.35, dorsal * 0.5), 0.0, 1.0);
}

vec3 applyTeamInsignia(vec3 baseColor, vec2 uv, vec3 localPos, vec3 teamTint, int raceTextureIndex) {
    float mask = teamInsigniaMask(uv, localPos);
    vec3 mark = teamTint * (0.85 + 0.15 * raceSoftNoise(uv * 2.0));
    float insigniaMixByRace[8] = float[8](0.20, 0.18, 0.12, 0.16, 0.18, 0.20, 0.15, 0.14);
    float insigniaMix = insigniaMixByRace[clamp(raceTextureIndex, 0, 7)];
    return mix(baseColor, mark, mask * insigniaMix);
}

vec3 applyTeamHullAura(vec3 color, vec3 localPos, vec3 teamTint) {
    vec3 n = raceFakeNormal(localPos);
    vec3 viewBias = normalize(vec3(0.12, 1.0, 0.18));
    float rim = pow(1.0 - max(dot(n, viewBias), 0.0), 2.0);
    float crest = pow(max(localPos.y * 0.06 + 0.35, 0.0), 1.4) * 0.1;
    return color + teamTint * (rim * 0.3 + crest);
}`;

    const vertSrc = `#version 300 es
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
void main() {
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
}`;

    const fragSrc = `#version 300 es
precision mediump float;
in vec3 fragColor;
in vec3 vWorldPos;
in vec3 vLocalPos;
in vec3 vVertexMask;
uniform int raceTextureIndex;
uniform int componentTextureIndex;
uniform vec3 teamTint;
uniform vec4 overrideColor;
out vec4 outputColor;
${raceTextureGlsl}
${componentTextureGlsl}
${teamVisualGlsl}
void main() {
    vec3 color = fragColor;
    float alpha = 1.0;
    if (componentTextureIndex >= 0) {
        float uvScale = componentUvScale(componentTextureIndex);
        vec2 uv = vLocalPos.xz * uvScale;
        vec3 tex = sampleComponentTexture(componentTextureIndex, uv, vLocalPos);
        vec3 base = overrideColor.a > 0.0 ? overrideColor.rgb : fragColor;
        color = base * tex;
        color = raceLighting(color, vLocalPos);
    } else if (raceTextureIndex >= 0) {
        float uvScale = raceUvScale(raceTextureIndex);
        vec2 uv = vLocalPos.xz * uvScale;
        vec3 tex = sampleRaceTexture(raceTextureIndex, uv, vLocalPos);
        if (raceTextureIndex == 1) {
            color = mix(color * 0.10, color * tex, 0.97) + tex * 0.08;
        } else {
            color = color * tex;
        }
        color = applyComponentZoneBlends(color, uv, vLocalPos, fragColor);
        color = applyTeamInsignia(color, uv, vLocalPos, teamTint, raceTextureIndex);
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
}`;

    function compile(type, src) {
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS))
            throw new Error(gl.getShaderInfoLog(s) || 'shader compile failed');
        return s;
    }

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;
        gl = canvas.getContext('webgl2', { alpha: false, antialias: true, premultipliedAlpha: false });
        if (!gl) return false;

        const vs = compile(gl.VERTEX_SHADER, vertSrc);
        const fs = compile(gl.FRAGMENT_SHADER, fragSrc);
        program = gl.createProgram();
        gl.attachShader(program, vs);
        gl.attachShader(program, fs);
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS))
            throw new Error(gl.getProgramInfoLog(program) || 'program link failed');

        locProjection = gl.getUniformLocation(program, 'projection');
        locView = gl.getUniformLocation(program, 'view');
        locModel = gl.getUniformLocation(program, 'model');
        locColor = gl.getUniformLocation(program, 'overrideColor');
        locPointSize = gl.getUniformLocation(program, 'pointSize');
        locRaceTextureIndex = gl.getUniformLocation(program, 'raceTextureIndex');
        locTeamTint = gl.getUniformLocation(program, 'teamTint');
        locComponentTextureIndex = gl.getUniformLocation(program, 'componentTextureIndex');
        gl.uniform1f(locPointSize, 2.0);
        gl.uniform1i(locRaceTextureIndex, -1);
        gl.uniform1i(locComponentTextureIndex, -1);
        gl.uniform3fv(locTeamTint, [1, 1, 1]);

        gl.enable(gl.DEPTH_TEST);
        gl.depthFunc(gl.LEQUAL);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

        resize(canvas.clientWidth, canvas.clientHeight);
        return true;
    }

    function resize(w, h) {
        const canvas = gl?.canvas;
        if (!canvas) return;
        const width = Math.max(1, w | 0);
        const height = Math.max(1, h | 0);
        canvas.width = width;
        canvas.height = height;
        gl.viewport(0, 0, width, height);
    }

    function clear(r, g, b) {
        if (!gl) return;
        gl.clearColor(r, g, b, 1);
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    }

    function uploadMesh(vertices) {
        if (!gl || !vertices || vertices.length === 0) return 0;

        const vao = gl.createVertexArray();
        gl.bindVertexArray(vao);

        const vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertices), gl.STATIC_DRAW);

        const stride = 6 * 4;
        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, stride, 0);
        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(1, 3, gl.FLOAT, false, stride, 12);
        gl.enableVertexAttribArray(1);

        gl.bindVertexArray(null);

        const id = nextMeshId++;
        meshes[id] = { vao, vbo };
        return id;
    }

    function beginFrame(projection, view) {
        if (!gl) return;
        gl.useProgram(program);
        gl.uniformMatrix4fv(locProjection, false, projection);
        gl.uniformMatrix4fv(locView, false, view);
    }

    function drawMesh(meshId, vertexCount, model, color, primitiveType, raceTextureIndex, teamTint, componentTextureIndex) {
        if (!gl || meshId <= 0 || vertexCount <= 0) return;
        const mesh = meshes[meshId];
        if (!mesh) return;

        gl.uniformMatrix4fv(locModel, false, model);
        gl.uniform4fv(locColor, color);
        gl.uniform1i(locRaceTextureIndex, raceTextureIndex ?? -1);
        gl.uniform1i(locComponentTextureIndex, componentTextureIndex ?? -1);
        gl.uniform3fv(locTeamTint, teamTint ?? [1, 1, 1]);

        gl.bindVertexArray(mesh.vao);
        const mode = primitiveType === 1 ? gl.LINES
            : primitiveType === 0 ? gl.POINTS
            : primitiveType === 3 ? gl.LINE_STRIP
            : gl.TRIANGLES;
        gl.drawArrays(mode, 0, vertexCount);
        gl.bindVertexArray(null);
    }

    function drawPoints(meshId, vertices, pointCount, model, pointSize) {
        if (!gl || meshId <= 0 || pointCount <= 0) return;
        const mesh = meshes[meshId];
        if (!mesh) return;

        gl.uniformMatrix4fv(locModel, false, model);
        gl.uniform4fv(locColor, [0, 0, 0, 0]);
        gl.uniform1f(locPointSize, pointSize || 5.0);
        gl.uniform1i(locRaceTextureIndex, -1);
        gl.uniform1i(locComponentTextureIndex, -1);

        gl.bindBuffer(gl.ARRAY_BUFFER, mesh.vbo);
        gl.bufferSubData(gl.ARRAY_BUFFER, 0, new Float32Array(vertices));

        gl.bindVertexArray(mesh.vao);
        gl.drawArrays(gl.POINTS, 0, pointCount);
        gl.bindVertexArray(null);
    }

    function drawLineStrip(meshId, vertices, vertexCount, model, color) {
        if (!gl || meshId <= 0 || vertexCount <= 0) return;
        const mesh = meshes[meshId];
        if (!mesh) return;

        gl.uniformMatrix4fv(locModel, false, model);
        gl.uniform4fv(locColor, color ?? [0.2, 0.85, 0.55, 0.45]);
        gl.uniform1i(locRaceTextureIndex, -1);
        gl.uniform1i(locComponentTextureIndex, -1);

        gl.bindBuffer(gl.ARRAY_BUFFER, mesh.vbo);
        gl.bufferSubData(gl.ARRAY_BUFFER, 0, new Float32Array(vertices));

        gl.bindVertexArray(mesh.vao);
        gl.drawArrays(gl.LINE_STRIP, 0, vertexCount);
        gl.bindVertexArray(null);
    }

    return { init, resize, clear, uploadMesh, beginFrame, drawMesh, drawPoints, drawLineStrip };
})();
