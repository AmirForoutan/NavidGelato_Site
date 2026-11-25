(() => {
    const executingScript = document.currentScript
        || [...document.scripts].reverse().find(script => script.src.includes('/js/home-intro'));
    const resolveAssetUrl = relativePath => executingScript?.src
        ? new URL(relativePath, executingScript.src).toString()
        : relativePath;

    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    const homeHero = document.querySelector('.home-hero');
    if (!homeHero) return;

    if (!document.querySelector('link[data-home-polish]')) {
        const polishCss = document.createElement('link');
        polishCss.rel = 'stylesheet';
        polishCss.href = resolveAssetUrl('../css/home-polish.css');
        polishCss.dataset.homePolish = '1';
        document.head.appendChild(polishCss);
    }

    const loadPolishScript = () => {
        if (document.querySelector('script[data-home-polish]')) return;
        const polishScript = document.createElement('script');
        polishScript.src = resolveAssetUrl('home-polish.js');
        polishScript.defer = true;
        polishScript.dataset.homePolish = '1';
        document.body.appendChild(polishScript);
    };

    if (reduceMotion) {
        loadPolishScript();
        return;
    }

    const introImageA = homeHero.querySelector('.hero-image-wrap img')?.src
        || document.querySelector('.showcase-card img')?.src
        || '';
    const introImageB = document.querySelector('.showcase-card:nth-child(2) img')?.src
        || document.querySelector('.identity-product img')?.src
        || introImageA;

    document.body.classList.add('home-intro-lock', 'home-intro-preparing');

    const intro = document.createElement('div');
    intro.className = 'home-cinematic-intro';
    intro.setAttribute('aria-label', 'معرفی نوید ژلاتو');
    intro.innerHTML = `
        <div class="intro-noise" aria-hidden="true"></div>
        <div class="intro-scan" aria-hidden="true"></div>
        <div class="intro-grid-ring" aria-hidden="true"></div>
        <div class="intro-burst" aria-hidden="true"></div>
        <div class="intro-line" aria-hidden="true"></div>
        <i class="intro-particle p1"></i><i class="intro-particle p2"></i><i class="intro-particle p3"></i><i class="intro-particle p4"></i><i class="intro-particle p5"></i>
        <i class="intro-particle p6"></i><i class="intro-particle p7"></i><i class="intro-particle p8"></i><i class="intro-particle p9"></i><i class="intro-particle p10"></i>
        <div class="intro-stage">
            <figure class="intro-product intro-product-a">
                <span class="intro-product-index">01</span>
                <img src="${introImageA}" alt="ساندویچ نوید ژلاتو" />
                <figcaption class="intro-product-label"><small>CHARACTER 01</small><b>آرچر</b></figcaption>
            </figure>
            <figure class="intro-product intro-product-b">
                <span class="intro-product-index">02</span>
                <img src="${introImageB}" alt="ساندویچ نوید ژلاتو" />
                <figcaption class="intro-product-label"><small>CHARACTER 02</small><b>PRO SERIES</b></figcaption>
            </figure>
            <div class="intro-center">
                <div class="intro-brand-mark">NP</div>
                <div class="intro-brand-name"><span>NAVID</span><span>PRO</span></div>
                <p class="intro-tagline">اینجا هر ساندویچ فقط یک اسم نیست؛ <strong>یک شخصیت داره.</strong></p>
                <span class="intro-kicker">TASTE / CHARACTER / NAVID GELATO</span>
            </div>
        </div>
        <div class="intro-progress"><span>LOADING EXPERIENCE</span><div class="intro-progress-track"><i></i></div></div>
        <button class="intro-skip" type="button">رد کردن معرفی</button>
        <div class="intro-curtain" aria-hidden="true"></div>
    `;

    document.body.prepend(intro);

    let finished = false;
    const finish = () => {
        if (finished) return;
        finished = true;
        intro.classList.add('is-exiting');
        document.body.classList.remove('home-intro-preparing');
        document.body.classList.add('home-intro-reveal');

        window.setTimeout(() => {
            intro.remove();
            document.body.classList.remove('home-intro-lock');
            homeHero.classList.add('intro-pointer-ready');
        }, 1200);
    };

    intro.querySelector('.intro-skip')?.addEventListener('click', finish);
    window.setTimeout(finish, 4550);

    window.addEventListener('keydown', event => {
        if (event.key === 'Escape' && !finished) finish();
    }, { once: true });

    let pointerFrame = 0;
    const updatePointer = event => {
        if (!homeHero.classList.contains('intro-pointer-ready')) return;
        if (event.pointerType === 'touch') return;
        cancelAnimationFrame(pointerFrame);
        pointerFrame = requestAnimationFrame(() => {
            const rect = homeHero.getBoundingClientRect();
            if (rect.bottom <= 0 || rect.top >= window.innerHeight) return;
            const x = (event.clientX - rect.left) / Math.max(1, rect.width) - .5;
            const y = (event.clientY - rect.top) / Math.max(1, rect.height) - .5;
            homeHero.style.setProperty('--intro-pointer-y', `${(x * 5).toFixed(2)}deg`);
            homeHero.style.setProperty('--intro-pointer-x', `${(-y * 4).toFixed(2)}deg`);
            homeHero.style.setProperty('--intro-card-x', `${(x * 18).toFixed(1)}px`);
            homeHero.style.setProperty('--intro-card-y', `${(y * 14).toFixed(1)}px`);
        });
    };

    homeHero.addEventListener('pointermove', updatePointer);
    homeHero.addEventListener('pointerleave', () => {
        homeHero.style.setProperty('--intro-pointer-x', '0deg');
        homeHero.style.setProperty('--intro-pointer-y', '0deg');
        homeHero.style.setProperty('--intro-card-x', '0px');
        homeHero.style.setProperty('--intro-card-y', '0px');
    });

    loadPolishScript();
})();
