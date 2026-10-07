// Carbon comparison page (plan Phase 4): the calculator, sent to POST /api/v1/carbon/calculate, and a bar chart
// comparing grid carbon intensity across a group of regions (US states, Canadian provinces, or a continent's
// countries). Data comes from #carbon-comparison-data (CarbonController).
/* global Chart */
import { colorFor } from "./carbon-bands.js";

const data = JSON.parse(document.getElementById("carbon-comparison-data").textContent);
const regionsByCode = new Map(data.regions.map(region => [region.code, region]));
const number = (value, digits = 0) =>
    Number(value).toLocaleString(undefined, { minimumFractionDigits: digits, maximumFractionDigits: digits });

// ---------- Calculator ----------
const form = document.getElementById("carbon-calculator");
const result = document.getElementById("carbon-result");
const field = name => form.elements[name];

// Choosing a location fills in its grid intensity (still editable) and shows it in the chart.
field("regionCode").addEventListener("change", () => {
    const region = regionsByCode.get(field("regionCode").value);
    selectedCode = region?.code ?? null;
    if (region) {
        field("gridIntensity").value = region.value;
    }
    showGroup((region && groupOf(region)) ?? currentGroup);
});

// As on the old site, the EUI unit implies the area unit: kBtu/ft² goes with ft², the metric ones with m².
field("euiUnit").addEventListener("change", () => {
    field("areaUnit").value = field("euiUnit").value === "KbtuPerSquareFoot" ? "SquareFeet" : "SquareMetres";
});

form.addEventListener("submit", async event => {
    event.preventDefault();
    clearErrors();

    const region = regionsByCode.get(field("regionCode").value);
    const grid = field("gridIntensity").value.trim();
    const errors = {};
    if (!(Number(field("eui").value) > 0)) errors.eui = ["Enter an EUI above 0."];
    if (!(Number(field("area").value) > 0)) errors.area = ["Enter a floor area above 0."];
    if (!region && grid === "") errors.gridIntensity = ["Choose a location or enter a grid carbon intensity."];
    if (Object.keys(errors).length > 0) {
        showErrors(errors);
        return;
    }

    // The region's own value is sent as the region; only a changed number is sent as an entered value.
    const request = {
        eui: Number(field("eui").value),
        euiUnit: field("euiUnit").value,
        area: Number(field("area").value),
        areaUnit: field("areaUnit").value,
        regionCode: region?.code ?? null,
        gridIntensity: grid === "" || (region && Number(grid) === region.value) ? null : Number(grid),
    };
    try {
        const response = await fetch(data.calculateUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request),
        });
        if (response.status === 400) {
            showErrors((await response.json()).errors ?? {});
            return;
        }
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        showResult(await response.json());
    } catch (error) {
        showErrors({ form: ["The calculator is unavailable right now. Please try again later."] });
        console.error("Carbon calculator failed.", error);
    }
});

// Errors come from our own checks or the API (keys like "Eui" or "$.eui"); each goes next to its field.
function showErrors(errors) {
    for (const [key, messages] of Object.entries(errors)) {
        const name = key.replace(/^\$\./, "").toLowerCase();
        const slot = [...form.querySelectorAll("[data-error-for]")]
            .find(element => element.dataset.errorFor.toLowerCase().split(" ").includes(name));
        if (slot) {
            slot.textContent = messages.join(" ");
            slot.parentElement.querySelector("input").classList.add("is-invalid");
        } else {
            const general = form.querySelector("[data-form-error]");
            general.textContent = messages.join(" ");
            general.hidden = false;
        }
    }
}

function clearErrors() {
    form.querySelectorAll(".is-invalid").forEach(element => element.classList.remove("is-invalid"));
    form.querySelector("[data-form-error]").hidden = true;
}

// Infographic: the total as CO₂ clouds, then each EPA equivalency as a row of icons, one card per line so the
// results are easy to compare. Each icon stands for a round amount (1, 2, 5, 10, 20, 50 …) chosen so a card never
// needs more than 30 icons; the last icon is filled in part for the remainder.
const maxIcons = 30;
const svgNs = "http://www.w3.org/2000/svg";

function unitFor(amount) {
    for (let power = 1; ; power *= 10) {
        for (const step of [1, 2, 5]) {
            if (amount / (step * power) <= maxIcons) return step * power;
        }
    }
}

