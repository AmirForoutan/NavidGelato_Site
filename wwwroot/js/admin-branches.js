(() => {
    const tree = document.querySelector('[data-branch-catalog-tree]');
    const securityKey = document.querySelector('[data-security-key]');

    const updateCategoryState = node => {
        const category = node.querySelector('[data-category-check]');
        const products = [...node.querySelectorAll('[data-product-check]')];
        if (!category || products.length === 0) return;

        const checkedCount = products.filter(x => x.checked).length;
        category.indeterminate = category.checked && checkedCount > 0 && checkedCount < products.length;
    };

    tree?.querySelectorAll('[data-category-node]').forEach(node => {
        const category = node.querySelector('[data-category-check]');
        const products = [...node.querySelectorAll('[data-product-check]')];

        category?.addEventListener('change', () => {
            category.indeterminate = false;
            products.forEach(product => product.checked = category.checked);
        });

        products.forEach(product => {
            product.addEventListener('change', () => {
                if (product.checked && category) category.checked = true;
                updateCategoryState(node);
            });
        });

        node.querySelector('[data-category-collapse]')?.addEventListener('click', () => {
            node.classList.toggle('is-collapsed');
        });

        updateCategoryState(node);
    });

    document.querySelector('[data-catalog-select-all]')?.addEventListener('click', () => {
        tree?.querySelectorAll('input[type="checkbox"]').forEach(input => {
            input.checked = true;
            input.indeterminate = false;
        });
    });

    document.querySelector('[data-catalog-clear]')?.addEventListener('click', () => {
        tree?.querySelectorAll('input[type="checkbox"]').forEach(input => {
            input.checked = false;
            input.indeterminate = false;
        });
    });

    document.querySelector('[data-generate-security-key]')?.addEventListener('click', () => {
        if (!securityKey) return;

        const bytes = new Uint8Array(24);
        crypto.getRandomValues(bytes);
        securityKey.value = [...bytes].map(value => value.toString(16).padStart(2, '0')).join('');
        securityKey.type = 'text';
        securityKey.focus();
        securityKey.select();

        const toggle = document.querySelector('[data-toggle-security-key]');
        if (toggle) toggle.textContent = 'مخفی';
    });

    document.querySelector('[data-toggle-security-key]')?.addEventListener('click', event => {
        if (!securityKey) return;
        const show = securityKey.type === 'password';
        securityKey.type = show ? 'text' : 'password';
        event.currentTarget.textContent = show ? 'مخفی' : 'نمایش';
    });
})();
