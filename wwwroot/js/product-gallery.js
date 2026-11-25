(() => {
    let modal = null;
    let stage = null;
    let imageElement = null;
    let titleElement = null;
    let counterElement = null;
    let thumbsElement = null;
    let prevButton = null;
    let nextButton = null;
    let currentImages = [];
    let currentIndex = 0;
    let touchStartX = null;

    const createModal = () => {
        if (modal) return;

        modal = document.createElement('div');
        modal.className = 'product-lightbox';
        modal.hidden = true;
        modal.setAttribute('data-product-lightbox', '');

        const dialog = document.createElement('section');
        dialog.className = 'product-lightbox-dialog';
        dialog.setAttribute('role', 'dialog');
        dialog.setAttribute('aria-modal', 'true');
        dialog.setAttribute('aria-label', 'گالری تصاویر کالا');

        stage = document.createElement('div');
        stage.className = 'product-lightbox-stage';

        imageElement = document.createElement('img');
        imageElement.className = 'product-lightbox-image';
        imageElement.alt = '';

        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'product-lightbox-close';
        closeButton.setAttribute('aria-label', 'بستن گالری');
        closeButton.textContent = '×';

        prevButton = document.createElement('button');
        prevButton.type = 'button';
        prevButton.className = 'product-lightbox-nav product-lightbox-prev';
        prevButton.setAttribute('aria-label', 'تصویر قبلی');
        prevButton.textContent = '›';

        nextButton = document.createElement('button');
        nextButton.type = 'button';
        nextButton.className = 'product-lightbox-nav product-lightbox-next';
        nextButton.setAttribute('aria-label', 'تصویر بعدی');
        nextButton.textContent = '‹';

        stage.append(imageElement, closeButton, prevButton, nextButton);

        const footer = document.createElement('div');
        footer.className = 'product-lightbox-footer';

        const info = document.createElement('div');
        info.className = 'product-lightbox-info';
        titleElement = document.createElement('strong');
        titleElement.className = 'product-lightbox-title';
        counterElement = document.createElement('span');
        counterElement.className = 'product-lightbox-counter';
        info.append(titleElement, counterElement);

        thumbsElement = document.createElement('div');
        thumbsElement.className = 'product-lightbox-thumbs';
        footer.append(info, thumbsElement);

        dialog.append(stage, footer);
        modal.appendChild(dialog);
        document.body.appendChild(modal);

        closeButton.addEventListener('click', close);
        prevButton.addEventListener('click', () => move(-1));
        nextButton.addEventListener('click', () => move(1));
        modal.addEventListener('click', event => {
            if (event.target === modal) close();
        });

        stage.addEventListener('touchstart', event => {
            touchStartX = event.changedTouches[0]?.clientX ?? null;
        }, { passive: true });

        stage.addEventListener('touchend', event => {
            if (touchStartX === null || currentImages.length < 2) return;
            const endX = event.changedTouches[0]?.clientX ?? touchStartX;
            const delta = endX - touchStartX;
            touchStartX = null;
            if (Math.abs(delta) < 45) return;
            move(delta > 0 ? -1 : 1);
        }, { passive: true });
    };

    const readImages = group => {
        const container = [...document.querySelectorAll('[data-product-gallery-items]')]
            .find(element => element.getAttribute('data-product-gallery-items') === group);
        if (!container) return [];
        return [...container.querySelectorAll('[data-product-gallery-src]')]
            .map(element => ({
                src: element.getAttribute('data-product-gallery-src') || '',
                alt: element.getAttribute('data-product-gallery-alt') || ''
            }))
            .filter(item => item.src);
    };

    const renderThumbs = () => {
        thumbsElement.replaceChildren();
        if (currentImages.length <= 1) return;

        currentImages.forEach((item, index) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `product-lightbox-thumb${index === currentIndex ? ' active' : ''}`;
            button.setAttribute('aria-label', `تصویر ${index + 1}`);
            const thumb = document.createElement('img');
            thumb.src = item.src;
            thumb.alt = item.alt;
            button.appendChild(thumb);
            button.addEventListener('click', () => show(index));
            thumbsElement.appendChild(button);
        });
    };

    const show = index => {
        if (!currentImages.length) return;
        currentIndex = (index + currentImages.length) % currentImages.length;
        const item = currentImages[currentIndex];
        imageElement.src = item.src;
        imageElement.alt = item.alt;
        counterElement.textContent = currentImages.length > 1
            ? `${(currentIndex + 1).toLocaleString('fa-IR')} از ${currentImages.length.toLocaleString('fa-IR')}`
            : '۱ تصویر';
        prevButton.hidden = currentImages.length <= 1;
        nextButton.hidden = currentImages.length <= 1;
        renderThumbs();
        const activeThumb = thumbsElement.querySelector('.product-lightbox-thumb.active');
        activeThumb?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    };

    const move = delta => show(currentIndex + delta);

    const open = trigger => {
        const group = trigger.getAttribute('data-product-gallery-open');
        if (!group) return;
        const images = readImages(group);
        if (!images.length) return;

        createModal();
        currentImages = images;
        titleElement.textContent = trigger.getAttribute('data-product-gallery-title') || 'تصاویر کالا';
        const requestedIndex = Number.parseInt(trigger.getAttribute('data-product-gallery-index') || '0', 10);
        show(Number.isFinite(requestedIndex) ? requestedIndex : 0);

        modal.hidden = false;
        requestAnimationFrame(() => modal.classList.add('visible'));
        document.body.classList.add('product-gallery-open');
    };

    const close = () => {
        if (!modal || modal.hidden) return;
        modal.classList.remove('visible');
        document.body.classList.remove('product-gallery-open');
        window.setTimeout(() => {
            modal.hidden = true;
            imageElement.removeAttribute('src');
            currentImages = [];
        }, 200);
    };

    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-product-gallery-open]');
        if (!trigger) return;
        event.preventDefault();
        open(trigger);
    });

    document.addEventListener('keydown', event => {
        if (!modal || modal.hidden) return;
        if (event.key === 'Escape') close();
        else if (event.key === 'ArrowRight') move(-1);
        else if (event.key === 'ArrowLeft') move(1);
    });
})();
