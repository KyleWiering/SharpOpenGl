window.sharpUi = (() => {
    let ctx = null;
    let width = 0;
    let height = 0;

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return false;
        ctx = canvas.getContext('2d');
        resize(canvas.clientWidth, canvas.clientHeight);
        return true;
    }

    function resize(w, h) {
        width = Math.max(1, w | 0);
        height = Math.max(1, h | 0);
        const canvas = ctx?.canvas;
        if (canvas) {
            canvas.width = width;
            canvas.height = height;
        }
    }

    function clear() {
        if (!ctx) return;
        ctx.clearRect(0, 0, width, height);
    }

    function fillRect(x, y, w, h, r, g, b, a) {
        if (!ctx) return;
        ctx.fillStyle = `rgba(${Math.round(r * 255)},${Math.round(g * 255)},${Math.round(b * 255)},${a})`;
        ctx.fillRect(x, y, w, h);
    }

    function strokeRect(x, y, w, h, r, g, b, a) {
        if (!ctx) return;
        ctx.strokeStyle = `rgba(${Math.round(r * 255)},${Math.round(g * 255)},${Math.round(b * 255)},${a})`;
        ctx.lineWidth = 1;
        ctx.strokeRect(x + 0.5, y + 0.5, w - 1, h - 1);
    }

    function drawText(text, x, y, fontSize, r, g, b, a) {
        if (!ctx || !text) return;
        ctx.fillStyle = `rgba(${Math.round(r * 255)},${Math.round(g * 255)},${Math.round(b * 255)},${a})`;
        ctx.font = `${fontSize}px monospace`;
        ctx.textBaseline = 'top';
        ctx.fillText(text, x, y);
    }

    return { init, resize, clear, fillRect, strokeRect, drawText };
})();