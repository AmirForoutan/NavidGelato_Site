(() => {
    const page = document.querySelector('[data-franchise-page]');
    if (!page) return;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    document.body.classList.add('franchise-motion-ready');

    const revealItems = [...page.querySelectorAll('[data-fr-reveal],[data-fr-card]')];
    revealItems.forEach((item, index) => item.style.setProperty('--fr-delay', `${Math.min((index % 5) * 70, 280)}ms`));

    if (!reduceMotion && 'IntersectionObserver' in window) {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('fr-visible');
                observer.unobserve(entry.target);
            });
        }, { threshold: .14, rootMargin: '0px 0px -8% 0px' });
        revealItems.forEach(item => observer.observe(item));
    } else {
        revealItems.forEach(item => item.classList.add('fr-visible'));
    }

    const spotlight = page.querySelector('[data-fr-spotlight]');
    if (spotlight && !reduceMotion) {
        spotlight.addEventListener('pointermove', event => {
            if (event.pointerType === 'touch') return;
            const rect = spotlight.getBoundingClientRect();
            spotlight.style.setProperty('--spot-x', `${event.clientX - rect.left}px`);
            spotlight.style.setProperty('--spot-y', `${event.clientY - rect.top}px`);
        });
        spotlight.addEventListener('pointerleave', () => {
            spotlight.style.setProperty('--spot-x', '50%');
            spotlight.style.setProperty('--spot-y', '20%');
        });
    }

    const hero = page.querySelector('.franchise-hero');
    const visual = page.querySelector('.franchise-hero-visual');
    if (hero && visual && !reduceMotion) {
        hero.addEventListener('pointermove', event => {
            if (event.pointerType === 'touch') return;
            const rect = hero.getBoundingClientRect();
            const x = (event.clientX - rect.left) / Math.max(rect.width, 1) - .5;
            const y = (event.clientY - rect.top) / Math.max(rect.height, 1) - .5;
            visual.style.transform = `perspective(1100px) rotateX(${(-y * 4).toFixed(2)}deg) rotateY(${(x * 5).toFixed(2)}deg) translate3d(${(x * 10).toFixed(1)}px,${(y * 8).toFixed(1)}px,0)`;
        });
        hero.addEventListener('pointerleave', () => { visual.style.transform = ''; });
    }
})();
