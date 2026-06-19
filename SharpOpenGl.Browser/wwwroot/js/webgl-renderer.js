window.sharpGl = (() => {
    let gl = null;
    let program = null;
    let width = 0;
    let height = 0;

    const vertSrc = `#version 300 es
precision mediump float;
layout(location=0) in vec3 aPos;
layout(location=1) in vec4 aColor;
uniform mat4 uMvp;
out vec4 vColor;
void main() {
    vColor = aColor;
    gl_Position = uMvp * vec4(aPos, 1.0);
    gl_PointSize = 6.0;
}`;

    const fragSrc = `#version 300 es
precision mediump float;
in vec4 vColor;
out vec4 outColor;
void main() { outColor = vColor; }`;

    function compile(type, src) {
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS))
            throw new Error(gl.getShaderInfoLog(s));
        return s;
    }

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;
        gl = canvas.getContext('webgl2', { alpha: false, antialias: true });
        if (!gl) return false;

        const vs = compile(gl.VERTEX_SHADER, vertSrc);
        const fs = compile(gl.FRAGMENT_SHADER, fragSrc);
        program = gl.createProgram();
        gl.attachShader(program, vs);
        gl.attachShader(program, fs);
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS))
            throw new Error(gl.getProgramInfoLog(program));

        gl.enable(gl.DEPTH_TEST);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        resize(canvas.clientWidth, canvas.clientHeight);
        return true;
    }

    function resize(w, h) {
        width = Math.max(1, w | 0);
        height = Math.max(1, h | 0);
        const canvas = gl?.canvas;
        if (canvas) {
            canvas.width = width;
            canvas.height = height;
            gl.viewport(0, 0, width, height);
        }
    }

    function clear(r, g, b) {
        if (!gl) return;
        gl.clearColor(r, g, b, 1);
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    }

    function drawBatch(mvp, interleaved, primitive) {
        if (!gl || !interleaved || interleaved.length === 0) return;

        const buf = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, buf);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(interleaved), gl.DYNAMIC_DRAW);

        const stride = 7 * 4;
        gl.useProgram(program);
        gl.uniformMatrix4fv(gl.getUniformLocation(program, 'uMvp'), false, mvp);

        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, stride, 0);
        gl.enableVertexAttribArray(1);
        gl.vertexAttribPointer(1, 4, gl.FLOAT, false, stride, 12);

        const count = interleaved.length / 7;
        const mode = primitive === 1 ? gl.LINES : primitive === 2 ? gl.POINTS : gl.TRIANGLES;
        gl.drawArrays(mode, 0, count);

        gl.deleteBuffer(buf);
    }

    return { init, resize, clear, drawBatch };
})();