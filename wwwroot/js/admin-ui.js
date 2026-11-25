(() => {
    const body = document.body;
    const sidebar = document.querySelector('[data-admin-sidebar]');
    const toggle = document.querySelector('[data-admin-sidebar-toggle]');
    const closeButton = document.querySelector('[data-admin-sidebar-close]');
    const backdrop = document.querySelector('[data-admin-sidebar-backdrop]');

    const closeSidebar = () => {
        body.classList.remove('admin-nav-open');
        toggle?.setAttribute('aria-expanded', 'false');
    };

    const openSidebar = () => {
        body.classList.add('admin-nav-open');
        toggle?.setAttribute('aria-expanded', 'true');
    };

    toggle?.addEventListener('click', () => body.classList.contains('admin-nav-open') ? closeSidebar() : openSidebar());
    closeButton?.addEventListener('click', closeSidebar);
    backdrop?.addEventListener('click', closeSidebar);
    sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', closeSidebar));

    const editor = document.querySelector('[data-admin-editor-modal].is-open');
    let editorBackdrop = null;
    if (editor) {
        editorBackdrop = document.createElement('div');
        editorBackdrop.className = 'admin-editor-backdrop is-open';
        document.body.appendChild(editorBackdrop);
        body.classList.add('admin-editor-open');
        editorBackdrop.addEventListener('click', () => {
            const url = editor.getAttribute('data-admin-editor-close-url');
            if (url) window.location.assign(url);
        });
    }

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        if (body.classList.contains('admin-nav-open')) {
            closeSidebar();
            return;
        }
        if (editor) {
            const url = editor.getAttribute('data-admin-editor-close-url');
            if (url) window.location.assign(url);
        }
    });
})();