function icon(name, className) {
    const svg = document.createElementNS(svgNs, "svg");
    svg.setAttribute("aria-hidden", "true");
    if (className) svg.setAttribute("class", className);
    const use = document.createElementNS(svgNs, "use");
    use.setAttribute("href", `#cs-icon-${name}`);
    svg.append(use);
    return svg;
}

const amountText = amount =>
    amount >= 1e6 ? `${number(amount / 1e6, 1)} million` : number(amount, amount < 100 ? 1 : 0);

function equivalencyCard({ key, iconName, amount, label, unitSingular, unitPlural }) {
    const unit = unitFor(amount);
    const card = document.createElement("div");
    card.className = `cs-eq-card cs-eq-${key}`;

    const head = document.createElement("div");
    head.className = "cs-eq-head";
    const badge = document.createElement("span");
    badge.className = "cs-eq-badge";
    badge.append(icon(iconName));
    const text = document.createElement("div");
    const big = document.createElement("div");
    big.className = "cs-eq-amount";
    big.textContent = amountText(amount);
    const caption = document.createElement("div");
    caption.className = "cs-eq-label";
    caption.textContent = label;
    text.append(big, caption);
    head.append(badge, text);

    // The row of icons; screen readers get the number and label instead.
    const row = document.createElement("div");
    row.className = "cs-eq-icons";
    row.setAttribute("aria-hidden", "true");
    const count = amount / unit;
    for (let i = 0; i < Math.ceil(count); i++) {
        const cell = document.createElement("span");
        cell.className = "cs-eq-icon";
        cell.style.setProperty("--i", i);
        const filled = document.createElement("span");
        filled.className = "cs-eq-fill";
        filled.style.width = `${Math.min(1, count - i) * 100}%`;
        filled.append(icon(iconName));
        cell.append(icon(iconName, "cs-eq-empty"), filled);
        row.append(cell);
    }

    const scale = document.createElement("div");
    scale.className = "cs-eq-scale";
    scale.append(icon(iconName), ` = ${number(unit)} ${unit === 1 ? unitSingular : unitPlural}`);

    card.append(head, row, scale);
    return card;
}

function renderInfographic(container, response) {
    const heading = text => {
        const h = document.createElement("h3");
        h.className = "h6 text-uppercase mt-4 mb-2";
        h.textContent = text;
        return h;
    };
    const cards = list => list.map(e => equivalencyCard({
        key: e.key, iconName: e.icon ?? "cloud-fill", amount: e.amount, label: e.label,
        unitSingular: e.unitSingular, unitPlural: e.unitPlural,
    }));
    const emissions = response.equivalencies.filter(e => e.kind === "Emissions");
    const absorption = response.equivalencies.filter(e => e.kind === "Absorption");

    container.replaceChildren(
        equivalencyCard({ key: "co2", iconName: "cloud-fill", amount: response.totalTonnes,
            label: "metric tons of CO₂e emitted each year", unitSingular: "metric ton", unitPlural: "metric tons" }),
        ...(emissions.length ? [heading("The same as"), ...cards(emissions)] : []),
        ...(absorption.length ? [heading("To absorb it you would need"), ...cards(absorption)] : []));

    const sourceUrl = response.equivalencies.find(e => e.sourceUrl)?.sourceUrl;
    if (sourceUrl) {
        const source = document.createElement("a");
        source.className = "d-inline-block small mt-2";
        source.href = sourceUrl;
        source.rel = "noopener";
        source.textContent = "Source: EPA greenhouse gas equivalencies";
        container.append(source);
    }
}

function showResult(response) {
    const set = (name, text) => { result.querySelector(`[data-result="${name}"]`).textContent = text; };
    const grid = response.gridIntensity;
    set("grid", grid.entered
        ? `Using your grid carbon intensity of ${number(grid.value)} g CO₂e/kWh.`
        : `Using ${grid.region.name}'s grid carbon intensity of ${number(grid.value)} g CO₂e/kWh (${grid.year}, ${grid.source}).`);
    set("kgPerSquareMetre", number(response.kgPerSquareMetre, 2));
    set("lbPerSquareFoot", number(response.lbPerSquareFoot, 2));
    set("totalTonnes", number(response.totalTonnes, 2));
    set("totalKg", number(response.totalKg));
    set("totalLb", number(response.totalLb));

    renderInfographic(result.querySelector('[data-result="infographic"]'), response);

    form.hidden = true;
    result.hidden = false;
    result.scrollIntoView({ block: "nearest" });
}

