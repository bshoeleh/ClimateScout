// Admin colour fields: a colour picker next to a #RRGGBB text box, kept in step both ways.
(() => {
    for (const picker of document.querySelectorAll("[data-color-picker]")) {
        const text = picker.closest(".input-group").querySelector("[data-color-text]");
        picker.addEventListener("input", () => { text.value = picker.value.toUpperCase(); });
        text.addEventListener("input", () => {
            if (/^#[0-9a-f]{6}$/i.test(text.value)) {
                picker.value = text.value.toLowerCase();
            }
        });
    }
})();
