// Carbon map (plan Phase 4): grid carbon intensity by region, coloured in the old site's eight bands, with a legend
// and a label on hover. A click opens the carbon comparison for that region. Data comes from #carbon-map-data
// (CarbonController); the base map, locate and search come from map-base.js.
/* global L */
import { createMap } from "./map-base.js";

const data = JSON.parse(document.getElementById("carbon-map-data").textContent);
const map = createMap("carbon-map", data.map);

// Bands in g CO2/kWh, dirtiest first; a value goes in the first band it is above (0–200 is the cleanest).
const bands = [
    { above: 800, color: "#800026", label: "800+" },
    { above: 700, color: "#BD0026", label: "700–800" },
    { above: 600, color: "#E31A1C", label: "600–700" },
    { above: 500, color: "#FC4E2A", label: "500–600" },
    { above: 400, color: "#FD8D3C", label: "400–500" },
    { above: 300, color: "#FEB24C", label: "300–400" },
    { above: 200, color: "#FED976", label: "200–300" },
    { above: -1, color: "#FFEDA0", label: "0–200" },
];
const noDataColor = "#ADB5BD";
const colorFor = value => value === null ? noDataColor : bands.find(band => value > band.above).color;
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
