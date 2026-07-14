window.sharpGameInput = {
    attach(dotNetRef) {
        window.__sharpGameRef = dotNetRef;
        const canvas = document.getElementById('ui-canvas');

        window.addEventListener('keydown', (e) => {
            if (window.__sharpGameRef)
                window.__sharpGameRef.invokeMethodAsync('OnKey', e.key);
        });

        window.addEventListener('resize', () => {
            if (!window.__sharpGameRef) return;
            const w = Math.max(1, window.innerWidth);
            const h = Math.max(1, window.innerHeight);
            window.__sharpGameRef.invokeMethodAsync('OnResize', w, h);
        });

        if (!canvas) return;

        const active = new Map();

        function rectOffset(clientX, clientY) {
            const r = canvas.getBoundingClientRect();
            return { x: clientX - r.left, y: clientY - r.top };
        }

        function emitTouches() {
            if (!window.__sharpGameRef) return;
            const payload = [];
            active.forEach((t) => {
                payload.push({
                    id: t.id,
                    x: t.x,
                    y: t.y,
                    isActive: t.isActive
                });
            });
            window.__sharpGameRef.invokeMethodAsync('OnTouchPoints', JSON.stringify(payload));
        }

        canvas.addEventListener('touchstart', (e) => {
            for (const touch of e.changedTouches) {
                const p = rectOffset(touch.clientX, touch.clientY);
                active.set(touch.identifier, {
                    id: touch.identifier,
                    x: p.x,
                    y: p.y,
                    isActive: true
                });
            }
            emitTouches();
        }, { passive: false });

        canvas.addEventListener('touchmove', (e) => {
            for (const touch of e.changedTouches) {
                const entry = active.get(touch.identifier);
                if (!entry) continue;
                const p = rectOffset(touch.clientX, touch.clientY);
                entry.x = p.x;
                entry.y = p.y;
            }
            emitTouches();
        }, { passive: false });

        canvas.addEventListener('touchend', (e) => {
            for (const touch of e.changedTouches) {
                const entry = active.get(touch.identifier);
                if (entry) {
                    const p = rectOffset(touch.clientX, touch.clientY);
                    entry.x = p.x;
                    entry.y = p.y;
                    entry.isActive = false;
                }
            }
            emitTouches();
            for (const touch of e.changedTouches)
                active.delete(touch.identifier);
        }, { passive: false });

        canvas.addEventListener('touchcancel', (e) => {
            for (const touch of e.changedTouches)
                active.delete(touch.identifier);
            emitTouches();
        }, { passive: false });
    }
};