(() => {
    const header = document.querySelector('[data-site-header]');
    const toggle = document.querySelector('[data-nav-toggle]');
    const nav = document.querySelector('[data-nav]');
    const navClose = document.querySelector('[data-nav-close]');
    const navBackdrop = document.querySelector('[data-nav-backdrop]');

    const onScroll = () => header?.classList.toggle('scrolled', window.scrollY > 24);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });

    const closeNav = () => {
        nav?.classList.remove('open');
        toggle?.setAttribute('aria-expanded', 'false');
        document.body.classList.remove('nav-open');
    };
    const openNav = () => {
        nav?.classList.add('open');
        toggle?.setAttribute('aria-expanded', 'true');
        document.body.classList.add('nav-open');
    };

    toggle?.addEventListener('click', () => nav?.classList.contains('open') ? closeNav() : openNav());
    navClose?.addEventListener('click', closeNav);
    navBackdrop?.addEventListener('click', closeNav);
    nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', closeNav));

    const openingBar = document.querySelector('[data-opening-bar]');
    const openingModal = document.querySelector('[data-opening-modal]');
    const openingDetailsButtons = [...document.querySelectorAll('[data-opening-details]')];
    const openingCloseButtons = [...document.querySelectorAll('[data-opening-close]')];
    const openingCountdowns = [...document.querySelectorAll('[data-opening-countdown]')];
    const openingExpiryCards = [...document.querySelectorAll('[data-opening-expiry]')];

    const parseOpeningEnd = value => {
        const timestamp = value ? Date.parse(value) : NaN;
        return Number.isFinite(timestamp) ? timestamp : null;
    };

    const primaryOpeningEnd = parseOpeningEnd(openingBar?.getAttribute('data-opening-end') ?? openingModal?.getAttribute('data-opening-end'));
    const openingKey = openingModal?.getAttribute('data-opening-key');
    const storageKey = openingKey ? `navidpro.opening.dismissed.${openingKey}` : null;

    const wasOpeningDismissed = () => {
        if (!storageKey) return false;
        try { return localStorage.getItem(storageKey) === '1'; } catch { return false; }
    };
    const rememberOpeningDismissed = () => {
        if (!storageKey) return;
        try { localStorage.setItem(storageKey, '1'); } catch { }
    };
    const showOpeningModal = () => {
        if (!openingModal || (primaryOpeningEnd && Date.now() >= primaryOpeningEnd)) return;
        openingModal.hidden = false;
        requestAnimationFrame(() => openingModal.classList.add('visible'));
        document.body.classList.add('opening-modal-open');
    };
    const closeOpeningModal = (remember = true) => {
        if (!openingModal) return;
        openingModal.classList.remove('visible');
        document.body.classList.remove('opening-modal-open');
        if (remember) rememberOpeningDismissed();
        window.setTimeout(() => { openingModal.hidden = true; }, 220);
    };

    openingDetailsButtons.forEach(button => button.addEventListener('click', showOpeningModal));
    openingCloseButtons.forEach(button => button.addEventListener('click', () => closeOpeningModal(true)));
    openingModal?.addEventListener('click', event => { if (event.target === openingModal) closeOpeningModal(true); });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        if (nav?.classList.contains('open')) { closeNav(); return; }
        if (openingModal && !openingModal.hidden) closeOpeningModal(true);
    });

    const toPersianNumber = value => Number(value).toLocaleString('fa-IR');
    const updateOpeningCountdown = () => {
        if (!primaryOpeningEnd) return;
        const remaining = primaryOpeningEnd - Date.now();
        if (remaining <= 0) {
            openingBar?.remove();
            if (openingModal) { openingModal.hidden = true; openingModal.remove(); }
            document.body.classList.remove('has-opening-announcement', 'opening-modal-open');
            return;
        }
        const minutes = Math.floor(remaining / 60000);
        const days = Math.floor(minutes / 1440);
        const hours = Math.floor((minutes % 1440) / 60);
        const mins = minutes % 60;
        let text;
        if (days > 0) text = `${toPersianNumber(days)} روز و ${toPersianNumber(hours)} ساعت تا افتتاح`;
        else if (hours > 0) text = `${toPersianNumber(hours)} ساعت و ${toPersianNumber(mins)} دقیقه تا افتتاح`;
        else if (mins > 0) text = `${toPersianNumber(mins)} دقیقه تا افتتاح`;
        else text = 'لحظاتی تا افتتاح';
        openingCountdowns.forEach(element => element.textContent = text);
    };

    const refreshExpiredBranchOpenings = () => {
        const now = Date.now();
        openingExpiryCards.forEach(card => {
            const end = parseOpeningEnd(card.getAttribute('data-opening-expiry'));
            if (!end || now < end || card.dataset.openingExpired === '1') return;
            card.dataset.openingExpired = '1';
            card.dataset.status = '0';
            card.classList.remove('opening-soon');
            card.classList.add('open');
            card.querySelectorAll('[data-opening-only="true"]').forEach(element => element.remove());
        });
    };

    updateOpeningCountdown();
    refreshExpiredBranchOpenings();
    if (primaryOpeningEnd) window.setInterval(updateOpeningCountdown, 30000);
    if (openingExpiryCards.length) window.setInterval(refreshExpiredBranchOpenings, 30000);
    if (openingModal && !wasOpeningDismissed() && (!primaryOpeningEnd || Date.now() < primaryOpeningEnd)) window.setTimeout(showOpeningModal, 850);

    const reveals = document.querySelectorAll('.reveal');
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => { if (entry.isIntersecting) { entry.target.classList.add('visible'); observer.unobserve(entry.target); } });
        }, { threshold: 0.12 });
        reveals.forEach(el => observer.observe(el));
    } else reveals.forEach(el => el.classList.add('visible'));

    const menuSections = [...document.querySelectorAll('[data-menu-section]')];
    const menuLinks = [...document.querySelectorAll('[data-category-link]')];
    const menuNav = document.querySelector('[data-menu-nav]');
    let menuScrollFrame = 0;
    let activeMenuId = null;
    const setActiveMenu = id => {
        if (!id || id === activeMenuId) return;
        activeMenuId = id;
        let activeLink = null;
        menuLinks.forEach(link => { const active = link.getAttribute('data-category-link') === id; link.classList.toggle('active', active); if (active) activeLink = link; });
        activeLink?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    };
    const updateActiveMenuFromScroll = () => {
        menuScrollFrame = 0;
        if (!menuSections.length) return;
        const headerHeight = header?.getBoundingClientRect().height ?? 0;
        const navHeight = menuNav?.getBoundingClientRect().height ?? 0;
        const activationLine = headerHeight + navHeight + 34;
        let current = menuSections[0];
        for (const section of menuSections) { const rect = section.getBoundingClientRect(); if (rect.top <= activationLine) current = section; else break; }
        if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 8) current = menuSections[menuSections.length - 1];
        setActiveMenu(current?.getAttribute('data-menu-section'));
    };
    if (menuSections.length && menuLinks.length) {
        updateActiveMenuFromScroll();
        window.addEventListener('scroll', () => { if (!menuScrollFrame) menuScrollFrame = window.requestAnimationFrame(updateActiveMenuFromScroll); }, { passive: true });
        window.addEventListener('resize', updateActiveMenuFromScroll, { passive: true });
    }
    menuLinks.forEach(link => link.addEventListener('click', event => {
        const href = link.getAttribute('href');
        if (!href?.startsWith('#')) return;
        event.preventDefault();
        const target = document.querySelector(href); if (!target) return;
        const navHeight = menuNav?.getBoundingClientRect().height ?? 0;
        const headerHeight = header?.getBoundingClientRect().height ?? 0;
        const top = target.getBoundingClientRect().top + window.scrollY - navHeight - headerHeight - 14;
        setActiveMenu(link.getAttribute('data-category-link'));
        window.scrollTo({ top, behavior: 'smooth' });
    }));

    const branchSearch = document.querySelector('#branch-search');
    const branchCards = [...document.querySelectorAll('[data-branch]')];
    const branchGrid = document.querySelector('#branches-grid');
    const branchCount = document.querySelector('#branch-count');
    const branchEmpty = document.querySelector('#branch-empty');
    const applyBranchSearch = () => {
        const term = branchSearch?.value.trim().toLocaleLowerCase('fa') ?? '';
        let visible = 0;
        branchCards.forEach(card => { const matches = !term || (card.getAttribute('data-branch') ?? '').toLocaleLowerCase('fa').includes(term); card.hidden = !matches; if (matches) visible++; });
        if (branchCount) branchCount.textContent = `${visible} شعبه`;
        if (branchEmpty) branchEmpty.hidden = visible !== 0;
    };
    branchSearch?.addEventListener('input', applyBranchSearch);

    const findNearestButton = document.querySelector('[data-find-nearest]');
    const locationStatus = document.querySelector('[data-location-status]');
    const toRadians = value => value * Math.PI / 180;
    const distanceKm = (lat1, lng1, lat2, lng2) => {
        const earthRadiusKm = 6371, dLat = toRadians(lat2 - lat1), dLng = toRadians(lng2 - lng1);
        const a = Math.sin(dLat / 2) ** 2 + Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) * Math.sin(dLng / 2) ** 2;
        return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    };
    const formatDistance = km => km < 1 ? `${Math.max(1, Math.round(km * 1000))} متر فاصله تقریبی` : `${km.toLocaleString('fa-IR', { maximumFractionDigits: 1 })} کیلومتر فاصله تقریبی`;
    findNearestButton?.addEventListener('click', () => {
        if (!navigator.geolocation) { if (locationStatus) locationStatus.textContent = 'مرورگر شما امکان دریافت موقعیت مکانی را پشتیبانی نمی‌کند.'; return; }
        findNearestButton.disabled = true; findNearestButton.textContent = 'در حال پیدا کردن...'; if (locationStatus) locationStatus.textContent = 'در حال دریافت موقعیت مکانی شما...';
        navigator.geolocation.getCurrentPosition(position => {
            const userLat = position.coords.latitude, userLng = position.coords.longitude, located = [];
            branchCards.forEach(card => {
                card.classList.remove('nearest'); const badge = card.querySelector('[data-nearest-badge]'); const distanceEl = card.querySelector('[data-branch-distance]'); if (badge) badge.hidden = true; if (distanceEl) distanceEl.hidden = true;
                if (card.getAttribute('data-status') !== '0') return;
                const lat = Number.parseFloat(card.getAttribute('data-lat') ?? ''), lng = Number.parseFloat(card.getAttribute('data-lng') ?? ''); if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
                located.push({ card, distance: distanceKm(userLat, userLng, lat, lng) });
            });
            located.sort((a, b) => a.distance - b.distance);
            if (!located.length) { if (locationStatus) locationStatus.textContent = 'برای شعب دایر هنوز مختصات مکانی ثبت نشده است.'; }
            else {
                const nearest = located[0];
                located.forEach(({ card, distance }) => { card.dataset.distance = String(distance); const distanceEl = card.querySelector('[data-branch-distance]'); if (distanceEl) { distanceEl.textContent = formatDistance(distance); distanceEl.hidden = false; } });
                nearest.card.classList.add('nearest'); const badge = nearest.card.querySelector('[data-nearest-badge]'); if (badge) badge.hidden = false;
                if (branchGrid) { located.forEach(({ card }) => branchGrid.appendChild(card)); branchCards.filter(card => card.getAttribute('data-status') !== '0').forEach(card => branchGrid.appendChild(card)); }
                if (branchSearch) branchSearch.value = ''; applyBranchSearch();
                const branchName = nearest.card.querySelector('h2')?.textContent?.trim() ?? 'نزدیک‌ترین شعبه'; if (locationStatus) locationStatus.textContent = `${branchName} با ${formatDistance(nearest.distance)} نزدیک‌ترین شعبه دایر به شماست.`; nearest.card.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            findNearestButton.disabled = false; findNearestButton.textContent = 'محاسبه دوباره';
        }, error => {
            const messages = { 1: 'برای پیدا کردن نزدیک‌ترین شعبه باید اجازه دسترسی به موقعیت مکانی را بدهی.', 2: 'موقعیت مکانی شما در حال حاضر قابل دریافت نیست.', 3: 'دریافت موقعیت مکانی بیش از حد طول کشید. دوباره تلاش کن.' };
            if (locationStatus) locationStatus.textContent = messages[error.code] ?? 'دریافت موقعیت مکانی با خطا روبه‌رو شد.'; findNearestButton.disabled = false; findNearestButton.textContent = 'تلاش دوباره';
        }, { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 });
    });
})();