result.querySelector("[data-calculate-again]").addEventListener("click", () => {
    result.hidden = true;
    form.hidden = false;
    field("eui").focus();
});

// ---------- Comparison chart ----------
// Each region belongs to one group of buttons: US states, Canadian provinces, or its continent (countries).
// World and regional averages belong to none, but show up when chosen as the location.
const groupOf = region =>
    region.type === "State" ? "United States" :
    region.type === "Province" ? "Canada" :
    region.type === "Country" ? region.continent : null;
const groupNames = ["United States", "Canada",
    ...[...new Set(data.regions.filter(r => r.type === "Country").map(groupOf))].filter(Boolean).sort()];

let selectedCode = data.selected;
let currentGroup = null;

const groupButtons = document.querySelector("[data-chart-groups]");
for (const name of groupNames) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn-sm btn-outline-secondary";
    button.dataset.group = name;
    button.textContent = name;
    button.setAttribute("aria-pressed", "false");
    button.addEventListener("click", () => showGroup(name));
    groupButtons.append(button);
}

// Chart text and grid lines follow the page theme (light or dark), read from Bootstrap's colours each time the
// chart draws (functions, so a theme change only needs a redraw).
const theme = getComputedStyle(document.documentElement);
const themeColor = name => theme.getPropertyValue(name).trim();
const textColor = () => themeColor("--bs-secondary-color");
const gridColor = () => themeColor("--bs-border-color-translucent");
Chart.defaults.font.family = themeColor("--bs-body-font-family");

const chartBox = document.querySelector(".cs-carbon-chart");
const chart = new Chart(document.getElementById("carbon-chart"), {
    type: "bar",
    data: { labels: [], datasets: [{ data: [], backgroundColor: [], borderColor: themeColor("--bs-emphasis-color"), borderWidth: [] }] },
    options: {
        indexAxis: "y",
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: context => ` ${number(context.parsed.x)} g CO₂e/kWh` } },
        },
        scales: {
            x: {
                beginAtZero: true,
                title: { display: true, text: "g CO₂e/kWh", color: textColor },
                ticks: { color: textColor },
                grid: { color: gridColor },
                border: { color: gridColor },
            },
            y: {
                grid: { color: gridColor },
                border: { color: gridColor },
                ticks: {
                    color: textColor,
                    autoSkip: false,
                    // The chosen location's name in bold (context.chart, since this runs while the chart is created).
                    font: context => ({
                        weight: context.chart.data.labels[context.index] === regionsByCode.get(selectedCode)?.name ? "bold" : "normal",
                    }),
                },
            },
        },
    },
});

// One bar per region in the group (plus the chosen location), dirtiest first; the chosen location is outlined.
function showGroup(name) {
    currentGroup = name;
    for (const button of groupButtons.querySelectorAll("button")) {
        button.setAttribute("aria-pressed", String(button.dataset.group === name));
    }
    const rows = data.regions
        .filter(region => groupOf(region) === name || region.code === selectedCode)
        .sort((a, b) => b.value - a.value);
    const dataset = chart.data.datasets[0];
    chart.data.labels = rows.map(region => region.name);
    dataset.data = rows.map(region => region.value);
    dataset.backgroundColor = rows.map(region => colorFor(region.value));
    dataset.borderWidth = rows.map(region => region.code === selectedCode ? 2 : 0);
    chartBox.style.height = `${rows.length * 20 + 60}px`;
    chart.update();
}

// Redraw in the new colours when the theme changes: the theme menu, the computer's setting, or printing
// (theme.js prints in the light theme).
new MutationObserver(() => {
    chart.data.datasets[0].borderColor = themeColor("--bs-emphasis-color");
    chart.update("none");
}).observe(document.documentElement, { attributes: true, attributeFilter: ["data-bs-theme"] });

const selected = regionsByCode.get(selectedCode);
showGroup((selected && groupOf(selected)) ?? "United States");
