// Admin rich-text editor (Quill 2) for HTML fields such as content blocks. A form marked data-rich-text-form holds
// a hidden <textarea data-rich-text-source> with the saved HTML and a <div data-rich-text-editor>; the editor loads
// the HTML and writes it back to the textarea when the form is submitted. The server cleans it again (HtmlCleaner).
/* global Quill */
(() => {
    for (const form of document.querySelectorAll("[data-rich-text-form]")) {
        const source = form.querySelector("[data-rich-text-source]");
        const container = form.querySelector("[data-rich-text-editor]");

        // Only what the public pages style: headings 2–3 (1 and 4 still load if a block already has them),
        // bold, italic, lists, links, and a button that clears formatting.
        const quill = new Quill(container, {
            theme: "snow",
            modules: {
                toolbar: [
                    [{ header: [2, 3, false] }],
                    ["bold", "italic"],
                    [{ list: "bullet" }, { list: "ordered" }],
                    ["link"],
                    ["clean"],
                ],
            },
        });
        quill.clipboard.dangerouslyPasteHTML(source.value, "silent");
        quill.history.clear();   // so Undo doesn't empty the editor

        // The toolbar buttons have no text of their own: name them for screen readers.
        const labels = {
            "ql-bold": "Bold", "ql-italic": "Italic", "ql-link": "Link", "ql-clean": "Clear formatting",
        };
        for (const button of container.previousElementSibling.querySelectorAll("button")) {
            const name = [...button.classList].find(c => labels[c]);
            if (name) {
                button.setAttribute("aria-label", labels[name]);
            } else if (button.classList.contains("ql-list")) {
                button.setAttribute("aria-label", button.value === "bullet" ? "Bulleted list" : "Numbered list");
            }
        }

        let dirty = false;
        quill.on("text-change", () => { dirty = true; });

        form.addEventListener("submit", () => {
            // getSemanticHTML gives real <ul>/<ol> lists; Quill 2.0.3 also turns every space into &nbsp;, so undo that.
            source.value = quill.getLength() > 1 ? quill.getSemanticHTML().replaceAll("&nbsp;", " ") : "";
            dirty = false;
        });

        // Warn before leaving with unsaved changes.
        addEventListener("beforeunload", event => {
            if (dirty) {
                event.preventDefault();
            }
        });
    }
})();
