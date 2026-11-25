(() => {
    const page = document.querySelector('.branches-page');
    if (!page) return;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    const targets = [
        page.querySelector('.branches-page-hero h1'),
        page.querySelector('.nearest-branch-panel h2'),
        page.querySelector('.branch-route-head h2')
    ].filter(Boolean);

    const splitTextNode = (node, indexRef) => {
        const value = node.nodeValue ?? '';
        const parts = value.split(/(\s+)/);
        const fragment = document.createDocumentFragment();

        parts.forEach(part => {
            if (!part) return;
            if (/^\s+$/.test(part)) {
                fragment.appendChild(document.createTextNode(part));
                return;
            }

            const span = document.createElement('span');
            span.className = 'motion-word';
            span.style.setProperty('--motion-word-index', String(indexRef.value++));
            span.textContent = part;
            fragment.appendChild(span);
        });

        node.replaceWith(fragment);
    };

    targets.forEach(target => {
        if (target.dataset.motionWords === '1') return;
        target.dataset.motionWords = '1';
        target.classList.add('branches-motion-text');

        const indexRef = { value: 0 };
        const walker = document.createTreeWalker(target, NodeFilter.SHOW_TEXT);
        const textNodes = [];
        let current;
        while ((current = walker.nextNode())) {
            if ((current.nodeValue ?? '').trim()) textNodes.push(current);
        }
        textNodes.forEach(node => splitTextNode(node, indexRef));
    });

    if (reduceMotion || !('IntersectionObserver' in window)) {
        targets.forEach(target => target.classList.add('is-motion-visible'));
        return;
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add('is-motion-visible');
            observer.unobserve(entry.target);
        });
    }, { threshold: .28, rootMargin: '0px 0px -8% 0px' });

    targets.forEach(target => observer.observe(target));
})();
