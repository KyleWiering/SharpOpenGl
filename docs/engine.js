/**
 * SharpOpenGL Engine - WebGL2 Port
 * 
 * This is a JavaScript/WebGL2 port of the C# OpenTK engine,
 * rendering the same scene: a starfield + rotating colored triangles.
 */

// ============================================================
// Matrix math utilities (minimal gl-matrix style)
// ============================================================
const Mat4 = {
    create() { const m = new Float32Array(16); m[0]=m[5]=m[10]=m[15]=1; return m; },
    identity(out) { out.fill(0); out[0]=out[5]=out[10]=out[15]=1; return out; },

    perspective(out, fovY, aspect, near, far) {
        const f = 1.0 / Math.tan(fovY / 2);
        out.fill(0);
        out[0] = f / aspect;
        out[5] = f;
        out[10] = (far + near) / (near - far);
        out[11] = -1;
        out[14] = (2 * far * near) / (near - far);
        return out;
    },

    lookAt(out, eye, center, up) {
        let x0, x1, x2, y0, y1, y2, z0, z1, z2, len;
        z0 = eye[0] - center[0]; z1 = eye[1] - center[1]; z2 = eye[2] - center[2];
        len = 1 / Math.sqrt(z0*z0 + z1*z1 + z2*z2);
        z0 *= len; z1 *= len; z2 *= len;
        x0 = up[1]*z2 - up[2]*z1; x1 = up[2]*z0 - up[0]*z2; x2 = up[0]*z1 - up[1]*z0;
        len = Math.sqrt(x0*x0 + x1*x1 + x2*x2);
        if (len) { len = 1/len; x0*=len; x1*=len; x2*=len; } else { x0=x1=x2=0; }
        y0 = z1*x2 - z2*x1; y1 = z2*x0 - z0*x2; y2 = z0*x1 - z1*x0;
        len = Math.sqrt(y0*y0 + y1*y1 + y2*y2);
        if (len) { len = 1/len; y0*=len; y1*=len; y2*=len; } else { y0=y1=y2=0; }
        out[0]=x0; out[1]=y0; out[2]=z0; out[3]=0;
        out[4]=x1; out[5]=y1; out[6]=z1; out[7]=0;
        out[8]=x2; out[9]=y2; out[10]=z2; out[11]=0;
        out[12]=-(x0*eye[0]+x1*eye[1]+x2*eye[2]);
        out[13]=-(y0*eye[0]+y1*eye[1]+y2*eye[2]);
        out[14]=-(z0*eye[0]+z1*eye[1]+z2*eye[2]);
        out[15]=1;
        return out;
    },

    rotateZ(out, a, rad) {
        const s = Math.sin(rad), c = Math.cos(rad);
        Mat4.identity(out);
        out[0] = c; out[1] = s;
        out[4] = -s; out[5] = c;
        return out;
    }
};

function vec3Normalize(v) {
    const len = Math.sqrt(v[0]*v[0] + v[1]*v[1] + v[2]*v[2]);
    if (len > 0) { v[0]/=len; v[1]/=len; v[2]/=len; }
    return v;
}

function vec3Cross(out, a, b) {
    out[0] = a[1]*b[2] - a[2]*b[1];
    out[1] = a[2]*b[0] - a[0]*b[2];
    out[2] = a[0]*b[1] - a[1]*b[0];
    return out;
}

// ============================================================
// Camera
// ============================================================
class Camera {
    constructor() {
        this.position = [0, 0, 5];
        this.forward = [0, 0, -1];
        this.right = [1, 0, 0];
        this.up = [0, 1, 0];
    }

    getViewMatrix(out) {
        const center = [
            this.position[0] + this.forward[0],
            this.position[1] + this.forward[1],
            this.position[2] + this.forward[2]
        ];
        return Mat4.lookAt(out, this.position, center, this.up);
    }

    moveXAxis(dist) {
        this.position[0] += this.right[0] * dist;
        this.position[1] += this.right[1] * dist;
        this.position[2] += this.right[2] * dist;
    }

    moveYAxis(dist) {
        this.position[0] += this.up[0] * dist;
        this.position[1] += this.up[1] * dist;
        this.position[2] += this.up[2] * dist;
    }

    moveZAxis(dist) {
        this.position[0] += this.forward[0] * dist;
        this.position[1] += this.forward[1] * dist;
        this.position[2] += this.forward[2] * dist;
    }

