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

    const list = result.querySelector('[data-result="equivalencies"]');
    list.replaceChildren(...response.equivalencies.map(equivalency => {
        const item = document.createElement("li");
        const amount = document.createElement("strong");
        amount.textContent = number(equivalency.amount);
        item.append("Equivalent to ", amount, ` ${equivalency.label}`);
        return item;
    }));
    const sourceUrl = response.equivalencies.find(equivalency => equivalency.sourceUrl)?.sourceUrl;
    if (sourceUrl) {
        const source = document.createElement("li");
        source.className = "small text-body-secondary";
        const link = document.createElement("a");
        link.href = sourceUrl;
        link.rel = "noopener";
        link.textContent = "Source: EPA greenhouse gas equivalencies";
        source.append(link);
        list.append(source);
    }

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

// Chart text and grid lines follow the page theme (light or dark).
const theme = getComputedStyle(document.documentElement);
Chart.defaults.color = theme.getPropertyValue("--bs-secondary-color").trim();
Chart.defaults.borderColor = theme.getPropertyValue("--bs-border-color-translucent").trim();
Chart.defaults.font.family = theme.getPropertyValue("--bs-body-font-family").trim();

const chartBox = document.querySelector(".cs-carbon-chart");
const chart = new Chart(document.getElementById("carbon-chart"), {
    type: "bar",
    data: { labels: [], datasets: [{ data: [], backgroundColor: [], borderColor: theme.getPropertyValue("--bs-emphasis-color").trim(), borderWidth: [] }] },
    options: {
        indexAxis: "y",
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: context => ` ${number(context.parsed.x)} g CO₂e/kWh` } },
        },
        scales: {
            x: { beginAtZero: true, title: { display: true, text: "g CO₂e/kWh" } },
            y: {
                ticks: {
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

const selected = regionsByCode.get(selectedCode);
showGroup((selected && groupOf(selected)) ?? "United States");
