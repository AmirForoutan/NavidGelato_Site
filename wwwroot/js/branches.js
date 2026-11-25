(() => {
    const executingScript = document.currentScript
        || [...document.scripts].reverse().find(script => script.src.includes('/js/branches'));
    const resolveAssetUrl = relativePath => executingScript?.src
        ? new URL(relativePath, executingScript.src).toString()
        : relativePath;

    if (!document.querySelector('link[data-branches-motion]')) {
        const motionCss = document.createElement('link');
        motionCss.rel = 'stylesheet';
        motionCss.href = resolveAssetUrl('../css/branches-motion.css');
        motionCss.dataset.branchesMotion = '1';
        document.head.appendChild(motionCss);
    }

    if (!document.querySelector('script[data-branches-motion]')) {
        const motionScript = document.createElement('script');
        motionScript.src = resolveAssetUrl('branches-motion.js');
        motionScript.defer = true;
        motionScript.dataset.branchesMotion = '1';
        document.body.appendChild(motionScript);
    }

    const search = document.querySelector('#branch-search');
    const cards = [...document.querySelectorAll('#branches-grid [data-branch]')];
    const count = document.querySelector('#branch-count');
    const empty = document.querySelector('#branch-empty');
    const nearestButton = document.querySelector('[data-find-nearest]');
    const locationStatus = document.querySelector('[data-location-status]');
    const branchGrid = document.querySelector('#branches-grid');

    if (!search || !cards.length) return;

    const normalize = value => (value ?? '')
        .toString()
        .normalize('NFKC')
        .replace(/[يى]/g, 'ی')
        .replace(/ك/g, 'ک')
        .replace(/[ۀة]/g, 'ه')
        .replace(/[ؤ]/g, 'و')
        .replace(/[إأٱ]/g, 'ا')
        .replace(/[\u064B-\u065F\u0670]/g, '')
        .replace(/[\u200C\u200D\u200E\u200F]/g, ' ')
        .replace(/\s+/g, ' ')
        .trim()
        .toLocaleLowerCase('fa-IR');

    const searchableText = new Map(
        cards.map(card => [card, normalize(card.getAttribute('data-branch') || card.textContent)])
    );

    const render = () => {
        const term = normalize(search.value);
        const terms = term ? term.split(' ').filter(Boolean) : [];
        let visible = 0;

        cards.forEach(card => {
            const haystack = searchableText.get(card) ?? '';
            const matches = terms.length === 0 || terms.every(part => haystack.includes(part));

            card.style.display = matches ? '' : 'none';
            card.hidden = !matches;
            if (matches) visible++;
        });

        if (count) count.textContent = `${visible.toLocaleString('fa-IR')} شعبه`;
        if (empty) empty.hidden = visible !== 0;
        search.setAttribute('aria-label', `جستجو بین ${cards.length.toLocaleString('fa-IR')} شعبه`);
    };

    search.addEventListener('input', render);
    search.addEventListener('search', render);
    search.addEventListener('compositionend', render);

    const toRadians = value => value * Math.PI / 180;
    const distanceKm = (lat1, lng1, lat2, lng2) => {
        const earthRadiusKm = 6371;
        const dLat = toRadians(lat2 - lat1);
        const dLng = toRadians(lng2 - lng1);
        const a = Math.sin(dLat / 2) ** 2
            + Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) * Math.sin(dLng / 2) ** 2;
        return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    };

    const formatDistance = km => km < 1
        ? `${Math.max(1, Math.round(km * 1000)).toLocaleString('fa-IR')} متر فاصله تقریبی`
        : `${km.toLocaleString('fa-IR', { maximumFractionDigits: 1 })} کیلومتر فاصله تقریبی`;

    const resetDistanceState = () => {
        cards.forEach(card => {
            card.classList.remove('nearest');
            delete card.dataset.distance;
            const badge = card.querySelector('[data-nearest-badge]');
            const distanceEl = card.querySelector('[data-branch-distance]');
            if (badge) badge.hidden = true;
            if (distanceEl) {
                distanceEl.hidden = true;
                distanceEl.textContent = '';
            }
        });
    };

    nearestButton?.addEventListener('click', event => {
        event.preventDefault();
        event.stopImmediatePropagation();

        search.value = '';
        render();
        resetDistanceState();

        if (!navigator.geolocation) {
            if (locationStatus) locationStatus.textContent = 'مرورگر شما امکان دریافت موقعیت مکانی را پشتیبانی نمی‌کند.';
            return;
        }

        nearestButton.disabled = true;
        nearestButton.textContent = 'در حال پیدا کردن...';
        if (locationStatus) locationStatus.textContent = 'در حال دریافت موقعیت مکانی شما...';

        navigator.geolocation.getCurrentPosition(position => {
            const userLat = position.coords.latitude;
            const userLng = position.coords.longitude;
            const located = [];

            cards.forEach(card => {
                if (card.getAttribute('data-status') !== '0') return;

                const lat = Number.parseFloat(card.getAttribute('data-lat') ?? '');
                const lng = Number.parseFloat(card.getAttribute('data-lng') ?? '');
                if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

                located.push({ card, distance: distanceKm(userLat, userLng, lat, lng) });
            });

            located.sort((a, b) => a.distance - b.distance);

            if (!located.length) {
                if (locationStatus) locationStatus.textContent = 'برای شعب دایر هنوز مختصات مکانی ثبت نشده است.';
            } else {
                const nearby = located.filter(item => item.distance <= 15);

                nearby.forEach(({ card, distance }) => {
                    card.dataset.distance = String(distance);
                    const distanceEl = card.querySelector('[data-branch-distance]');
                    if (distanceEl) {
                        distanceEl.textContent = formatDistance(distance);
                        distanceEl.hidden = false;
                    }
                });

                if (nearby.length > 0) {
                    const nearest = nearby[0];
                    nearest.card.classList.add('nearest');
                    const badge = nearest.card.querySelector('[data-nearest-badge]');
                    if (badge) badge.hidden = false;

                    if (branchGrid) {
                        located.forEach(({ card }) => branchGrid.appendChild(card));
                        cards.filter(card => card.getAttribute('data-status') !== '0').forEach(card => branchGrid.appendChild(card));
                    }

                    const branchName = nearest.card.querySelector('h2')?.textContent?.trim() ?? 'نزدیک‌ترین شعبه';
                    if (locationStatus) locationStatus.textContent = `${branchName} با ${formatDistance(nearest.distance)} نزدیک‌ترین شعبه دایر در شعاع ۱۵ کیلومتری شماست.`;
                    nearest.card.scrollIntoView({ behavior: 'smooth', block: 'center' });
                } else {
                    if (locationStatus) locationStatus.textContent = 'در شعاع ۱۵ کیلومتری موقعیت شما شعبه دایری پیدا نشد.';
                }
            }

            nearestButton.disabled = false;
            nearestButton.textContent = 'محاسبه دوباره';
        }, error => {
            const messages = {
                1: 'برای پیدا کردن نزدیک‌ترین شعبه باید اجازه دسترسی به موقعیت مکانی را بدهی.',
                2: 'موقعیت مکانی شما در حال حاضر قابل دریافت نیست.',
                3: 'دریافت موقعیت مکانی بیش از حد طول کشید. دوباره تلاش کن.'
            };
            if (locationStatus) locationStatus.textContent = messages[error.code] ?? 'دریافت موقعیت مکانی با خطا روبه‌رو شد.';
            nearestButton.disabled = false;
            nearestButton.textContent = 'تلاش دوباره';
        }, { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 });
    }, { capture: true });

    render();
})();