    rotateX(angle) {
        const c = Math.cos(angle), s = Math.sin(angle);
        this.forward = vec3Normalize([
            this.forward[0] * c + this.up[0] * s,
            this.forward[1] * c + this.up[1] * s,
            this.forward[2] * c + this.up[2] * s
        ]);
        const cross = [0, 0, 0];
        vec3Cross(cross, this.forward, this.right);
        this.up = vec3Normalize([-cross[0], -cross[1], -cross[2]]);
    }

    rotateY(angle) {
        const c = Math.cos(angle), s = Math.sin(angle);
        this.forward = vec3Normalize([
            this.forward[0] * c - this.right[0] * s,
            this.forward[1] * c - this.right[1] * s,
            this.forward[2] * c - this.right[2] * s
        ]);
        const cross = [0, 0, 0];
        vec3Cross(cross, this.forward, this.up);
        this.right = vec3Normalize(cross);
    }
}

// ============================================================
// Input Handler
// ============================================================
class InputHandler {
    constructor() {
        this.keys = {};
        this.axisMovement = [0, 0, 0];
        this.axisRotation = [0, 0, 0];
        this.active = false;

        document.addEventListener('keydown', (e) => { this.keys[e.code] = true; e.preventDefault(); });
        document.addEventListener('keyup', (e) => { this.keys[e.code] = false; e.preventDefault(); });

        const canvas = document.getElementById('glcanvas');
        canvas.addEventListener('click', () => { this.active = true; });
        canvas.addEventListener('blur', () => { this.active = false; });
        canvas.setAttribute('tabindex', '0');
    }

    update() {
        let mx = 0, my = 0, mz = 0, ry = 0;
        if (this.keys['KeyW']) mz = -1;
        if (this.keys['KeyS']) mz = 1;
        if (this.keys['KeyQ']) mx = -1;
        if (this.keys['KeyE']) mx = 1;
        if (this.keys['KeyZ']) my = 1;
        if (this.keys['KeyX']) my = -1;
        if (this.keys['KeyA']) ry = 1;
        if (this.keys['KeyD']) ry = -1;
        this.axisMovement = [mx, my, mz];
        this.axisRotation = [0, ry, 0];
    }
}

// ============================================================
// Starfield
// ============================================================
class Spacefield {
    constructor(gl) { this.gl = gl; }

    initialize() {
        const gl = this.gl;
        // Seeded random (simple LCG for reproducibility matching C# Random(42))
        let seed = 42;
        const rand = () => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed; };
        const randNext = (max) => rand() % max;

        const starCount = 1000;
        const vertices = new Float32Array(starCount * 6);

        for (let i = 0; i < starCount; i++) {
            const off = i * 6;
            vertices[off + 0] = randNext(1000) - 500;
            vertices[off + 1] = randNext(1000) - 500;
            vertices[off + 2] = randNext(1000) - 500;
            vertices[off + 3] = (randNext(5) / 5.0) + 0.5;
            vertices[off + 4] = (randNext(5) / 5.0) + 0.5;
            vertices[off + 5] = (randNext(5) / 5.0) + 0.5;
        }

        this.vertexCount = starCount;
        this.vao = gl.createVertexArray();
        gl.bindVertexArray(this.vao);

        const vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
        gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 24, 0);
        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 24, 12);
        gl.enableVertexAttribArray(1);

        gl.bindVertexArray(null);
    }

    render(uniforms) {
        const gl = this.gl;
        const model = Mat4.create();
        gl.uniformMatrix4fv(uniforms.model, false, model);
        gl.uniform4f(uniforms.color, 0, 0, 0, 0);
        gl.bindVertexArray(this.vao);
        gl.drawArrays(gl.POINTS, 0, this.vertexCount);
        gl.bindVertexArray(null);
    }
}

// ============================================================
// Rotating Model
// ============================================================
class RotatingModel {
    constructor(gl) { this.gl = gl; this.rotation = 0; }

