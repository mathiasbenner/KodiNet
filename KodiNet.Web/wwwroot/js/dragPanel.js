function dragStart(e, panel) {
    startY = e.touches[0].clientY;
    startH = panel.getBoundingClientRect().height;
    startTime = Date.now();
    dragging = true;
    panel.style.transition = 'none';
}

function dragMove(e, panel) {
    if (!dragging) return;
    const dy = e.touches[0].clientY - startY;
    const newH = Math.max(0, startH - dy);
    panel.style.height = newH + 'px';
}

function dragEnd(e, panel, dotnet) {
    if (!dragging) return;
    dragging = false;
    panel.style.transition = '';

    const dy = e.changedTouches[0].clientY - startY;
    const elapsed = Date.now() - startTime;
    const velocity = dy / Math.max(elapsed, 1);

    if (dy > startH * 0.50 || velocity > 1)
    {
        panel.style.height = 0;
        dotnet.invokeMethodAsync('CloseFromDrag');
    }
    panel.style.height = '';
}

window.dragPanel = {
    init(handle, panel, dotnet) {
        if (!handle || !panel) return;

        let startY = 0, startH = 0, startTime = 0, dragging = false;

        handle.addEventListener('touchstart', (e) => dragStart(e, panel), {passive: true});

        handle.addEventListener('touchmove', (e) => dragMove(e, panel), {passive: true});

        handle.addEventListener('touchend', (e) => dragEnd(e, panel, dotnet), {passive: true});
    }
};