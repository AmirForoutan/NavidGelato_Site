(() => {
    const normalize = value => String(value ?? '')
        .toLocaleLowerCase('fa-IR')
        .replace(/ي/g, 'ی')
        .replace(/ك/g, 'ک')
        .replace(/\u200c/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();

    const enhanceTable = (table, index) => {
        if (!table || table.dataset.adminSearchReady === 'true') return;

        // Products already uses DataTables with its own global search.
        if (table.id === 'products-datatable') {
            table.dataset.adminSearchReady = 'native';
            return;
        }

        const tbody = table.tBodies?.[0];
        if (!tbody) return;

        table.dataset.adminSearchReady = 'true';

        const wrap = table.closest('.admin-table-wrap');
        if (!wrap) return;

        const panel = table.closest('.admin-panel');
        const heading = panel?.querySelector('h2')?.textContent?.trim();
        const labelText = heading
            ? `جستجو در ${heading}`
            : 'جستجو در جدول';

        const toolbar = document.createElement('div');
        toolbar.className = 'admin-table-search';
        toolbar.dataset.adminTableSearch = '';

        const field = document.createElement('label');
        field.className = 'admin-table-search-field';

        const icon = document.createElement('span');
        icon.className = 'admin-table-search-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = '⌕';

        const input = document.createElement('input');
        input.type = 'search';
        input.autocomplete = 'off';
        input.spellcheck = false;
        input.placeholder = 'جستجو در تمام ستون‌ها...';
        input.setAttribute('aria-label', labelText);
        input.dataset.adminTableSearchInput = String(index);

        const clear = document.createElement('button');
        clear.type = 'button';
        clear.className = 'admin-table-search-clear';
        clear.textContent = '×';
        clear.setAttribute('aria-label', 'پاک کردن جستجو');
        clear.hidden = true;

        const status = document.createElement('span');
        status.className = 'admin-table-search-status';
        status.setAttribute('aria-live', 'polite');

        field.append(icon, input, clear);
        toolbar.append(field, status);
        wrap.parentNode?.insertBefore(toolbar, wrap);

        const apply = () => {
            const query = normalize(input.value);
            const rows = [...tbody.rows];
            let visible = 0;

            rows.forEach(row => {
                const matches = !query || normalize(row.textContent).includes(query);
                row.classList.toggle('admin-search-hidden', !matches);
                if (matches) visible++;
            });

            clear.hidden = !input.value;
            status.textContent = query
                ? `${visible.toLocaleString('fa-IR')} نتیجه از ${rows.length.toLocaleString('fa-IR')}`
                : `${rows.length.toLocaleString('fa-IR')} ردیف`;
        };

        input.addEventListener('input', apply);
        input.addEventListener('keydown', event => {
            if (event.key === 'Escape' && input.value) {
                input.value = '';
                apply();
            }
        });

        clear.addEventListener('click', () => {
            input.value = '';
            apply();
            input.focus();
        });

        apply();
    };

    const enhanceAll = () => {
        document.querySelectorAll('table.admin-table').forEach(enhanceTable);
    };

    enhanceAll();

    const observer = new MutationObserver(mutations => {
        if (!mutations.some(mutation => mutation.addedNodes.length > 0)) return;
        enhanceAll();
    });

    observer.observe(document.body, { childList: true, subtree: true });
})();
