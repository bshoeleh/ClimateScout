// Home page climate map (plan Phase 4): Köppen zone polygons over the base map; a click opens the zone's page.
// The group buttons filter both the map and the zone list below it. Settings and the zone lookup come from
// the page (#climate-map-data, built by HomeController); the base map, locate and search come from map-base.js.
/* global L, topojson */
import { createMap } from "./map-base.js";

const data = JSON.parse(document.getElementById("climate-map-data").textContent);
const map = createMap("climate-map", data.map);

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
