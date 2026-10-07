// Carbon map (plan Phase 4): grid carbon intensity by region, coloured in the old site's eight bands, with a legend
// and a label on hover. A click opens the carbon comparison for that region. Data comes from #carbon-map-data
// (CarbonController); the base map, locate and search come from map-base.js.
/* global L */
import { bands, colorFor, noDataColor } from "./carbon-bands.js";
import { createMap } from "./map-base.js";

const data = JSON.parse(document.getElementById("carbon-map-data").textContent);
const map = createMap("carbon-map", data.map);

const format = value => `${Math.round(value).toLocaleString()} g CO₂/kWh`;

// Legend (bottom right, above the credits).
const legend = L.control({ position: "bottomright" });
legend.onAdd = () => {
    const panel = L.DomUtil.create("div", "cs-map-panel cs-map-legend");
    const title = document.createElement("strong");
    title.textContent = "g CO₂/kWh";
    panel.append(title);
    for (const { color, label } of [...bands].reverse().concat({ color: noDataColor, label: "No data" })) {
        const row = document.createElement("div");
        const swatch = document.createElement("span");
        swatch.className = "cs-map-legend-swatch";
        swatch.style.backgroundColor = color;
        row.append(swatch, label);
        panel.append(row);
    }
    return panel;
};
legend.addTo(map);

// Phones: the same key as one strip under the map, cleanest first (CSS hides the one on the map).
const strip = document.getElementById("carbon-legend-strip");
const stripTitle = document.createElement("strong");
stripTitle.textContent = "g CO₂/kWh";
const stripRow = document.createElement("div");
stripRow.className = "cs-legend-strip-row";
for (const { color, label } of [...bands].reverse().concat({ color: noDataColor, label: "No data" })) {
    const item = document.createElement("div");
    const swatch = document.createElement("span");
    swatch.className = "cs-legend-strip-swatch";
    swatch.style.backgroundColor = color;
    item.append(swatch, label.replace(/–.*/, ""));   // "200–300" → "200": each colour starts at its number
    stripRow.append(item);
}
strip.append(stripTitle, stripRow);

// The region outlines (wwwroot/geo/carbon-regions.geojson), joined to data.regions by "code".
const baseStyle = { weight: 1, color: "#fff", opacity: 0.8, fillOpacity: 0.7 };
try {
    const outlines = await (await fetch(data.outlinesUrl)).json();
    L.geoJSON(outlines, {
        filter: feature => feature.properties.code in data.regions,
        style: feature => ({ ...baseStyle, fillColor: colorFor(data.regions[feature.properties.code].value) }),
        onEachFeature: (feature, layer) => {
            const region = data.regions[feature.properties.code];

            // Label that follows the mouse, like the home map: name, value, year and source (text only, never HTML).
            const label = document.createElement("span");
            const name = document.createElement("strong");
            name.textContent = region.name;
            label.append(name, document.createElement("br"),
                region.value === null ? "No data" : `${format(region.value)} (${region.year}, ${region.source})`);
            layer.bindTooltip(label, { sticky: true });

            layer.on({
                mouseover: () => {
                    layer.setStyle({ weight: 3, color: "#343a40" });
                    layer.bringToFront();
                },
                mouseout: () => layer.setStyle(baseStyle),
                click: () => { location.href = data.compareUrl + encodeURIComponent(feature.properties.code); },
            });
        },
    }).addTo(map);
} catch (error) {
    console.error("Carbon map: could not load the region outlines.", error);
}
