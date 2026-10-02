// Zone page: show design strategies on the building diagram (plan Phase 4).
// Selecting a strategy shows its layer (<g id="ds-{slug}">) and disables the strategies that conflict with it.
// Conflicts are stored both ways, so a strategy is disabled while any selected strategy conflicts with it.
// The selection is kept in the address (#cool-roof,green-roof) — the old site's format, so shared links still work.

const cards = new Map([...document.querySelectorAll("[data-strategy]")].map(card => [card.dataset.strategy, card]));
const help = document.querySelector("[data-diagram-help]");
const selected = new Set();

const conflictsOf = slug => (cards.get(slug)?.dataset.conflicts ?? "").split(",").filter(Boolean);
const nameOf = slug => cards.get(slug)?.querySelector(".cs-strategy-name")?.textContent.trim() ?? slug;
const blockersOf = slug => [...selected].filter(other => conflictsOf(other).includes(slug));

function render() {
    for (const [slug, card] of cards) {
        const isSelected = selected.has(slug);
        const blockers = blockersOf(slug);
        document.getElementById(`ds-${slug}`)?.classList.toggle("is-visible", isSelected);
        card.classList.toggle("is-selected", isSelected);
        card.classList.toggle("is-disabled", blockers.length > 0);

        const button = card.querySelector(".cs-strategy-toggle");
        if (button) {
            button.setAttribute("aria-pressed", String(isSelected));
            button.disabled = blockers.length > 0;
        }

        const note = card.querySelector("[data-conflict-note]");
        note.hidden = blockers.length === 0;
        note.textContent = blockers.length > 0 ? `Conflicts with ${blockers.map(nameOf).join(", ")}` : "";
    }

    if (help) {
        help.hidden = selected.size > 0;
    }
    history.replaceState(null, "", selected.size > 0 ? `#${[...selected].join(",")}` : location.pathname + location.search);
}

document.addEventListener("click", event => {
    const button = event.target.closest(".cs-strategy-toggle");
    if (!button || button.disabled) {
        return;
    }

    const slug = button.closest("[data-strategy]").dataset.strategy;
    if (selected.has(slug)) {
        selected.delete(slug);
    } else {
        selected.add(slug);
    }
    render();
});

// Restore a shared selection: unknown slugs, strategies without a layer, and conflicting ones are skipped.
for (const slug of decodeURIComponent(location.hash.slice(1)).split(",")) {
    if (cards.get(slug)?.querySelector(".cs-strategy-toggle") && blockersOf(slug).length === 0) {
        selected.add(slug);
    }
}
render();
