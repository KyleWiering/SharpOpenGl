window.sharpLoop = (() => {
    let handle = 0;
    let ref = null;
    let last = 0;

    function start(dotNetRef) {
        stop();
        ref = dotNetRef;
        last = performance.now();
        const tick = (ts) => {
            const dt = Math.min(0.05, (ts - last) / 1000);
            last = ts;
            if (ref) ref.invokeMethodAsync('OnFrame', dt);
            handle = requestAnimationFrame(tick);
        };
        handle = requestAnimationFrame(tick);
    }

    function stop() {
        if (handle) cancelAnimationFrame(handle);
        handle = 0;
        ref = null;
    }

    return { start, stop };
})();