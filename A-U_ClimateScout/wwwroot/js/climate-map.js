// Home page climate map (plan Phase 4): Köppen zone polygons over the base map; a click opens the zone's page.
// The group buttons filter both the map and the zone list below it. Settings and the zone lookup come from
// the page (#climate-map-data, built by HomeController); tiles come through our own /map/tiles proxy.
/* global L, topojson */

const data = JSON.parse(document.getElementById("climate-map-data").textContent);

const map = L.map("climate-map", { minZoom: data.minZoom, maxZoom: data.maxZoom, worldCopyJump: true })
    .setView([20, 0], 2);
map.attributionControl.setPrefix('<a href="https://leafletjs.com">Leaflet</a>');   // Leaflet's default prefix adds a flag
L.tileLayer(data.tileUrl, {
    attribution: data.attribution,
    minZoom: data.minZoom,
    maxZoom: data.maxZoom,
    className: "cs-map-tiles",
}).addTo(map);
L.control.locate({ flyTo: true, strings: { title: "Show my location" } }).addTo(map);

// Address search (plan §5): a form in the map's top-right corner. It runs on Enter or the button, never while
// typing (Nominatim's usage policy). The map zooms to the first result and marks it.
const SearchControl = L.Control.extend({
    options: { position: "topright" },
    onAdd() {
        const form = L.DomUtil.create("form", "cs-map-search");
        form.setAttribute("role", "search");
        form.innerHTML = `
            <label class="visually-hidden" for="map-search">Search for a place</label>
            <input id="map-search" name="q" type="search" class="form-control form-control-sm" placeholder="Search for a place"
                   minlength="3" maxlength="200" required autocomplete="off" />
            <button type="submit" class="btn btn-sm btn-primary">Search</button>
            <p class="cs-map-search-status" aria-live="polite"></p>`;
        // Clicks and scrolling on the form must not reach the map (a click would open the zone underneath).
        L.DomEvent.disableClickPropagation(form);
        L.DomEvent.disableScrollPropagation(form);
        form.addEventListener("submit", event => {
            event.preventDefault();
            search(form);
        });
        return form;
    },
});
new SearchControl().addTo(map);

let searchMarker = null;

async function search(form) {
    const status = form.querySelector(".cs-map-search-status");
    status.textContent = "Searching…";
    try {
        const response = await fetch(`${data.geocodeUrl}?q=${encodeURIComponent(form.elements.q.value.trim())}`);
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
        map.fitBounds([[south, west], [north, east]], { maxZoom: data.maxZoom });

        const label = document.createElement("span");
        label.textContent = place.name;
        searchMarker?.remove();
        searchMarker = L.circleMarker([place.lat, place.lng], { radius: 7, color: "#000", weight: 2, fillColor: "#fff", fillOpacity: 1 })
            .bindTooltip(label)
            .addTo(map);
    } catch (error) {
        status.textContent = "Search is unavailable right now.";
        console.error("Climate map: address search failed.", error);
    }
}

// Group filter: "" shows every group. Works on the list straight away; the map follows once its polygons load.
const groupLayers = new Map();   // zone group slug → Leaflet layer group
let currentGroup = "";

function showGroup(slug) {
    currentGroup = slug;
    for (const button of document.querySelectorAll("[data-group-filter]")) {
        button.setAttribute("aria-pressed", String(button.dataset.groupFilter === slug));
    }
    for (const item of document.querySelectorAll("[data-group]")) {
        item.hidden = slug !== "" && item.dataset.group !== slug;
    }
    for (const [group, layer] of groupLayers) {
        if (slug === "" || group === slug) {
            layer.addTo(map);
        } else {
            layer.remove();
        }
    }
}

document.addEventListener("click", event => {
    const button = event.target.closest("[data-group-filter]");
    if (button) {
        showGroup(button.dataset.groupFilter);
    }
});

// The zone polygons: each one's "n" is looked up in data.zones (by MapId). A canvas draws the 5,499 polygons
// much faster than one SVG element each.
try {
    const topology = await (await fetch(data.polygonsUrl)).json();
    L.geoJSON(topojson.feature(topology, topology.objects.geojson), {
        renderer: L.canvas(),
        filter: feature => feature.properties.n in data.zones,
        style: feature => ({ fillColor: data.zones[feature.properties.n].color, fillOpacity: 0.75, stroke: false }),
        onEachFeature: (feature, layer) => {
            const zone = data.zones[feature.properties.n];
            const label = document.createElement("span");
            label.textContent = `${zone.koppenCode} ${zone.name}`;
            layer.bindTooltip(label, { sticky: true });
            layer.on("click", () => { location.href = data.zoneUrl + zone.slug; });

            if (!groupLayers.has(zone.group)) {
                groupLayers.set(zone.group, L.layerGroup());
            }
            groupLayers.get(zone.group).addLayer(layer);
        },
    });
    showGroup(currentGroup);
} catch (error) {
    console.error("Climate map: could not load the zone polygons.", error);
}