    initialize() {
        const gl = this.gl;
        const vertices = new Float32Array([
            // Triangle 1
            -1, -1, 0,   1, 0, 0,
             1, -1, 0,   0, 1, 0,
             0,  1, 0,   0, 0, 1,
            // Triangle 2
             0,  1, 0,   0, 0, 1,
             1, -1, 0,   0, 1, 0,
             2,  1, 0,   1, 1, 0,
            // Triangle 3
            -2,  1, 0,   1, 0, 1,
            -1, -1, 0,   1, 0, 0,
             0,  1, 0,   0, 0, 1,
        ]);

        this.vertexCount = 9;
        this.vao = gl.createVertexArray();
        gl.bindVertexArray(this.vao);

        const vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
        gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 24, 0);
        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 24, 12);
        gl.enableVertexAttribArray(1);

        gl.bindVertexArray(null);
    }

    render(uniforms) {
        const gl = this.gl;
        const model = Mat4.create();
        Mat4.rotateZ(model, model, this.rotation * Math.PI / 180);
        gl.uniformMatrix4fv(uniforms.model, false, model);
        gl.uniform4f(uniforms.color, 0, 0, 0, 0);
        gl.bindVertexArray(this.vao);
        gl.drawArrays(gl.TRIANGLES, 0, this.vertexCount);
        gl.bindVertexArray(null);
    }

    update(dt) {
        this.rotation += dt * 45.0; // 45 degrees per second
    }
}

// ============================================================
// Main Engine
// ============================================================
(function main() {
    const canvas = document.getElementById('glcanvas');
    const gl = canvas.getContext('webgl2', { antialias: true });
    if (!gl) {
        document.body.innerHTML = '<h1 style="color:red;padding:40px;">WebGL2 is not supported in this browser.</h1>';
        return;
    }

    // Resize canvas to fill window
    function resize() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        gl.viewport(0, 0, canvas.width, canvas.height);
    }
    window.addEventListener('resize', resize);
    resize();

    // Compile shaders
    const vertSrc = `#version 300 es
precision highp float;
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;

out vec3 fragColor;

void main() {
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    gl_PointSize = 2.0;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}`;

    const fragSrc = `#version 300 es
precision highp float;
in vec3 fragColor;
out vec4 outputColor;

void main() {
    outputColor = vec4(fragColor, 1.0);
}`;

    function compileShader(type, src) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, src);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            console.error('Shader compile error:', gl.getShaderInfoLog(shader));
            gl.deleteShader(shader);
            return null;
        }
        return shader;
    }

    const vs = compileShader(gl.VERTEX_SHADER, vertSrc);
    const fs = compileShader(gl.FRAGMENT_SHADER, fragSrc);
    const program = gl.createProgram();
    gl.attachShader(program, vs);
    gl.attachShader(program, fs);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        console.error('Program link error:', gl.getProgramInfoLog(program));
        return;
    }

    const uniforms = {
        projection: gl.getUniformLocation(program, 'projection'),
        view: gl.getUniformLocation(program, 'view'),
        model: gl.getUniformLocation(program, 'model'),
        color: gl.getUniformLocation(program, 'overrideColor')
    };

    // Initialize scene
    const camera = new Camera();
    const input = new InputHandler();
    const spacefield = new Spacefield(gl);
    spacefield.initialize();
    const model = new RotatingModel(gl);
    model.initialize();

    // GL state
    gl.clearColor(0.0, 0.0, 0.05, 1.0);
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LESS);

    // FPS counter
    let frameCount = 0;
    let lastFpsTime = performance.now();
    const fpsEl = document.getElementById('fps');

    // Render loop
    const projMatrix = Mat4.create();
    const viewMatrix = Mat4.create();
    let lastTime = performance.now();
    const MOVE_SPEED = 50;

    function frame(now) {
        const dt = (now - lastTime) / 1000;
        lastTime = now;

        // FPS
        frameCount++;
        if (now - lastFpsTime >= 1000) {
            fpsEl.textContent = `FPS: ${frameCount}`;
            frameCount = 0;
            lastFpsTime = now;
        }

        // Input
        input.update();
        camera.moveXAxis(input.axisMovement[0] * dt * MOVE_SPEED);
        camera.moveYAxis(input.axisMovement[1] * dt * MOVE_SPEED);
        camera.moveZAxis(input.axisMovement[2] * dt * MOVE_SPEED);
        camera.rotateY(input.axisRotation[1] * dt);

        // Update
        model.update(dt);

        // Render
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

        const aspect = canvas.width / canvas.height;
        Mat4.perspective(projMatrix, 45 * Math.PI / 180, aspect, 0.1, 10000);
        camera.getViewMatrix(viewMatrix);

        gl.useProgram(program);
        gl.uniformMatrix4fv(uniforms.projection, false, projMatrix);
        gl.uniformMatrix4fv(uniforms.view, false, viewMatrix);

        spacefield.render(uniforms);
        model.render(uniforms);

        requestAnimationFrame(frame);
    }

    requestAnimationFrame(frame);
    console.log('SharpOpenGL WebGL2 Engine initialized.');
})();

