document.addEventListener("DOMContentLoaded", function () {
    // We want to auto-save forms inside the main content area, ignoring things like logout forms
    const forms = document.querySelectorAll('main form');
    if (!forms.length) return;

    forms.forEach(form => {
        // Create a unique storage key based on the page URL
        const storageKey = 'autosave_' + window.location.pathname + window.location.search;

        // Restore saved data on load
        const savedData = localStorage.getItem(storageKey);
        if (savedData) {
            try {
                const data = JSON.parse(savedData);
                for (const [name, value] of Object.entries(data)) {
                    const elements = form.querySelectorAll(`[name="${name}"]`);
                    if (!elements.length) continue;

                    // Handle single elements or NodeList (like radio buttons)
                    elements.forEach(element => {
                        if (element.type === 'file' || element.type === 'hidden' || element.readOnly) return;
                        
                        if (element.type === 'checkbox' || element.type === 'radio') {
                            if (element.value === value) {
                                element.checked = true;
                            }
                        } else {
                            element.value = value;
                        }
                    });
                }
            } catch (e) {
                console.error("Autosave restore error", e);
            }
        }

        // Save data when user types or changes inputs
        form.addEventListener('input', function (e) {
            if (e.target.type === 'file' || e.target.type === 'hidden' || e.target.readOnly) return;

            let data = {};
            try {
                const existing = localStorage.getItem(storageKey);
                if (existing) data = JSON.parse(existing);
            } catch (e) { }

            if (e.target.type === 'checkbox') {
                if (e.target.checked) {
                    data[e.target.name] = e.target.value;
                } else {
                    delete data[e.target.name];
                }
            } else {
                data[e.target.name] = e.target.value;
            }

            localStorage.setItem(storageKey, JSON.stringify(data));
        });

        // Clear the saved data when they submit successfully (to move to next step)
        form.addEventListener('submit', function () {
            localStorage.removeItem(storageKey);
        });
    });
});
