// Admin pages: a form with data-confirm="question" asks before it's sent (delete, roll back, reset password).
// Lives in a file because the Content Security Policy doesn't allow inline event handlers (onsubmit="…").
document.addEventListener("submit", event => {
    const question = event.target.closest("form[data-confirm]")?.dataset.confirm;
    if (question && !confirm(question)) {
        event.preventDefault();
    }
});
