// Shared setup for the site's maps (plan §5): the base map through our /map/tiles proxy, the credits, the locate
// button and the address search box. Each page passes the settings its controller put in the page (MapSettings).
/* global L */

export function createMap(elementId, settings) {
    const map = L.map(elementId, { minZoom: settings.minZoom, maxZoom: settings.maxZoom, worldCopyJump: true })
        .setView([20, 0], 2);
    map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');   // Leaflet's default prefix adds a flag
    L.tileLayer(settings.tileUrl, {
        attribution: settings.attribution,
        minZoom: settings.minZoom,
        maxZoom: settings.maxZoom,
        className: "cs-map-tiles",
    }).addTo(map);
    L.control.locate({ flyTo: true, strings: { title: "Show my location" } }).addTo(map);
    new SearchControl({ geocodeUrl: settings.geocodeUrl }).addTo(map);
    return map;
}

// Address search: a form in the map's top-right corner. It runs on Enter or the button, never while typing
// (Nominatim's usage policy). The map zooms to the first result and marks it.
const SearchControl = L.Control.extend({
    options: { position: "topright", geocodeUrl: "" },
    onAdd(map) {
        const form = L.DomUtil.create("form", "cs-map-search");
        form.setAttribute("role", "search");
        form.innerHTML = `
            <label class="visually-hidden" for="map-search">Search for a place</label>
            <input id="map-search" name="q" type="search" class="form-control form-control-sm" placeholder="Search for a place"
                   minlength="3" maxlength="200" required autocomplete="off" />
            <button type="submit" class="btn btn-sm btn-primary">Search</button>
            <p class="cs-map-search-status" aria-live="polite"></p>`;
        // Clicks and scrolling on the form must not reach the map (a click would act on the area underneath).
        L.DomEvent.disableClickPropagation(form);
        L.DomEvent.disableScrollPropagation(form);
        form.addEventListener("submit", event => {
            event.preventDefault();
            search(map, form, this.options.geocodeUrl);
        });
        return form;
    },
});

const searchMarkers = new WeakMap();   // map → its search marker

async function search(map, form, geocodeUrl) {
    const status = form.querySelector(".cs-map-search-status");
    status.textContent = "Searching…";
    try {
        const response = await fetch(`${geocodeUrl}?q=${encodeURIComponent(form.elements.q.value.trim())}`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        const [place] = await response.json();
        if (!place) {
            status.textContent = "No places found.";
            return;
        }

        status.textContent = "";
        const [south, west, north, east] = place.bounds;
        map.fitBounds([[south, west], [north, east]], { maxZoom: map.getMaxZoom() });

        const label = document.createElement("span");
        label.textContent = place.name;
        searchMarkers.get(map)?.remove();
        searchMarkers.set(map, L.circleMarker([place.lat, place.lng], { radius: 7, color: "#000", weight: 2, fillColor: "#fff", fillOpacity: 1 })
            .bindTooltip(label)
            .addTo(map));
    } catch (error) {
        status.textContent = "Search is unavailable right now.";
        console.error("Map: address search failed.", error);
    }
}
