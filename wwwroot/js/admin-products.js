(() => {
    const root = document.querySelector('[data-products-root]');
    if (!root) return;

    const pageUrl = root.dataset.pageUrl || window.location.pathname;
    const canCreate = root.dataset.canCreate === 'true';
    const canEdit = root.dataset.canEdit === 'true';
    const canDelete = root.dataset.canDelete === 'true';
    const canImages = root.dataset.canImages === 'true';

    const modal = document.querySelector('[data-product-modal]');
    const backdrop = document.querySelector('[data-product-backdrop]');
    const form = document.querySelector('[data-product-form]');
    const errorsBox = document.querySelector('[data-product-errors]');
    const title = document.querySelector('[data-product-modal-title]');
    const submitButton = document.querySelector('[data-product-submit]');
    const status = document.querySelector('[data-products-status]');
    const imagesSection = document.querySelector('[data-product-images]');
    const imageGrid = document.querySelector('[data-product-image-grid]');
    const imageCount = document.querySelector('[data-product-image-count]');
    const token = form?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    const field = name => form?.querySelector(`[data-field="${name}"]`);
    const handlerUrl = (handler, params = {}) => {
        const url = new URL(pageUrl, window.location.origin);
        url.searchParams.set('handler', handler);
        Object.entries(params).forEach(([key, value]) => url.searchParams.set(key, value));
        return url.toString();
    };

    const escapeHtml = value => String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');

    const toast = (message, kind = 'success') => {
        const element = document.createElement('div');
        element.className = `admin-ajax-toast ${kind}`;
        element.textContent = message;
        document.body.appendChild(element);
        requestAnimationFrame(() => element.classList.add('show'));
        window.setTimeout(() => {
            element.classList.remove('show');
            window.setTimeout(() => element.remove(), 250);
        }, 2600);
    };

    const setBusy = busy => {
        if (submitButton) {
            submitButton.disabled = busy;
            submitButton.textContent = busy ? 'در حال ذخیره…' : 'ذخیره کالا';
        }
    };

    const clearErrors = () => {
        if (!errorsBox) return;
        errorsBox.hidden = true;
        errorsBox.innerHTML = '';
    };

    const showErrors = errors => {
        if (!errorsBox) return;
        const messages = Object.values(errors || {}).flat().filter(Boolean);
        errorsBox.innerHTML = messages.length
            ? `<ul>${messages.map(message => `<li>${escapeHtml(message)}</li>`).join('')}</ul>`
            : 'خطا در ذخیره اطلاعات.';
        errorsBox.hidden = false;
    };

    const openModal = () => {
        if (!modal || !backdrop) return;
        modal.hidden = false;
        backdrop.hidden = false;
        document.body.classList.add('product-editor-open');
        requestAnimationFrame(() => {
            modal.classList.add('is-open');
            backdrop.classList.add('is-open');
        });
    };

    const closeModal = () => {
        if (!modal || !backdrop) return;
        modal.classList.remove('is-open');
        backdrop.classList.remove('is-open');
        document.body.classList.remove('product-editor-open');
        window.setTimeout(() => {
            modal.hidden = true;
            backdrop.hidden = true;
        }, 180);
        clearErrors();
    };

    const resetForm = () => {
        form?.reset();
        if (field('id')) field('id').value = '0';
        if (field('isActive')) field('isActive').checked = true;
        if (field('isFeatured')) field('isFeatured').checked = false;
        if (title) title.textContent = 'کالای جدید';
        if (imagesSection) imagesSection.hidden = true;
        if (imageGrid) imageGrid.innerHTML = '';
        if (imageCount) imageCount.textContent = '0 تصویر';
        clearErrors();
    };

    const renderImages = product => {
        if (!imagesSection || !imageGrid || !imageCount) return;
        const images = product?.images || [];
        imageCount.textContent = `${images.length} تصویر`;
        imagesSection.hidden = product?.id <= 0 || images.length === 0;
        imageGrid.innerHTML = images.map(image => `
            <article class="admin-image-card ${image.isPrimary ? 'primary' : ''}">
                <div class="admin-image-preview static">
                    <img src="${escapeHtml(image.filePath)}" alt="${escapeHtml(image.altText || product.name)}" />
                    ${image.isPrimary ? '<span class="ajax-primary-badge">اصلی</span>' : ''}
                </div>
                <div class="admin-image-meta">
                    <b>${image.isPrimary ? 'تصویر اصلی' : 'تصویر کالا'}</b>
                </div>
                ${canImages ? `
                <div class="admin-image-actions">
                    ${image.isPrimary ? '' : `<button type="button" data-image-primary="${image.id}">انتخاب به‌عنوان اصلی</button>`}
                    <button type="button" class="danger" data-image-delete="${image.id}">حذف تصویر</button>
                </div>` : ''}
            </article>
        `).join('');
    };

    const populateForm = product => {
        if (!product || !form) return;
        field('id').value = product.id ?? 0;
        field('productCode').value = product.productCode ?? '';
        field('name').value = product.name ?? '';
        field('menuCategoryId').value = product.menuCategoryId ?? '';
        field('price').value = product.price ?? 0;
        field('sortOrder').value = product.sortOrder ?? 0;
        field('description').value = product.description ?? '';
        field('isFeatured').checked = Boolean(product.isFeatured);
        field('isActive').checked = Boolean(product.isActive);
        if (field('newImages')) field('newImages').value = '';
        if (title) title.textContent = `ویرایش ${product.name || 'کالا'}`;
        renderImages(product);
        clearErrors();
    };

    const loadEditor = async id => {
        if (!canEdit) return;
        clearErrors();
        try {
            const response = await fetch(handlerUrl('Editor', { id }), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!response.ok) throw new Error('load');
            const product = await response.json();
            populateForm(product);
            openModal();
        } catch {
            toast('اطلاعات کالا دریافت نشد.', 'error');
        }
    };

    const postAction = async (handler, params = {}) => {
        const body = new FormData();
        body.append('__RequestVerificationToken', token);
        Object.entries(params).forEach(([key, value]) => body.append(key, value));
        const response = await fetch(handlerUrl(handler), {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body
        });
        if (!response.ok) throw new Error('request');
        return response.json();
    };

    const tableElement = document.querySelector('#products-datatable');
    if (!tableElement || typeof window.DataTable !== 'function') {
        if (status) status.textContent = 'DataTable بارگذاری نشد';
        return;
    }

    const table = new DataTable(tableElement, {
        ajax: {
            url: handlerUrl('List'),
            dataSrc: 'data'
        },
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        order: [[3, 'asc'], [5, 'asc']],
        stateSave: true,
        autoWidth: false,
        layout: {
            topStart: 'pageLength',
            topEnd: 'search',
            bottomStart: 'info',
            bottomEnd: 'paging'
        },
        language: {
            search: '',
            searchPlaceholder: 'جستجو بین کد، نام، دسته‌بندی و توضیحات…',
            lengthMenu: 'نمایش _MENU_ کالا',
            info: 'نمایش _START_ تا _END_ از _TOTAL_ کالا',
            infoEmpty: 'کالایی وجود ندارد',
            zeroRecords: 'کالایی با این جستجو پیدا نشد',
            emptyTable: 'هنوز کالایی ثبت نشده',
            paginate: { next: 'بعدی', previous: 'قبلی' }
        },
        columns: [
            {
                data: 'primaryImage',
                orderable: false,
                searchable: false,
                width: '66px',
                render: (data, type, row) => type === 'display'
                    ? `<img class="product-dt-thumb" src="${escapeHtml(data)}" alt="${escapeHtml(row.name)}" />`
                    : data
            },
            {
                data: 'productCode',
                render: (data, type) => type === 'display' ? (data ?? '<span class="muted-code">—</span>') : (data ?? '')
            },
            { data: 'name' },
            { data: 'categoryName' },
            {
                data: 'price',
                render: (data, type) => type === 'display'
                    ? `<strong class="product-dt-price">${Number(data || 0).toLocaleString('fa-IR')} <small>تومان</small></strong>`
                    : data
            },
            { data: 'sortOrder' },
            {
                data: 'isActive',
                render: (data, type, row) => {
                    if (type !== 'display') return data ? 'فعال' : 'غیرفعال';
                    const featured = row.isFeatured ? '<span class="product-dt-featured">پیشنهادی</span>' : '';
                    return `<span class="status ${data ? 'on' : 'off'}">${data ? 'فعال' : 'غیرفعال'}</span>${featured}`;
                }
            },
            {
                data: 'imageCount',
                render: (data, type) => type === 'display' ? `<span class="product-dt-count">${data} تصویر</span>` : data
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: (data, type, row) => {
                    if (type !== 'display') return '';
                    const edit = canEdit ? `<button type="button" class="product-dt-action edit" data-product-edit="${row.id}">ویرایش</button>` : '';
                    const del = canDelete ? `<button type="button" class="product-dt-action delete" data-product-delete="${row.id}" data-product-name="${escapeHtml(row.name)}">حذف</button>` : '';
                    return `<div class="product-dt-actions">${edit}${del}</div>`;
                }
            },
            { data: 'description', visible: false, searchable: true }
        ],
        initComplete: () => {
            if (status) status.textContent = 'آماده';
        }
    });

    table.on('xhr', (event, settings, json) => {
        if (status) status.textContent = `${json?.data?.length ?? 0} کالا`;
    });

    document.querySelector('[data-product-new]')?.addEventListener('click', () => {
        if (!canCreate) return;
        resetForm();
        openModal();
        window.setTimeout(() => field('productCode')?.focus(), 120);
    });

    tableElement.addEventListener('click', async event => {
        const editButton = event.target.closest('[data-product-edit]');
        if (editButton) {
            await loadEditor(editButton.dataset.productEdit);
            return;
        }

        const deleteButton = event.target.closest('[data-product-delete]');
        if (!deleteButton || !canDelete) return;

        const name = deleteButton.dataset.productName || 'این کالا';
        if (!window.confirm(`${name} حذف شود؟`)) return;

        try {
            await postAction('DeleteAjax', { id: deleteButton.dataset.productDelete });
            table.ajax.reload(null, false);
            toast('کالا حذف شد.');
        } catch {
            toast('حذف کالا انجام نشد.', 'error');
        }
    });

    form?.addEventListener('submit', async event => {
        event.preventDefault();
        clearErrors();
        setBusy(true);

        try {
            const response = await fetch(handlerUrl('SaveAjax'), {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(form)
            });

            const payload = await response.json().catch(() => ({}));
            if (!response.ok) {
                showErrors(payload.errors);
                return;
            }

            populateForm(payload.product);
            table.ajax.reload(null, false);
            toast(payload.message || 'کالا ذخیره شد.');
        } catch {
            showErrors({ request: ['ارتباط با سرور برقرار نشد.'] });
        } finally {
            setBusy(false);
        }
    });

    imageGrid?.addEventListener('click', async event => {
        const primary = event.target.closest('[data-image-primary]');
        const remove = event.target.closest('[data-image-delete]');

        try {
            if (primary) {
                const payload = await postAction('SetPrimaryAjax', { imageId: primary.dataset.imagePrimary });
                populateForm(payload.product);
                table.ajax.reload(null, false);
                toast('تصویر اصلی تغییر کرد.');
                return;
            }

            if (remove) {
                if (!window.confirm('این تصویر حذف شود؟')) return;
                const payload = await postAction('DeleteImageAjax', { imageId: remove.dataset.imageDelete });
                populateForm(payload.product);
                table.ajax.reload(null, false);
                toast('تصویر حذف شد.');
            }
        } catch {
            toast('عملیات تصویر انجام نشد.', 'error');
        }
    });

    document.querySelector('[data-product-close]')?.addEventListener('click', closeModal);
    document.querySelector('[data-product-cancel]')?.addEventListener('click', closeModal);
    backdrop?.addEventListener('click', closeModal);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && modal && !modal.hidden) closeModal();
    });
})();
