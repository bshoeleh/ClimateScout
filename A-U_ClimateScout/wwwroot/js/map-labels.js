// Country names drawn over the map colours (the base map's own names are few at the first zoom levels, and the
// coloured regions hide them). Positions come from the country outlines in carbon-regions.geojson: the middle of
// each country's largest piece of land. Bigger countries show from further out; smaller ones as the map zooms in.
// Labels ignore the mouse, so hovering and clicking still reach the region underneath.
/* global L */

// The United States and Canada are drawn as states and provinces, so their names get fixed points.
const extraLabels = [
    { name: "United States", lat: 39.5, lng: -98.5, area: Infinity },
    { name: "Canada", lat: 58, lng: -101, area: Infinity },
];

// Shorter names where the full one would cover its neighbours.
const shortNames = {
    CD: "DR Congo",
    CF: "Central African Rep.",
};

// Smallest outline area (in square degrees) shown at each zoom level; anything smaller waits for the last level.
const areaByZoom = [[2, 140], [3, 40], [4, 8]];
const minZoomFor = area => areaByZoom.find(([, minArea]) => area >= minArea)?.[0] ?? 5;

export async function addCountryLabels(map, outlinesUrl) {
    map.createPane("cs-labels").style.zIndex = 620;   // above the coloured regions (400), below tooltips (650)

    const outlines = await (await fetch(outlinesUrl)).json();
    const countries = outlines.features
        .filter(feature => !feature.properties.code.includes("-"))   // states, provinces and averages have a "-"
        .map(feature => ({ name: shortNames[feature.properties.code] ?? feature.properties.name, ...labelPoint(feature.geometry) }))
        .concat(extraLabels);

    const labels = countries.map(country => ({
        minZoom: minZoomFor(country.area),
        marker: L.marker([country.lat, country.lng], {
            pane: "cs-labels",
            interactive: false,
            keyboard: false,
            icon: L.divIcon({ className: "cs-map-label", html: `<span>${escapeHtml(country.name)}</span>`, iconSize: null }),
        }),
    }));

    const update = () => {
        const zoom = map.getZoom();
        for (const { minZoom, marker } of labels) {
            if (zoom >= minZoom) {
                marker.addTo(map);
            } else {
                marker.remove();
            }
        }
    };
    map.on("zoomend", update);
    update();
}

// The centre (area-weighted) of the largest ring in the outline, and the outline's total area.
function labelPoint(geometry) {
    const polygons = geometry.type === "Polygon" ? [geometry.coordinates] : geometry.coordinates;
    let best = null;
    let total = 0;
    for (const [ring] of polygons) {
        let area = 0, x = 0, y = 0;
        for (let i = 0; i < ring.length - 1; i++) {
            const [x0, y0] = ring[i];
            const [x1, y1] = ring[i + 1];
            const cross = x0 * y1 - x1 * y0;
            area += cross;
            x += (x0 + x1) * cross;
            y += (y0 + y1) * cross;
        }
        area /= 2;
        total += Math.abs(area);
        if (area !== 0 && (!best || Math.abs(area) > best.area)) {
            best = { area: Math.abs(area), lng: x / (6 * area), lat: y / (6 * area) };
        }
    }
    return { lat: best.lat, lng: best.lng, area: total };
}

function escapeHtml(text) {
    const span = document.createElement("span");
    span.textContent = text;
    return span.innerHTML;
}
