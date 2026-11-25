(() => {
    const shown = new Set();

    const ensureContainer = () => {
        let container = document.querySelector('[data-public-toast-container]');
        if (container) return container;
        container = document.createElement('div');
        container.className = 'public-toast-container';
        container.setAttribute('data-public-toast-container', '');
        container.setAttribute('aria-live', 'polite');
        document.body.appendChild(container);
        return container;
    };

    const show = (message, kind = 'info') => {
        const text = String(message ?? '').trim();
        const key = `${kind}:${text}`;
        if (!text || shown.has(key)) return;
        shown.add(key);

        const toast = document.createElement('div');
        toast.className = `public-toast ${kind}`;
        toast.setAttribute('role', kind === 'error' ? 'alert' : 'status');

        const icon = document.createElement('span');
        icon.className = 'public-toast-icon';
        icon.textContent = kind === 'success' ? '✓' : kind === 'error' ? '!' : 'i';

        const body = document.createElement('div');
        body.className = 'public-toast-body';
        const title = document.createElement('strong');
        title.textContent = kind === 'success' ? 'ارسال شد' : kind === 'error' ? 'ارسال نشد' : 'اطلاع';
        const content = document.createElement('span');
        content.textContent = text;
        body.append(title, content);

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'public-toast-close';
        close.setAttribute('aria-label', 'بستن پیام');
        close.textContent = '×';

        const progress = document.createElement('span');
        progress.className = 'public-toast-progress';
        const duration = kind === 'error' ? 9000 : 6000;
        progress.style.setProperty('--public-toast-duration', `${duration}ms`);

        toast.append(icon, body, close, progress);
        ensureContainer().appendChild(toast);

        const remove = () => {
            if (toast.classList.contains('leaving')) return;
            toast.classList.add('leaving');
            window.setTimeout(() => toast.remove(), 220);
        };

        close.addEventListener('click', remove);
        window.setTimeout(remove, duration);
    };

    window.NavidProToast = show;

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-public-toast-kind][data-public-toast-message]').forEach(source => {
            show(source.getAttribute('data-public-toast-message'), source.getAttribute('data-public-toast-kind') || 'info');
        });

        document.querySelectorAll('.validation-summary').forEach(summary => {
            const messages = [...summary.querySelectorAll('li')].map(x => x.textContent?.trim()).filter(Boolean);
            messages.forEach(message => show(message, 'error'));
            if (messages.length) summary.classList.add('toast-validation-source');
        });

        document.querySelectorAll('.field-validation-error').forEach(field => {
            const message = field.textContent?.trim();
            if (message) show(message, 'error');
            field.classList.add('toast-validation-source');
        });
    });
})();
