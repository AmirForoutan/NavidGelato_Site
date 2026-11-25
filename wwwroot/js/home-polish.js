(() => {
    const hero = document.querySelector('.home-hero');
    if (!hero) return;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    document.body.classList.add('home-polish-ready');

    const ticker = document.querySelector('.ticker');
    if (ticker && !document.querySelector('.home-franchise-shortcut')) {
        const franchise = document.createElement('section');
        franchise.className = 'home-franchise-shortcut';
        franchise.setAttribute('aria-label', 'درخواست فرانچایز نوید ژلاتو');
        franchise.innerHTML = `
            <div class="container home-franchise-shell">
                <div class="home-franchise-copy">
                    <span class="home-franchise-kicker">BUSINESS / FRANCHISE</span>
                    <h2>می‌خوای نوید ژلاتو<br><strong>به شهر تو هم برسه؟</strong></h2>
                    <p>اگر برای راه‌اندازی شعبه نوید ژلاتو در شهر یا منطقه خودت جدی هستی، شرایط همکاری، تفاوت فرانچایز و نمایندگی و مسیر ثبت درخواست رو یک‌جا ببین.</p>
                    <a class="home-franchise-link" href="/Franchise">درخواست فرانچایز <span>←</span></a>
                </div>
                <div class="home-franchise-visual" aria-hidden="true">
                    <div class="home-franchise-orbit orbit-one"></div>
                    <div class="home-franchise-orbit orbit-two"></div>
                    <div class="home-franchise-mark"><b>NP</b><small>FRANCHISE</small></div>
                    <span class="home-franchise-city city-one">YOUR CITY</span>
                    <span class="home-franchise-city city-two">NEXT?</span>
                </div>
            </div>
        `;
        ticker.insertAdjacentElement('afterend', franchise);
    }

    const flare = document.createElement('div');
    flare.className = 'home-scroll-flare';
    flare.setAttribute('aria-hidden', 'true');
    document.body.appendChild(flare);

    const sections = [
        document.querySelector('.home-franchise-shortcut'),
        document.querySelector('.story-scroll-section'),
        document.querySelector('.menu-journey'),
        document.querySelector('.showcase-section'),
        document.querySelector('.brand-numbers'),
        document.querySelector('.branch-preview'),
        document.querySelector('.final-choice'),
        document.querySelector('.cta-section')
    ].filter(Boolean);

    document.querySelectorAll('.mini-branches > div').forEach((item, index) => {
        item.style.setProperty('--mini-delay', `${index * 90}ms`);
    });

    if (!reduceMotion && 'IntersectionObserver' in window) {
        const sectionObserver = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) entry.target.classList.add('home-section-live');
            });
        }, { threshold: .18, rootMargin: '0px 0px -10% 0px' });
        sections.forEach(section => sectionObserver.observe(section));
    } else {
        sections.forEach(section => section.classList.add('home-section-live'));
    }

    const showcase = document.querySelector('.showcase-section');
    const primaryHeroButton = document.querySelector('.home-hero .hero-actions .btn-primary');
    const franchiseShell = document.querySelector('.home-franchise-shell');

    if (!reduceMotion) {
        hero.addEventListener('pointermove', event => {
            if (event.pointerType === 'touch') return;
            const rect = hero.getBoundingClientRect();
            hero.style.setProperty('--hero-glow-x', `${event.clientX - rect.left}px`);
            hero.style.setProperty('--hero-glow-y', `${event.clientY - rect.top}px`);
        });

        showcase?.addEventListener('pointermove', event => {
            const rect = showcase.getBoundingClientRect();
            showcase.style.setProperty('--showcase-x', `${event.clientX - rect.left}px`);
            showcase.style.setProperty('--showcase-y', `${event.clientY - rect.top}px`);
        });

        franchiseShell?.addEventListener('pointermove', event => {
            if (event.pointerType === 'touch') return;
            const rect = franchiseShell.getBoundingClientRect();
            const x = (event.clientX - rect.left) / Math.max(1, rect.width) - .5;
            const y = (event.clientY - rect.top) / Math.max(1, rect.height) - .5;
            franchiseShell.style.setProperty('--franchise-x', `${(x * 16).toFixed(1)}px`);
            franchiseShell.style.setProperty('--franchise-y', `${(y * 10).toFixed(1)}px`);
            franchiseShell.style.setProperty('--franchise-glow-x', `${event.clientX - rect.left}px`);
            franchiseShell.style.setProperty('--franchise-glow-y', `${event.clientY - rect.top}px`);
        });
        franchiseShell?.addEventListener('pointerleave', () => {
            franchiseShell.style.setProperty('--franchise-x', '0px');
            franchiseShell.style.setProperty('--franchise-y', '0px');
        });

        primaryHeroButton?.addEventListener('pointermove', event => {
            if (event.pointerType === 'touch') return;
            const rect = primaryHeroButton.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width - .5;
            const y = (event.clientY - rect.top) / rect.height - .5;
            primaryHeroButton.style.setProperty('--magnetic-x', `${(x * 7).toFixed(1)}px`);
            primaryHeroButton.style.setProperty('--magnetic-y', `${(y * 5).toFixed(1)}px`);
        });
        primaryHeroButton?.addEventListener('pointerleave', () => {
            primaryHeroButton.style.setProperty('--magnetic-x', '0px');
            primaryHeroButton.style.setProperty('--magnetic-y', '0px');
        });
    }

    let ticking = false;
    const updateScroll = () => {
        ticking = false;
        const doc = document.documentElement;
        const max = Math.max(1, doc.scrollHeight - window.innerHeight);
        const progress = Math.min(1, Math.max(0, window.scrollY / max));
        flare.style.setProperty('--flare-opacity', (0.2 + progress * 0.8).toFixed(3));
        flare.style.setProperty('--flare-scale', (0.85 + progress * 0.7).toFixed(3));
        flare.style.setProperty('--flare-line', progress.toFixed(3));
    };
    const requestScrollUpdate = () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(updateScroll);
    };

    window.addEventListener('scroll', requestScrollUpdate, { passive: true });
    window.addEventListener('resize', requestScrollUpdate, { passive: true });
    requestScrollUpdate();
})();