// ============================================================
// WebAudio Manager (Phase 10 — Audio)
// Mirrors the C# IAudioManager interface for the browser build.
// ============================================================

/**
 * @typedef {'WeaponFire'|'WeaponLaunch'|'Explosion'|'ShieldHit'|
 *           'UnitMoveAck'|'UnitAttackAck'|'ResourceCollected'|
 *           'UIClick'|'UIHover'|'MissionComplete'|'MissionFail'|
 *           'BuildingPlaced'|'EngineIdle'} AudioEventType
 */

/**
 * Procedural placeholder sound generator using the Web Audio API.
 * Mirrors PlaceholderSoundGenerator.cs logic.
 */
class WebAudioPlaceholderGenerator {
    /**
     * @param {AudioContext} ctx
     */
    constructor(ctx) {
        this._ctx = ctx;
    }

    /** Generate a sine-wave tone buffer. */
    tone(frequency, duration, attack = 0.01, release = 0.05) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const attackSamples  = Math.floor(sr * attack);
        const releaseSamples = Math.floor(sr * release);
        for (let i = 0; i < data.length; i++) {
            const t = i / sr;
            let env;
            if (i < attackSamples)             env = i / attackSamples;
            else if (i >= data.length - releaseSamples) env = (data.length - i) / releaseSamples;
            else                               env = 1;
            data[i] = Math.sin(2 * Math.PI * frequency * t) * env * 0.7;
        }
        return buf;
    }

    /** Generate a frequency-swept tone (laser effect). */
    sweep(startHz, endHz, duration, release = 0.03) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const releaseSamples = Math.floor(sr * release);
        let phase = 0;
        for (let i = 0; i < data.length; i++) {
            const t    = i / data.length;
            const freq = startHz + (endHz - startHz) * t;
            phase += 2 * Math.PI * freq / sr;
            const env = i >= data.length - releaseSamples ? (data.length - i) / releaseSamples : 1;
            data[i] = Math.sin(phase) * env * 0.6;
        }
        return buf;
    }

    /** Generate white noise (explosion / static). */
    noise(duration, release = 0.15) {
        const sr = this._ctx.sampleRate;
        const buf = this._ctx.createBuffer(1, Math.floor(sr * duration), sr);
        const data = buf.getChannelData(0);
        const releaseSamples = Math.floor(sr * release);
        for (let i = 0; i < data.length; i++) {
            const env = i >= data.length - releaseSamples ? (data.length - i) / releaseSamples : 1;
            data[i] = (Math.random() * 2 - 1) * env * 0.5;
        }
        return buf;
    }

    /** @param {AudioEventType} type @returns {AudioBuffer} */
    getPlaceholder(type) {
        switch (type) {
            case 'WeaponFire':        return this.sweep(800, 200, 0.12);
            case 'WeaponLaunch':      return this.sweep(400, 150, 0.18);
            case 'Explosion':         return this.noise(0.45, 0.20);
            case 'ShieldHit':         return this.sweep(1200, 600, 0.08);
            case 'UnitMoveAck':       return this.tone(660, 0.05);
            case 'UnitAttackAck':     return this.tone(440, 0.05);
            case 'ResourceCollected': return this.sweep(400, 800, 0.10);
            case 'UIClick':           return this.tone(1000, 0.04, 0.005, 0.02);
            case 'UIHover':           return this.tone(800, 0.03, 0.005, 0.015);
            case 'MissionComplete':   return this.tone(523, 0.6, 0.02, 0.3);
            case 'MissionFail':       return this.sweep(400, 100, 0.8, 0.4);
            case 'BuildingPlaced':    return this.tone(330, 0.08);
            case 'EngineIdle':        return this.noise(0.5, 0.05);
            default:                  return this.tone(440, 0.05);
        }
    }
}

