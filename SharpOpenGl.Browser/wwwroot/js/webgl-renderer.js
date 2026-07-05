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
    let meshes = {};
    let nextMeshId = 1;

    const raceTextureGlsl = `float raceUvScale(int raceIdx) {
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
        float girderX = racePanel(uv, 4.8, 0.04);
        float girderY = racePanel(vec2(uv.y, uv.x), 4.8, 0.04);
        float lattice = max(girderX, girderY);
        float module = racePanel(uv, 2.0, 0.12);
        float wear = smoothstep(0.38, 0.62, raceHash(floor(uv * 3.2)));
        vec3 base = vec3(0.76 + 0.1 * module, 0.79 + 0.08 * lattice, 0.84 + 0.06 * seam);
        return mix(base, base * vec3(0.94, 0.96, 1.0), rivet * 0.14 + lattice * 0.05 + wear * 0.06) * tint;
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
`;

    const teamVisualGlsl = `float teamInsigniaMask(vec2 uv, vec3 localPos) {
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
`;

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
uniform vec3 teamTint;
uniform vec4 overrideColor;
out vec4 outputColor;
${raceTextureGlsl}
${teamVisualGlsl}
void main() {
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
        gl.uniform1f(locPointSize, 2.0);
        gl.uniform1i(locRaceTextureIndex, -1);
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

    function drawMesh(meshId, vertexCount, model, color, primitiveType, raceTextureIndex, teamTint) {
        if (!gl || meshId <= 0 || vertexCount <= 0) return;
        const mesh = meshes[meshId];
        if (!mesh) return;

        gl.uniformMatrix4fv(locModel, false, model);
        gl.uniform4fv(locColor, color);
        gl.uniform1i(locRaceTextureIndex, raceTextureIndex ?? -1);
        gl.uniform3fv(locTeamTint, teamTint ?? [1, 1, 1]);

        gl.bindVertexArray(mesh.vao);
        const mode = primitiveType === 1 ? gl.LINES
            : primitiveType === 0 ? gl.POINTS
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

        gl.bindBuffer(gl.ARRAY_BUFFER, mesh.vbo);
        gl.bufferSubData(gl.ARRAY_BUFFER, 0, new Float32Array(vertices));

        gl.bindVertexArray(mesh.vao);
        gl.drawArrays(gl.POINTS, 0, pointCount);
        gl.bindVertexArray(null);
    }

    return { init, resize, clear, uploadMesh, beginFrame, drawMesh, drawPoints };
})();