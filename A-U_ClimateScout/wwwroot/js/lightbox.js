// Lightbox for reference project photos (strategy pages): a click on a [data-lightbox] button
// shows the full photo in the page's <dialog data-lightbox-dialog>. The browser's dialog
// handles Esc, focus and the backdrop; a click outside the photo also closes it.

const dialog = document.querySelector("[data-lightbox-dialog]");
const image = dialog?.querySelector("img");
const caption = dialog?.querySelector(".cs-lightbox-caption");

document.addEventListener("click", event => {
    const trigger = event.target.closest("[data-lightbox]");
    if (!trigger || !dialog) {
        return;
    }

    image.src = trigger.dataset.lightbox;
    image.alt = trigger.dataset.caption ?? "";
    caption.textContent = trigger.dataset.caption ?? "";
    dialog.showModal();
});

// A click on the backdrop lands on the <dialog> itself (not its contents), so close it.
dialog?.addEventListener("click", event => {
    if (event.target === dialog) {
        dialog.close();
    }
});

// Clear the photo once closed, so the next one doesn't briefly show the previous photo.
dialog?.addEventListener("close", () => image.removeAttribute("src"));
