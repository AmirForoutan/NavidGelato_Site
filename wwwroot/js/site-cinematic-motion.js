(() => {
    const main = document.querySelector('main');
    if (!main) return;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    document.body.classList.add('site-cinematic-ready');

    const processedTitles = new WeakSet();
    const processedMedia = new WeakSet();
    const processedSections = new WeakSet();

    const titleObserver = !reduceMotion && 'IntersectionObserver' in window
        ? new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('site-motion-visible');
                titleObserver.unobserve(entry.target);
            });
        }, { threshold: .32, rootMargin: '0px 0px -8% 0px' })
        : null;

    const mediaObserver = !reduceMotion && 'IntersectionObserver' in window
        ? new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('site-motion-media-ready');
                mediaObserver.unobserve(entry.target);
            });
        }, { threshold: .2, rootMargin: '0px 0px -10% 0px' })
        : null;

    const sectionObserver = !reduceMotion && 'IntersectionObserver' in window
        ? new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('site-motion-section-visible');
                sectionObserver.unobserve(entry.target);
            });
        }, { threshold: .12, rootMargin: '0px 0px -8% 0px' })
        : null;

    const prepareTitle = title => {
        if (processedTitles.has(title)) return;
        if (title.closest('.home-cinematic-intro,form,details,.opening-modal,.identity-scroll-scene')) return;
        if (title.matches('.branches-page-hero h1,.nearest-branch-panel h2,.branch-route-head h2')) return;
        if (title.matches('.home-hero h1')) return;

        processedTitles.add(title);
        title.classList.add('site-motion-title');
        if (reduceMotion || !titleObserver) title.classList.add('site-motion-visible');
        else titleObserver.observe(title);
    };

    const prepareMedia = wrapper => {
        if (processedMedia.has(wrapper)) return;
        if (wrapper.closest('.home-cinematic-intro,.identity-scroll-scene')) return;
        if (wrapper.matches('.home-hero .hero-product-showcase')) return;
        const directImage = [...wrapper.children].find(child => child.tagName === 'IMG');
        if (!directImage) return;

        processedMedia.add(wrapper);
        wrapper.classList.add('site-motion-media');
        if (getComputedStyle(wrapper).position === 'static') wrapper.classList.add('site-motion-media-positioned');

        const wipe = document.createElement('i');
        wipe.className = 'site-motion-wipe';
        wipe.setAttribute('aria-hidden', 'true');
        wrapper.appendChild(wipe);

        if (reduceMotion || !mediaObserver) wrapper.classList.add('site-motion-media-ready');
        else mediaObserver.observe(wrapper);
    };

    const prepareSection = section => {
        if (processedSections.has(section)) return;
        if (section.classList.contains('ticker') || section.classList.contains('identity-scroll-scene')) return;
        if (!section.querySelector('h2')) return;

        processedSections.add(section);
        section.classList.add('site-motion-section');
        const line = document.createElement('i');
        line.className = 'site-motion-section-line';
        line.setAttribute('aria-hidden', 'true');
        section.prepend(line);

        if (reduceMotion || !sectionObserver) section.classList.add('site-motion-section-visible');
        else sectionObserver.observe(section);
    };

    const addSheen = element => {
        if (!element || element.closest('.identity-scroll-scene') || element.querySelector(':scope > .site-motion-sheen-layer')) return;
        element.classList.add('site-motion-sheen');
        const sheen = document.createElement('i');
        sheen.className = 'site-motion-sheen-layer';
        sheen.setAttribute('aria-hidden', 'true');
        element.appendChild(sheen);
    };

    const enhance = root => {
        if (!(root instanceof Element || root instanceof Document)) return;

        const titles = [
            ...(root.matches?.('h1,h2') ? [root] : []),
            ...root.querySelectorAll('h1,h2')
        ];
        titles.forEach(prepareTitle);

        const mediaSelectors = '.hero-image-wrap,.menu-card-image,.showcase-card,.journey-image,.menu-hero-product,figure';
        const media = [
            ...(root.matches?.(mediaSelectors) ? [root] : []),
            ...root.querySelectorAll(mediaSelectors)
        ];
        media.forEach(prepareMedia);

        const sections = [
            ...(root.matches?.('section') ? [root] : []),
            ...root.querySelectorAll('section')
        ];
        sections.filter(section => section.closest('main')).forEach(prepareSection);

        const sheenSelectors = '.menu-card,.showcase-card,.final-choice-card,.branch-route-step,.franchise-benefit-card,.franchise-step,.franchise-faq-item,.contact-method-card';
        const sheenTargets = [
            ...(root.matches?.(sheenSelectors) ? [root] : []),
            ...root.querySelectorAll(sheenSelectors)
        ];
        sheenTargets.forEach(addSheen);
    };

    enhance(main);

    // Home inserts the franchise shortcut after initial parsing. Enhance later additions too.
    const mutationObserver = new MutationObserver(mutations => {
        mutations.forEach(mutation => mutation.addedNodes.forEach(node => {
            if (node instanceof Element) enhance(node);
        }));
    });
    mutationObserver.observe(main, { childList: true, subtree: true });

    if (!reduceMotion && window.matchMedia?.('(pointer:fine)').matches) {
        let pointerFrame = 0;
        window.addEventListener('pointermove', event => {
            cancelAnimationFrame(pointerFrame);
            pointerFrame = requestAnimationFrame(() => {
                document.body.style.setProperty('--cinematic-glow-x', `${event.clientX}px`);
                document.body.style.setProperty('--cinematic-glow-y', `${event.clientY}px`);
            });
        }, { passive: true });
    } else if (!reduceMotion) {
        let scrollFrame = 0;
        const updateGlow = () => {
            scrollFrame = 0;
            const max = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
            const progress = Math.min(1, Math.max(0, window.scrollY / max));
            document.body.style.setProperty('--cinematic-glow-x', `${68 - progress * 36}vw`);
            document.body.style.setProperty('--cinematic-glow-y', `${28 + progress * 38}vh`);
        };
        window.addEventListener('scroll', () => {
            if (!scrollFrame) scrollFrame = requestAnimationFrame(updateGlow);
        }, { passive: true });
        updateGlow();
    }
})();
