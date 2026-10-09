// Admin pages: a form with data-confirm="question" asks before it's sent (delete, roll back, reset password).
// Lives in a file because the Content Security Policy doesn't allow inline event handlers (onsubmit="…").
// A <select data-preview="img-id"> whose options carry data-image shows the chosen option's picture in that <img>
// (hidden when the option has none), e.g. the diagram on the climate zone form.
document.addEventListener("change", event => {
    const select = event.target.closest("select[data-preview]");
    const preview = select && document.getElementById(select.dataset.preview);
    if (preview) {
        const image = select.selectedOptions[0]?.dataset.image;
        preview.hidden = !image;
        if (image) {
            preview.src = image;
        }
    }
});

document.addEventListener("submit", event => {
    const question = event.target.closest("form[data-confirm]")?.dataset.confirm;
    if (question && !confirm(question)) {
        event.preventDefault();
    }
});
