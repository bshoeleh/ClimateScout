// Grid carbon intensity colour bands (g CO2/kWh), shared by the carbon map and the comparison chart:
// the old site's eight bands, dirtiest first; a value goes in the first band it is above (0–200 is the cleanest).
export const bands = [
    { above: 800, color: "#800026", label: "800+" },
    { above: 700, color: "#BD0026", label: "700–800" },
    { above: 600, color: "#E31A1C", label: "600–700" },
    { above: 500, color: "#FC4E2A", label: "500–600" },
    { above: 400, color: "#FD8D3C", label: "400–500" },
    { above: 300, color: "#FEB24C", label: "300–400" },
    { above: 200, color: "#FED976", label: "200–300" },
    { above: -1, color: "#FFEDA0", label: "0–200" },
];
export const noDataColor = "#ADB5BD";
export const colorFor = value => value === null ? noDataColor : bands.find(band => value > band.above).color;
