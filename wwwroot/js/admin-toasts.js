(() => {
    const ensureContainer = () => {
        let container = document.querySelector('[data-toast-container]');
        if (container) return container;

        container = document.createElement('div');
        container.className = 'toast-container';
        container.setAttribute('data-toast-container', '');
        container.setAttribute('aria-live', 'polite');
        container.setAttribute('aria-atomic', 'false');
        document.body.appendChild(container);
        return container;
    };

    const shownMessages = new Set();

    const showToast = (message, kind = 'info', options = {}) => {
        const text = String(message ?? '').trim();
        if (!text || shownMessages.has(`${kind}:${text}`)) return;
        shownMessages.add(`${kind}:${text}`);

        const container = ensureContainer();
        const toast = document.createElement('div');
        toast.className = `app-toast ${kind}`;
        toast.setAttribute('role', kind === 'error' ? 'alert' : 'status');

        const icon = document.createElement('span');
        icon.className = 'app-toast-icon';
        icon.textContent = kind === 'success' ? '✓' : kind === 'error' ? '!' : kind === 'warning' ? '!' : 'i';

        const body = document.createElement('div');
        body.className = 'app-toast-body';
        const title = document.createElement('strong');
        title.textContent = kind === 'success' ? 'انجام شد' : kind === 'error' ? 'خطا' : kind === 'warning' ? 'توجه' : 'اطلاع';
        const content = document.createElement('span');
        content.textContent = text;
        body.append(title, content);

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'app-toast-close';
        close.setAttribute('aria-label', 'بستن پیام');
        close.textContent = '×';

        const progress = document.createElement('span');
        progress.className = 'app-toast-progress';

        toast.append(icon, body, close, progress);
        container.appendChild(toast);

        const duration = Number(options.duration ?? (kind === 'error' ? 8500 : 5500));
        progress.style.setProperty('--toast-duration', `${duration}ms`);

        const remove = () => {
            if (toast.classList.contains('leaving')) return;
            toast.classList.add('leaving');
            window.setTimeout(() => toast.remove(), 220);
        };

        close.addEventListener('click', remove);
        if (!options.persistent) window.setTimeout(remove, duration);
    };

    window.NavidProToast = showToast;

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-toast-kind][data-toast-message]').forEach(source => {
            showToast(source.getAttribute('data-toast-message'), source.getAttribute('data-toast-kind') || 'info');
        });

        document.querySelectorAll('.validation-summary').forEach(summary => {
            const messages = [...summary.querySelectorAll('li')]
                .map(item => item.textContent?.trim())
                .filter(Boolean);

            messages.forEach(message => showToast(message, 'error'));
            if (messages.length) summary.classList.add('toast-validation-source');
        });

        document.querySelectorAll('.field-validation-error').forEach(field => {
            const message = field.textContent?.trim();
            if (message) showToast(message, 'error');
        });

        const lockoutSource = document.querySelector('[data-lockout-end]');
        if (lockoutSource) {
            const end = new Date(lockoutSource.getAttribute('data-lockout-end'));
            if (!Number.isNaN(end.getTime())) {
                const remainingMs = Math.max(0, end.getTime() - Date.now());
                const remainingMinutes = Math.max(1, Math.ceil(remainingMs / 60000));
                const timeText = new Intl.DateTimeFormat('fa-IR', { hour: '2-digit', minute: '2-digit' }).format(end);
                const minuteText = remainingMinutes.toLocaleString('fa-IR');
                showToast(
                    `حساب شما موقتاً قفل شده است. تا حدود ساعت ${timeText}، یعنی تقریباً ${minuteText} دقیقه دیگر صبر کنید. پس از پایان زمان قفل، حساب خودکار باز می‌شود. در صورت نیاز می‌توانید از مدیر سایت بخواهید زودتر حساب را باز کند.`,
                    'warning',
                    { duration: 12000 }
                );
            }
        }
    });
})();
