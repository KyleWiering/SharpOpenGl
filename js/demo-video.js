(function () {
    const toggle = document.getElementById('demo-toggle');
    const overlay = document.getElementById('demo-overlay');
    const close = document.getElementById('demo-close');
    const video = document.getElementById('gameplay-demo');

    if (!toggle || !overlay || !close || !video) return;

    toggle.addEventListener('click', () => {
        overlay.classList.add('open');
        video.play().catch(() => { /* autoplay may be blocked until user gesture */ });
    });

    close.addEventListener('click', () => {
        overlay.classList.remove('open');
        video.pause();
    });

    overlay.addEventListener('click', (event) => {
        if (event.target === overlay) {
            overlay.classList.remove('open');
            video.pause();
        }
    });
})();