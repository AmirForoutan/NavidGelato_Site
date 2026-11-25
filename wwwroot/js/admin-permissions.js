(() => {
    const groups = [...document.querySelectorAll('[data-permission-group]')];
    if (!groups.length) return;

    const syncGroup = group => {
        const master = group.querySelector('[data-permission-master]');
        const items = [...group.querySelectorAll('input[name="GrantedPermissions"]')];
        const count = group.querySelector('[data-permission-count]');
        if (!master || !items.length) return;

        const checked = items.filter(item => item.checked).length;
        master.checked = checked === items.length;
        master.indeterminate = checked > 0 && checked < items.length;
        if (count) count.textContent = `${checked.toLocaleString('fa-IR')} از ${items.length.toLocaleString('fa-IR')}`;
        group.classList.toggle('has-selection', checked > 0);
    };

    groups.forEach(group => {
        const master = group.querySelector('[data-permission-master]');
        const viewPermission = group.querySelector('[data-permission-view="true"]');
        const items = [...group.querySelectorAll('input[name="GrantedPermissions"]')];

        master?.addEventListener('click', event => event.stopPropagation());
        master?.addEventListener('change', () => {
            items.forEach(item => { item.checked = master.checked; });
            syncGroup(group);
        });

        items.forEach(item => item.addEventListener('change', () => {
            if (item.checked && viewPermission && item !== viewPermission)
                viewPermission.checked = true;

            if (item === viewPermission && !item.checked && items.some(other => other !== viewPermission && other.checked))
                viewPermission.checked = true;

            syncGroup(group);
        }));

        syncGroup(group);
    });
})();
