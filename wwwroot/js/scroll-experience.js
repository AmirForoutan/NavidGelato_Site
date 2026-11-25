(() => {
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    document.body.classList.add('scroll-motion-ready');

    const progressHost = document.createElement('div');
    progressHost.className = 'site-scroll-progress';
    progressHost.setAttribute('aria-hidden', 'true');
    document.body.appendChild(progressHost);

    const clamp = (value, min = 0, max = 1) => Math.min(max, Math.max(min, value));
    const lerp = (a, b, t) => a + (b - a) * t;
    const viewportProgress = (element, start = 1, end = 0) => {
        const rect = element.getBoundingClientRect();
        const vh = window.innerHeight || 1;
        const from = vh * start;
        const to = vh * end - rect.height;
        const denominator = from - to || 1;
        return clamp((from - rect.top) / denominator);
    };

    const revealObserver = !reduceMotion && 'IntersectionObserver' in window
        ? new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) entry.target.classList.add('scroll-in-view');
            });
        }, { threshold: 0.18, rootMargin: '0px 0px -8% 0px' })
        : null;

    document.querySelectorAll('.story-step,[data-scroll-card]').forEach(el => {
        if (reduceMotion || !revealObserver) el.classList.add('scroll-in-view');
        else revealObserver.observe(el);
    });

    const homeHero = document.querySelector('.home-hero');
    const identityScene = document.querySelector('[data-identity-scene]');
    const branchImage = document.querySelector('.branch-photo img[data-scroll-layer]');
    const pageHeroes = [...document.querySelectorAll('.page-hero')];
    const spotlight = document.querySelector('[data-spotlight]');

    let ticking = false;
    const update = () => {
        ticking = false;
        const doc = document.documentElement;
        const scrollable = Math.max(1, doc.scrollHeight - window.innerHeight);
        const pageProgress = clamp(window.scrollY / scrollable);
        doc.style.setProperty('--page-scroll-progress', pageProgress.toFixed(4));

        if (reduceMotion) return;

        if (homeHero) {
            const rect = homeHero.getBoundingClientRect();
            const heroHeight = Math.max(1, rect.height);
            const t = clamp(-rect.top / heroHeight, 0, 1);
            homeHero.style.setProperty('--hero-image-y', `${lerp(0, 72, t).toFixed(2)}px`);
            homeHero.style.setProperty('--hero-image-scale', lerp(1, 1.08, t).toFixed(4));
            homeHero.style.setProperty('--hero-copy-opacity', lerp(1, .25, clamp(t * 1.25)).toFixed(3));
            homeHero.style.setProperty('--hero-card-top-y', `${lerp(0, -48, t).toFixed(2)}px`);
            homeHero.style.setProperty('--hero-card-bottom-y', `${lerp(0, 56, t).toFixed(2)}px`);
        }

        pageHeroes.forEach(hero => {
            const rect = hero.getBoundingClientRect();
            const t = clamp(-rect.top / Math.max(rect.height, 1), 0, 1);
            hero.style.setProperty('--page-hero-y', `${lerp(0, 42, t).toFixed(2)}px`);
            hero.style.setProperty('--page-hero-opacity', lerp(1, .42, t).toFixed(3));
        });

        if (identityScene) {
            const rect = identityScene.getBoundingClientRect();
            const total = Math.max(1, rect.height - window.innerHeight);
            const t = clamp(-rect.top / total, 0, 1);
            const first = clamp(t / .46);
            const second = clamp((t - .28) / .46);
            identityScene.style.setProperty('--scene-one-opacity', (1 - clamp((t - .22) / .3)).toFixed(3));
            identityScene.style.setProperty('--scene-two-opacity', clamp((t - .18) / .34).toFixed(3));
            identityScene.style.setProperty('--scene-brand-opacity', clamp((t - .55) / .28).toFixed(3));
            identityScene.style.setProperty('--scene-one-x', `${lerp(0, -52, first).toFixed(2)}px`);
            identityScene.style.setProperty('--scene-two-x', `${lerp(42, 0, second).toFixed(2)}px`);
            identityScene.style.setProperty('--scene-product-x', `${lerp(0, 5.5, t).toFixed(3)}vw`);
            identityScene.style.setProperty('--scene-product-y', `${lerp(0, -4, t).toFixed(3)}vh`);
            identityScene.style.setProperty('--scene-product-rotate', `${lerp(-4, 13, t).toFixed(2)}deg`);
            identityScene.style.setProperty('--scene-product-scale', lerp(.92, 1.08, t).toFixed(4));
            identityScene.style.setProperty('--scene-index-width', `${(t * 100).toFixed(1)}%`);
        }

        if (branchImage) {
            const section = branchImage.closest('.branch-preview');
            if (section) {
                const t = viewportProgress(section, 1.15, -.1);
                branchImage.style.setProperty('--layer-shift', `${lerp(-28, 34, t).toFixed(2)}px`);
            }
        }
    };

    const requestUpdate = () => {
        if (ticking) return;
        ticking = true;
        window.requestAnimationFrame(update);
    };

    window.addEventListener('scroll', requestUpdate, { passive: true });
    window.addEventListener('resize', requestUpdate, { passive: true });
    requestUpdate();

    if (spotlight && !reduceMotion) {
        spotlight.addEventListener('pointermove', event => {
            const rect = spotlight.getBoundingClientRect();
            spotlight.style.setProperty('--spotlight-x', `${event.clientX - rect.left}px`);
            spotlight.style.setProperty('--spotlight-y', `${event.clientY - rect.top}px`);
        });
        spotlight.addEventListener('pointerleave', () => {
            spotlight.style.setProperty('--spotlight-x', '50%');
            spotlight.style.setProperty('--spotlight-y', '50%');
        });
    }
})();
