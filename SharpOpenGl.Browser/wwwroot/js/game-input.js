window.sharpGameInput = {
    attach(dotNetRef) {
        window.__sharpGameRef = dotNetRef;
        window.addEventListener('keydown', (e) => {
            if (window.__sharpGameRef)
                window.__sharpGameRef.invokeMethodAsync('OnKey', e.key);
        });
        window.addEventListener('resize', () => {
            // Viewport resize handled on next frame via canvas client size if needed.
        });
    }
};