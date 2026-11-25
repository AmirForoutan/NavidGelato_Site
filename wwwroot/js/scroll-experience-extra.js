(() => {
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    const journey = document.querySelector('[data-menu-journey]');
    const journeySteps = [...document.querySelectorAll('[data-journey-step]')];
    const journeyImages = [...document.querySelectorAll('.journey-image')];
    const journeyMark = document.querySelector('.journey-center-mark span');
    const numberCards = [...document.querySelectorAll('[data-count-card]')];
    const counters = [...document.querySelectorAll('[data-counter]')];
    const tiltCards = [...document.querySelectorAll('[data-tilt-card]')];
    const finalChoice = document.querySelector('[data-final-choice]');

    const fa = value => Number(value).toLocaleString('fa-IR', { useGrouping: false });

    const activateJourney = index => {
        journeySteps.forEach((step, i) => step.classList.toggle('active', i === index));
        journeyImages.forEach((image, i) => image.classList.toggle('active', i === index));
        if (journeyMark) journeyMark.textContent = String(index + 1).padStart(2, '0');
    };

    if (journeySteps.length) {
        activateJourney(0);
        if (!reduceMotion && 'IntersectionObserver' in window) {
            const journeyObserver = new IntersectionObserver(entries => {
                const visible = entries
                    .filter(entry => entry.isIntersecting)
                    .sort((a, b) => Math.abs(a.boundingClientRect.top - window.innerHeight * .45) - Math.abs(b.boundingClientRect.top - window.innerHeight * .45));
                if (!visible.length) return;
                const index = journeySteps.indexOf(visible[0].target);
                if (index >= 0) activateJourney(index);
            }, { threshold: [.25, .45, .65], rootMargin: '-18% 0px -28% 0px' });
            journeySteps.forEach(step => journeyObserver.observe(step));
        } else journeySteps.forEach(step => step.classList.add('active'));
    }

    const animateCounter = element => {
        if (element.dataset.counted === '1') return;
        element.dataset.counted = '1';
        const target = Number.parseInt(element.getAttribute('data-counter') || '0', 10);
        if (!Number.isFinite(target)) return;
        if (reduceMotion) { element.textContent = fa(target); return; }

        const duration = target > 100 ? 1350 : 900;
        const start = performance.now();
        const tick = now => {
            const t = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - t, 3);
            element.textContent = fa(Math.round(target * eased));
            if (t < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    };

    if ('IntersectionObserver' in window && !reduceMotion) {
        const numberObserver = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('scroll-in-view');
                entry.target.querySelectorAll('[data-counter]').forEach(animateCounter);
                numberObserver.unobserve(entry.target);
            });
        }, { threshold: .35 });
        numberCards.forEach(card => numberObserver.observe(card));
    } else {
        numberCards.forEach(card => card.classList.add('scroll-in-view'));
        counters.forEach(animateCounter);
    }

    if (!reduceMotion) {
        tiltCards.forEach(card => {
            const max = card.classList.contains('final-choice-card') ? 4 : 3;
            card.addEventListener('pointermove', event => {
                if (event.pointerType === 'touch') return;
                const rect = card.getBoundingClientRect();
                const x = (event.clientX - rect.left) / rect.width - .5;
                const y = (event.clientY - rect.top) / rect.height - .5;
                card.style.setProperty('--tilt-y', `${(x * max * 2).toFixed(2)}deg`);
                card.style.setProperty('--tilt-x', `${(-y * max * 2).toFixed(2)}deg`);
            });
            card.addEventListener('pointerleave', () => {
                card.style.setProperty('--tilt-x', '0deg');
                card.style.setProperty('--tilt-y', '0deg');
            });
        });
    }

    let ticking = false;
    const update = () => {
        ticking = false;
        if (reduceMotion) return;

        if (journey) {
            const rect = journey.getBoundingClientRect();
            const vh = window.innerHeight || 1;
            const progress = Math.max(0, Math.min(1, (vh - rect.top) / (vh + rect.height)));
            const stage = journey.querySelector('.journey-visual-stage');
            if (stage) {
                stage.style.transform = `translate3d(0, ${(progress - .5) * 22}px, 0) rotate(${(progress - .5) * 2.5}deg)`;
            }
        }

        if (finalChoice) {
            const rect = finalChoice.getBoundingClientRect();
            const vh = window.innerHeight || 1;
            const progress = Math.max(0, Math.min(1, (vh - rect.top) / (vh + rect.height)));
            finalChoice.style.setProperty('--final-bg-y', `${(progress - .5) * 95}px`);
        }
    };

    const requestUpdate = () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(update);
    };

    window.addEventListener('scroll', requestUpdate, { passive: true });
    window.addEventListener('resize', requestUpdate, { passive: true });
    requestUpdate();
})();
