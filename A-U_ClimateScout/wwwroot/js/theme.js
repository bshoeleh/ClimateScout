// Light / Dark / Auto theme switch. The initial theme is applied by the inline script in _Layout's <head>;
// this file handles the navbar menu and follows OS changes while "Auto" is selected.
(() => {
    const storageKey = "cs-theme";
    const darkQuery = matchMedia("(prefers-color-scheme: dark)");

    const getStored = () => {
        try { return localStorage.getItem(storageKey); } catch { return null; }
    };

    const setStored = (value) => {
        try {
            if (value === "auto") localStorage.removeItem(storageKey);
            else localStorage.setItem(storageKey, value);
        } catch { }
    };

    const getPreference = () => {
        const stored = getStored();
        return stored === "light" || stored === "dark" ? stored : "auto";
    };

    const apply = (preference) => {
        const theme = preference === "auto" ? (darkQuery.matches ? "dark" : "light") : preference;
        document.documentElement.setAttribute("data-bs-theme", theme);

        const name = preference[0].toUpperCase() + preference.slice(1);
        const label = document.getElementById("theme-label");
        if (label) label.textContent = `Theme: ${name}`;

        const icon = document.getElementById("theme-icon");
        if (icon) icon.setAttribute("href", `#cs-icon-${preference}`);

        document.querySelectorAll("[data-cs-theme-value]").forEach((item) => {
            const active = item.dataset.csThemeValue === preference;
            item.classList.toggle("active", active);
            item.setAttribute("aria-pressed", active);
        });
    };

    darkQuery.addEventListener("change", () => {
        if (getPreference() === "auto") apply("auto");
    });

    document.addEventListener("DOMContentLoaded", () => {
        apply(getPreference());
        document.querySelectorAll("[data-cs-theme-value]").forEach((item) => {
            item.addEventListener("click", () => {
                const preference = item.dataset.csThemeValue;
                setStored(preference);
                apply(preference);
            });
        });
    });
})();
