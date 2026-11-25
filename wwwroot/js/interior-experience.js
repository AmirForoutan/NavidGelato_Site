(() => {
    const page = document.querySelector('.public-motion-page');
    if (!page) return;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    document.body.classList.add('interior-motion-ready');

    const revealTargets = [...page.querySelectorAll('[data-motion-reveal],[data-motion-card]')];
    if (reduceMotion || !('IntersectionObserver' in window)) {
        revealTargets.forEach(el => el.classList.add('motion-visible'));
    } else {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('motion-visible');
                observer.unobserve(entry.target);
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -7% 0px' });
        revealTargets.forEach(el => observer.observe(el));
    }

    const menuHeroProduct = page.querySelector('.menu-hero-product');
    const branchVisual = page.querySelector('.branches-hero-visual');
    const contactSignal = page.querySelector('.contact-signal');
    const menuSections = [...page.querySelectorAll('.menu-section[data-menu-section]')];
    let ticking = false;

    const clamp = (value, min = 0, max = 1) => Math.min(max, Math.max(min, value));
    const update = () => {
        ticking = false;
        if (reduceMotion) return;
        const y = window.scrollY;
        if (menuHeroProduct) {
            const t = clamp(y / 650);
            menuHeroProduct.style.setProperty('--menu-product-shift', `${(t * 48).toFixed(1)}px`);
            menuHeroProduct.style.setProperty('--menu-product-rotate', `${(-3 + t * 8).toFixed(2)}deg`);
        }
        if (branchVisual) branchVisual.style.setProperty('--branches-visual-shift', `${(clamp(y / 600) * 38).toFixed(1)}px`);
        if (contactSignal) contactSignal.style.setProperty('--contact-signal-shift', `${(clamp(y / 620) * 42).toFixed(1)}px`);

        menuSections.forEach(section => {
            const rect = section.getBoundingClientRect();
            const center = window.innerHeight * .55;
            const distance = Math.abs(rect.top + Math.min(rect.height, window.innerHeight) * .36 - center);
            const active = distance < window.innerHeight * .5;
            section.classList.toggle('motion-section-active', active);
            const x = clamp((window.innerHeight - rect.top) / (window.innerHeight + Math.max(rect.height, 1))) * 42;
            section.style.setProperty('--section-word-x', `${x.toFixed(1)}px`);
        });
    };
    const requestUpdate = () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(update);
    };
    addEventListener('scroll', requestUpdate, { passive: true });
    addEventListener('resize', requestUpdate, { passive: true });
    requestUpdate();

    if (!reduceMotion && matchMedia('(hover:hover) and (pointer:fine)').matches) {
        page.querySelectorAll('.menu-card[data-motion-card]').forEach(card => {
            card.addEventListener('pointermove', event => {
                const r = card.getBoundingClientRect();
                const nx = (event.clientX - r.left) / r.width - .5;
                const ny = (event.clientY - r.top) / r.height - .5;
                card.style.setProperty('--tilt-y', `${(nx * 5).toFixed(2)}deg`);
                card.style.setProperty('--tilt-x', `${(-ny * 4).toFixed(2)}deg`);
                card.style.setProperty('--card-glow-x', `${((nx + .5) * 100).toFixed(1)}%`);
                card.style.setProperty('--card-glow-y', `${((ny + .5) * 100).toFixed(1)}%`);
            });
            card.addEventListener('pointerleave', () => {
                card.style.setProperty('--tilt-x', '0deg');
                card.style.setProperty('--tilt-y', '0deg');
                card.style.setProperty('--card-glow-x', '50%');
                card.style.setProperty('--card-glow-y', '40%');
            });
        });
    }

    page.querySelectorAll('.nearest-branch-panel,.contact-form-card').forEach(panel => {
        if (reduceMotion) return;
        panel.addEventListener('pointermove', event => {
            const r = panel.getBoundingClientRect();
            panel.style.setProperty('--pointer-x', `${event.clientX - r.left}px`);
            panel.style.setProperty('--pointer-y', `${event.clientY - r.top}px`);
        });
        panel.addEventListener('pointerleave', () => {
            panel.style.setProperty('--pointer-x', '50%');
            panel.style.setProperty('--pointer-y', '30%');
        });
    });

    const categoryLinks = [...page.querySelectorAll('[data-category-link]')];
    categoryLinks.forEach(link => link.addEventListener('click', () => {
        categoryLinks.forEach(x => x.classList.remove('motion-clicked'));
        link.classList.add('motion-clicked');
        setTimeout(() => link.classList.remove('motion-clicked'), 380);
    }));
})();