/**
 * WebAudio-backed audio manager for the browser build.
 * Mirrors the C# IAudioManager interface.
 *
 * Usage:
 *   const audio = new WebAudioManager();
 *   audio.playSound('UIClick');
 *   audio.setMasterVolume(0.8);
 */
class WebAudioManager {
    constructor() {
        /** @type {AudioContext|null} */
        this._ctx = null;
        /** @type {GainNode|null} */
        this._masterGain = null;
        /** @type {GainNode|null} */
        this._sfxGain = null;
        /** @type {GainNode|null} */
        this._musicGain = null;
        /** @type {AudioBufferSourceNode|null} */
        this._musicSource = null;
        /** @type {Map<AudioEventType, AudioBuffer>} */
        this._buffers = new Map();
        /** @type {WebAudioPlaceholderGenerator|null} */
        this._gen = null;

        this.settings = {
            masterVolume: 1.0,
            sfxVolume:    1.0,
            musicVolume:  0.7,
        };
    }

    /** Lazily initialise AudioContext on first user interaction (browser policy). */
    _ensureContext() {
        if (this._ctx) return;
        this._ctx = new (window.AudioContext || window.webkitAudioContext)();
        this._masterGain = this._ctx.createGain();
        this._sfxGain    = this._ctx.createGain();
        this._musicGain  = this._ctx.createGain();
        this._sfxGain.connect(this._masterGain);
        this._musicGain.connect(this._masterGain);
        this._masterGain.connect(this._ctx.destination);
        this._applyGains();
        this._gen = new WebAudioPlaceholderGenerator(this._ctx);
    }

    _applyGains() {
        if (!this._masterGain) return;
        this._masterGain.gain.value = this.settings.masterVolume;
        this._sfxGain.gain.value    = this.settings.sfxVolume;
        this._musicGain.gain.value  = this.settings.musicVolume;
    }

    /**
     * Play a one-shot sound effect.
     * @param {AudioEventType} eventType
     * @param {{x:number,y:number,z:number}} [position] world position (for panning)
     */
    playSound(eventType, position = { x: 0, y: 0, z: 0 }) {
        this._ensureContext();
        if (!this._buffers.has(eventType))
            this._buffers.set(eventType, this._gen.getPlaceholder(eventType));
        const src = this._ctx.createBufferSource();
        src.buffer = this._buffers.get(eventType);
        src.connect(this._sfxGain);
        src.start();
    }

    /**
     * Start background music.
     * @param {string} trackId  (placeholder: ignored, plays a tone)
     * @param {boolean} [loop]
     * @param {number} [crossfadeSeconds]
     */
    playMusic(trackId, loop = true, crossfadeSeconds = 1.0) {
        this._ensureContext();
        this.stopMusic(crossfadeSeconds);
        const buf = this._gen.tone(110, 4.0, 0.5, 0.5);
        this._musicSource = this._ctx.createBufferSource();
        this._musicSource.buffer = buf;
        this._musicSource.loop   = loop;
        this._musicSource.connect(this._musicGain);
        this._musicSource.start();
    }

    /**
     * Stop background music.
     * @param {number} [fadeOutSeconds]
     */
    stopMusic(fadeOutSeconds = 1.0) {
        if (!this._musicSource) return;
        const gain = this._musicGain.gain;
        gain.setValueAtTime(gain.value, this._ctx.currentTime);
        gain.linearRampToValueAtTime(0, this._ctx.currentTime + fadeOutSeconds);
        const src = this._musicSource;
        setTimeout(() => { try { src.stop(); } catch(_) {} }, fadeOutSeconds * 1000 + 50);
        this._musicSource = null;
    }

    /** @param {number} volume 0–1 */
    setMasterVolume(volume) {
        this.settings.masterVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** @param {number} volume 0–1 */
    setSfxVolume(volume) {
        this.settings.sfxVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** @param {number} volume 0–1 */
    setMusicVolume(volume) {
        this.settings.musicVolume = Math.max(0, Math.min(1, volume));
        this._applyGains();
    }

    /** Resume suspended context (call after user gesture). */
    resume() {
        if (this._ctx && this._ctx.state === 'suspended')
            this._ctx.resume();
    }
}

// Expose globally for the browser build
if (typeof window !== 'undefined') {
    window.WebAudioManager = WebAudioManager;
}
