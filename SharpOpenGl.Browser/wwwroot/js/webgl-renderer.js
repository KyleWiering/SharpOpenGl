// WebGL2 backend matching desktop GameShaders (projection/view/model/overrideColor).
window.sharpGl = (() => {
    let gl = null;
    let program = null;
    let locProjection = null;
    let locView = null;
    let locModel = null;
    let locColor = null;
    let meshes = {};
    let nextMeshId = 1;

    const vertSrc = `#version 300 es
precision mediump float;
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
precision mediump float;
in vec3 fragColor;
out vec4 outputColor;
void main() { outputColor = vec4(fragColor, 1.0); }`;

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

    function drawMesh(meshId, vertexCount, model, color, primitiveType) {
        if (!gl || meshId <= 0 || vertexCount <= 0) return;
        const mesh = meshes[meshId];
        if (!mesh) return;

        gl.uniformMatrix4fv(locModel, false, model);
        gl.uniform4fv(locColor, color);

        gl.bindVertexArray(mesh.vao);
        const mode = primitiveType === 1 ? gl.LINES
            : primitiveType === 0 ? gl.POINTS
            : gl.TRIANGLES;
        gl.drawArrays(mode, 0, vertexCount);
        gl.bindVertexArray(null);
    }

    return { init, resize, clear, uploadMesh, beginFrame, drawMesh };
})();