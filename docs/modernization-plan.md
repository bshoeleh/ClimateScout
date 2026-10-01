# ClimateScout Modernization Plan

Status: **IN PROGRESS — Phases 1–2 complete; Phase 3 (data command importers) started — media done, next `import wordpress`**
Last updated: 2026-09-30

## ▶ Where we left off (2026-09-30, end of day)

**Last commit:** `873bf4c Add old site images and media map for the WordPress import`. Working tree clean.

**Done so far:** Phase 1 complete (1.1–1.7). The site has the brand palette, Fira Sans, light/dark themes, the Arcadis ClimateScout logo, the public menu, a footer placeholder, a locked Admin area at `/admin`, registration disabled, and styled 404/error pages. The follow-ups from 1.7 are listed under that step in §11. Phase 2: domain model and initial migration (`afb54b5`); `tool init reference` and `tool init admin` with a forced password change on first sign-in (`3fce944`), verified end to end. Unit tests: conflict rules (`6588e41`) and carbon calculator (`2967d66`), 11 passing — run `dotnet test` from the repo root. Phase 3: import decisions recorded in §10 (hyphen slugs, description → Location, media approach); all 149 old images downloaded, cleaned and sorted into `wwwroot/img/media/{projects,strategies,other}/` with `Data/Import/media-map.csv` as the log (`873bf4c`).

**Next up — Phase 3, `import wordpress`**, built in small steps, each proposed and approved:
1. Command skeleton `tool import wordpress --sqlite <path> [--dry-run]` + `MediaAsset` rows from `media-map.csv` (simplest part first). Decide how the CSV ships with the published app (one `.csproj` setting, e.g. copy to output/publish) so the import can run on the server.
2. Zones (split `Cfa_Humid Subtropical` into code + name; hyphen slugs), linked to the existing zone groups and diagrams.
3. Strategies, zone↔strategy links, conflicts (PHP-serialized → `StrategyConflictRules.MakeSymmetric`), reference projects (description → `Location`), images via the CSV.
4. Content pages (About, Grid Carbon Intensity, Carbon Comparison) → `ContentBlocks`; then the data clean-up line (mojibake, known errors, CRTKL references).
- The old database is `climatescout-2016-06-11.sqlite` inside `xfer/old Site/climatescout-2016-06-11.zip` (unzip to a temp folder; don't commit it).
- Small pure parsing helpers (e.g. PHP-serialized conflicts, code/name split) are candidates for the user to write tests for.
- Note: there is no scoped CSS (`_Layout.cshtml.css` was removed along with its `<link>`). If a view needs scoped CSS later, re-add `<link rel="stylesheet" href="~/A-U_ClimateScout.styles.css" />`. The bundle name uses the assembly name, with a hyphen.

**Working agreement:** walkthrough style — propose each file/command with explanation, wait for OK, then do it, verify, and show the result. Commit at checkpoints with a descriptive message (no AI attribution). User is new to unit testing — explain tests in detail. Tests: the user decides the test cases (normal / nothing / repeats / invalid / boundaries / guarantees) and writes the tests; the reviewer explains and reviews, and doesn't add tests unprompted.

---

## 1. Goals

Rebuild ClimateScout (currently WordPress + ACF + custom theme) as an ASP.NET Core MVC application on SQL Server that:

- Preserves every public feature of the current site (climate map, zone pages, interactive strategy diagrams, strategy pages, carbon map, carbon calculator/comparison).
- Makes **all** content editable from a locked-down admin area.
- Imports new grid-carbon-intensity data (CSV) frequently and safely, with preview and history.
- Exposes a versioned JSON API.
- Sends email (contact, admin notifications, account emails).
- Adds a Sponsors page, plus a sponsor logo strip in the site footer.
- Removes all CallisonRTKL / CRTKL branding; branded as Arcadis ClimateScout®.
- Looks modern and clean — Bootstrap 5, vanilla JS for our code (jQuery stays installed but isn't used by new code), Chart.js, Arcadis-referenced palette.

## 2. Findings from the old platform (summary)

| Area | Old implementation | Notes for rebuild |
|---|---|---|
| Climate zones | WP taxonomy `climate-zone`, 5 groups + 31 Köppen sub-zones; ACF term fields `body`, `climate-code`, `heading-color`, `map_id`, `diagram` | `map_id` joins to property `n` in `koppen-2018-2.json` TopoJSON (5,499 polygons) |
| Design strategies | CPT `climate-strategy` (27); ACF `2030_url`, `conflicts` (PHP-serialized post IDs), `reference_projects` repeater (name/image/description/url/sector) | Conflicts are **asymmetric** in data; one 2030 URL wrong (east-west-shading → earth-sheltering) |
| Zone page diagram | 4 inline SVGs (hot-humid, hot-dry, temperate, cold), layers `id="ds-{slug}"`; toggling, conflict disabling (ref-counted), URL-hash state, print-only-selected | Keep behavior; SVGs become uploadable admin assets |
| Carbon map | Leaflet choropleth from a 1–1.7 MB JS file mixing geometry + values (267 features) | Split: geometry = static GeoJSON; values = SQL |
| Calculator | EUI × grid intensity × area; unit conversions; EPA equivalencies (gasoline, seedlings) | Port to a server-side service + API; fix dead unit-branch bug |
| Maps | Leaflet 1.9 + Mapbox tiles/geocoding, **token hard-coded**, style owned by personal account | Keep Leaflet; move Mapbox to an Arcadis account behind a server proxy (see §5) |
| Content | About page, calculator intro/result text, carbon sources (ACF) | Becomes editable content blocks |
| Media | 149 attachments; still downloadable from `climatescout.arcadis.com/wp-content/uploads/…` | Download once into our storage |
| Data quirks | `�` mojibake for degree signs; unused `product` CPT; duplicate/dead templates | Clean during migration |

### New carbon data (`xfer/carbondata`)

| File | Shape | Issues the importer must handle |
|---|---|---|
| `CEI_all_countries_from Ember.csv` | `Area,Year,Continent,Variable,Unit,Value` (228 rows) | Aggregates with blank continent (World, EU, G7, OECD, ASEAN…); **mixed years per row** (2009–2025); quoted names containing commas; names differ from map (`Viet Nam` vs `Vietnam`, `Congo (the)`, `Bahamas (the)`, …) |
| `CEI_USA_from Ember.csv` | `Country,State,Year,…` (53 rows) | Source changed EPA → Ember; includes `US Total`; `"Washington, DC"` vs map `District of Columbia` |
| `CEI_Canada_from Canada Energy Regulator.csv` | `Country,Province,Year,…` (12 rows) | Yukon missing (map has it) |

→ Regions must be keyed by **stable codes** (ISO 3166-1 alpha-3 for countries, ISO 3166-2 for states/provinces) with an **alias table** for name matching. Unmatched rows go to a review screen, never silently dropped.

### Existing starter project (`A-U_ClimateScout`)

.NET 10 MVC template + Identity + EF SQL Server, plus Copilot-generated models and a 1,071-line seeder. Treat as **scaffolding only**: models lack reference projects, zone↔strategy relation, proper conflicts, carbon, sponsors, content; image URLs point at the WP site that will be retired; jQuery/Bootstrap libs from the template. Plan: restructure and rebuild the domain; reuse seed text only after verifying it against the SQLite source.

## 3. Target architecture

### 3.1 Solution layout (flat — decided 2026-09-28)

One web project plus one test project. Separation of concerns is kept by **folder convention**, not by separate projects.

```
A-U_ClimateScout.slnx
A-U_ClimateScout/                   ASP.NET Core MVC web project (public site, Admin area, API, data commands)
  Areas/
    Admin/
      Controllers/                  lean: validate → call service → return view
      Views/
      ViewModels/
  Controllers/                      public MVC controllers
  Controllers/Api/V1/               API controllers ([ApiController], versioned)
  Data/
    ApplicationDbContext.cs
    Configurations/                 IEntityTypeConfiguration<T> per entity
    Migrations/
  Identity/                         ApplicationUser
  Models/                           entities (ClimateZone, DesignStrategy, Sponsor, CarbonIntensity …)
  Services/                         all business logic: ClimateZoneService, CarbonCalculator, CarbonImportService,
                                    SponsorService, ContentService, EmailService, FileStorage, ApiKeyService …
  Importing/                        CSV parsers per source profile, WordPress import (used by admin + data commands)
  Tools/                            data commands (see §3.3)
  ViewModels/
  Views/
    Shared/Components/              view components (nav, footer, sponsor strip)
  wwwroot/
    css/  (site.css — our styles on top of bootstrap.min.css; no SCSS build)
    js/   (ES modules: map.js, zone-diagram.js, calculator.js, carbon-chart.js, admin/*.js)
    geo/  (koppen.json, carbon-regions.geojson)
    lib/  (bootstrap 5, jquery (kept), chart.js, leaflet, topojson-client, leaflet.locatecontrol, quill — via LibMan)
  Program.cs                        startup; service registration grouped in extension methods
tests/
  A-U_ClimateScout.Tests/           xUnit: unit tests (calculator, conflict rules, import matching)
                                    + integration tests (in-memory app: API contracts, admin lockout)
docs/
```

Namespaces follow folders: `A_U_ClimateScout.Services`, `A_U_ClimateScout.Data`, `A_U_ClimateScout.Identity` …

Rules: controllers never touch `DbContext` — they call `Services/`; services return view models/DTOs/results; validation via DataAnnotations (FluentValidation if needed); a `Result<T>` pattern for expected failures.

### 3.3 Data initialization (no seeding)

The database schema comes from EF migrations only. **No `HasData` seeding and no startup seeder.** Data is loaded by **data commands built into the web app**: starting the app with the `tool` argument runs a command and exits instead of starting the website:

```
dotnet A-U_ClimateScout.dll tool <command> [options]      (deployed, on the IIS server)
dotnet run --project A-U_ClimateScout -- tool <command>   (development)
```

Commands are idempotent and can be run at any point, against any environment (same connection string as the site):

| Command | Does |
|---|---|
| `db migrate` | apply pending migrations |
| `init reference` | create/update lookup data (roles, zone groups, diagrams, equivalency factors, carbon sources) |
| `init admin --email …` | create/reset the first Admin user |
| `import wordpress --sqlite … [--dry-run]` | import zones, strategies, conflicts, reference projects, content from the old WP database; creates `MediaAsset` rows from `Data/Import/media-map.csv` |
| `import carbon --file … --profile …` | same import pipeline the Admin screen uses |
| `import geo …` | load carbon-region geometry/aliases |

Every command reports created / updated / skipped counts and supports `--dry-run`. The admin screens call the same services, so there's one code path.

### 3.2 Data model (first cut)

- **ClimateZoneGroup** — Id, Code (A–E), Name, Slug, Color, SortOrder
- **ClimateZone** — Id, GroupId, KoppenCode (`Cfa`), Name, Slug, DescriptionHtml, Color, MapId, DiagramId, SortOrder, IsActive
- **Diagram** — Id, Name (Hot-Humid …), Slug, SvgAssetId (uploaded SVG containing `ds-{slug}` layers)
- **DesignStrategy** — Id, Name, Slug, SummaryHtml, BodyHtml, IconAssetId, ImageAssetId, Palette2030Url, SortOrder, IsActive
- **ClimateZoneStrategy** — ZoneId, StrategyId (many-to-many, optional SortOrder)
- **StrategyConflict** — StrategyId, ConflictsWithStrategyId (store both directions if symmetric — see Q)
- **ReferenceProject** — Id, StrategyId, Name, Sector, DescriptionHtml, Url, ImageAssetId, SortOrder
- **CarbonRegion** — Id, Code (ISO), Name, RegionType (Country/State/Province/Aggregate), ParentRegionId, Continent, ShowOnMap
- **CarbonRegionAlias** — RegionId, Alias (unique), Source
- **CarbonDataSource** — Id, Name (Ember, CER, EPA), Url, Notes
- **CarbonIntensity** — Id, RegionId, SourceId, Year, ValueGPerKWh, ImportBatchId; unique (Region, Source, Year)
- **CarbonImportBatch** — Id, FileName, SourceId, Profile, UploadedBy, UploadedAt, Status (Preview/Committed/RolledBack), row counts, ErrorsJson
- **Sponsor** — Id, Name, Tier, LogoAssetId, Url, DescriptionHtml, StartDate, EndDate, SortOrder, IsActive, ShowInFooter
  - "Visible" = IsActive and today within Start/End dates (open-ended if null). Footer shows visible sponsors with ShowInFooter, ordered by Tier then SortOrder; cached and invalidated on save.
- **ContentBlock** — Key (`about.body`, `carbon.calculator.intro`, `footer.text` …), Title, Html, UpdatedBy/At
- **MediaAsset** — Id, FileName, ContentType, Path, Alt, Width, Height, UploadedAt
- **EquivalencyFactor** — Key, Label, TonsCo2ePerUnit, SourceUrl (EPA factors editable)
- **ContactMessage** — Id, Name, Email, Message, CreatedAt, Handled
- **AuditLog** — who changed what, when (all admin writes)
- Identity tables (ApplicationUser : IdentityUser, roles **Admin**, **Editor**)

"Current" carbon value for a region = latest year for that region's preferred source.

## 4. Front end

- **Bootstrap 5.3** — ready-made `bootstrap.min.css` + our own `site.css` that themes it through Bootstrap's CSS variables (no SCSS/Sass build). Vanilla ES modules for our code; Bootstrap's own JS for tooltips/tabs/modals.
- **jQuery policy:** jQuery, jquery-validation and jquery-validation-unobtrusive **stay in the project** (template/Identity pages use them) but new code doesn't depend on them unless there's a clear reason.
- Client validation: the template's jquery-validation-unobtrusive is fine for standard forms; custom interactive widgets are vanilla JS.
- Rich-text editing in admin: Quill 2 (BSD) or TinyMCE (needs licence key) — see Q. All saved HTML sanitized server-side (`HtmlSanitizer`).
- Chart.js 4 for the comparison chart.
- Accessibility: WCAG 2.2 AA — keyboard toggle for strategies, focus states, contrast-checked palette, `prefers-reduced-motion`.
- Keep public URLs so links/SEO survive: `/zone/{slug}`, `/design-strategy/{slug}`, `/carbon`, `/carbon-comparison`, `/about`; 301s for anything that changes.

### 4.1 Proposed palette (to validate against Arcadis brand guidelines)

| Token | Hex | Use |
|---|---|---|
| `--cs-orange` (Arcadis orange) | `#E4610F` | primary actions, highlights, active states |
| `--cs-orange-soft` | `#FBE7DA` | tints, badges |
| `--cs-ink` | `#16181D` | headings, dark hero/header, map chrome |
| `--cs-slate` | `#4A5560` | body text on light |
| `--cs-pine` | `#1F4D46` | secondary / "climate" accent, links on light |
| `--cs-sage` | `#9DBFB1` | subtle secondary, chart gridlines |
| `--cs-sand` | `#F6F3EE` | page background (warm off-white) |
| `--cs-line` | `#E3DED6` | borders, dividers |

Köppen zone colors remain data (editable per zone). Carbon choropleth moves to a sand → orange → deep-red sequential scale that echoes the brand.

**Decisions (2026-09-29, implemented in `wwwroot/css/site.css`):**
- **Accessibility (WCAG AA):** white text on `#E4610F` is only ~3.5 : 1, so primary buttons, active dropdown items and nav pills use `--cs-orange-dark` `#C4520C` (hover `#A94609`, pressed `#8E3A07`). Outline-button text is `#A94609` on light. Brand orange stays for accents, focus rings and checkboxes; use `text-primary` only for large text on light backgrounds (~3.2 : 1 on sand).
- **Light navbar / panels:** `--bs-tertiary-bg` is white.
- **Dark mode (site-wide):** Bootstrap 5.3 `data-bs-theme="dark"` with our palette — ink `#16181D` page, `#1F2228` / `#262A31` panels, `#D5D9DE` text, sand headings, sage `#9DBFB1` links, `#30343B` borders; brand orange as text passes (~5 : 1).
- **Theme selection:** follows the OS by default; a Light / Dark / Auto menu in the navbar overrides it, saved in `localStorage` (`cs-theme`). An inline `<head>` script applies it before first paint. Maps will need a dark Mapbox tile style (Phase 4+).

## 5. Maps

### Decision (2026-09-28)
- **Map library: Leaflet 1.9** (as on the old site) — proven with this exact data (~5,500 Köppen polygons, 267 carbon regions), simplest to maintain, provider-neutral (tile source is one URL). Used from vanilla ES modules, no jQuery.
- **Tiles & address search: Mapbox** under a new Arcadis-owned account (shared mailbox). Style copied from the old personal account (`mdoll/cknkn6oz71ru317nvrvefoghs`).
- **The Mapbox token never reaches the browser:**
  - Tiles are served through our endpoint `GET /map/tiles/{z}/{x}/{y}` which forwards to the Mapbox Static Tiles API (short-lived HTTP caching per Mapbox headers; no persistent tile storage).
  - Address search goes through `GET /api/v1/geocode?q=`.
  - Token lives in server config (IIS environment variable), and is also URL-restricted in the Mapbox dashboard.
- **Cost:** expected to stay inside Mapbox's free tier (100,000 temporary geocoding requests/month; tile requests have their own free allowance — confirm on mapbox.com/pricing when the account is created). A payment card may be required on the account; nothing is charged within the free tier.

### Plugins / replacements
| Old | New |
|---|---|
| `leaflet-omnivore` (deprecated) for TopoJSON | `topojson-client` → GeoJSON → `L.geoJSON` (canvas renderer for the Köppen layer) |
| `leaflet-geosearch` with Mapbox key in the browser | Small custom search box (vanilla JS) calling `/api/v1/geocode`; 300 ms debounce, minimum 3 characters |
| `leaflet.locatecontrol` | Kept (vanilla, no jQuery) |
| Hard-coded token in JS | Tile proxy + server config |

### Geocoding rules
- Use **temporary** geocoding only: results are displayed (fly the map to the address), **never stored**. Storing geocoded coordinates (e.g. saved project addresses) is "permanent" geocoding under Mapbox terms — no free tier (~$5 / 1,000). Revisit if saved projects are ever added.
- Server-side safeguards on `/api/v1/geocode` and the tile proxy: per-IP rate limit plus a configurable **daily request cap**, with an admin email when 80% of the cap is reached.

### Point lookup
Server-side lat/lng → Köppen zone + carbon region via NetTopologySuite, so the API can answer "what zone is this location in" without the browser.

### Alternatives considered (for reference)
MapLibre GL JS (vector, GPU; more complex than we need at zoom 2–8), Mapbox GL JS (proprietary licence, billed per map load, locks us to Mapbox), OpenFreeMap (free, no SLA, no geocoder), Esri (possible via Arcadis enterprise licence), Azure Maps (pay-as-you-go). Switching tile provider later with Leaflet = change the proxy's upstream URL.

## 6. Carbon importer

1. Admin uploads a **CSV or Excel (`.xlsx`, first sheet)** file and picks (or auto-detects by header) a **source profile**: Ember-Countries, Ember-US-States, CER-Canada (profiles are config, new ones addable).
2. Parse with CsvHelper (handles quoted commas, BOM, encodings) or ClosedXML for `.xlsx`; both feed the same validate/match/preview pipeline.
3. Validate: numeric value, unit = gCO2/kWh (or convert), year in range, variable = CO2 intensity.
4. Match each row → `CarbonRegion` by code, then alias, then normalized name. Aggregates (World, EU…) stored but flagged `Aggregate`.
5. **Preview screen**: new / changed (old → new value) / unchanged / unmatched / invalid. Unmatched rows can be mapped to a region inline (creates an alias for next time) or ignored.
6. Commit in a transaction → new `CarbonIntensity` rows (history kept), batch record, audit entry, email summary to admins.
7. Rollback a batch from the import history screen.
8. **Download current data** (decided 2026-10-01): Admin exports the current carbon values as `.xlsx` in exactly the upload layout, so the routine update is download → edit in Excel → upload → review preview → commit.
9. Later (optional): scheduled pull from Ember's API instead of manual CSV.

## 7. API (v1)

Read-only JSON, OpenAPI (Scalar UI), output-cached, rate-limited. **External callers must send `X-Api-Key`** (keys issued in Admin, stored hashed); the site's own pages are authorized same-origin.

- `GET /api/v1/climate-zones` · `GET /api/v1/climate-zones/{slug}` (with strategies)
- `GET /api/v1/design-strategies` · `GET /api/v1/design-strategies/{slug}` (conflicts, reference projects)
- `GET /api/v1/carbon/regions?type=&continent=` · `GET /api/v1/carbon/regions/{code}` (current + history)
- `POST /api/v1/carbon/calculate` — { eui, euiUnit, area, areaUnit, regionCode | gridIntensity } → results + equivalencies
- `GET /api/v1/lookup?lat=&lng=` → Köppen zone + carbon region + intensity
- `GET /api/v1/geocode?q=` → proxied Mapbox search (temporary geocoding; rate-limited + daily cap)
- `GET /api/v1/sponsors`

## 8. Email

`IEmailService` abstraction with templated Razor emails. Provider: **Mailjet** (production, account pending); SMTP to Mailpit for development. Uses: contact form → site admins; import completed/failed; account emails (invite, password reset, lockout). Outbound queue with retry (background `IHostedService` + table).

## 9. Admin & security

- Area `Admin`, `[Authorize(Policy = "AdminArea")]` on everything; roles **Admin** (users, settings, imports) and **Editor** (content).
- ASP.NET Core Identity local accounts now; **public registration disabled**; users invited by an Admin; lockout, strong passwords, optional TOTP 2FA.
- SSO later: add Microsoft Entra ID via `Microsoft.Identity.Web` as an additional scheme mapped to the same `ApplicationUser`/roles — designed-for now (claims-based policies, no hard-coded role checks in views).
- Anti-forgery on all posts, HTML sanitization, upload validation (type sniffing, size limits, SVG sanitization), security headers (CSP), secrets in User Secrets / Key Vault — **nothing in JS or appsettings committed**.
- Admin screens: Dashboard · Climate zones · Zone groups · Diagrams · Design strategies (+ conflicts matrix, reference projects) · Carbon regions/aliases · Carbon imports · Equivalency factors · Sponsors · Content blocks · Media library · Contact messages · Users · Audit log.

## 10. Decisions & open questions

### Decided (2026-09-28)
- **Hosting:** Arcadis on-prem **IIS + SQL Server**. → Media on local disk / file share behind `IFileStorage`; secrets via IIS environment variables (not committed appsettings); email via Mailjet (see below); Data Protection keys persisted to a folder/SQL so logins survive app-pool recycles.
- **Configuration & secrets:** development values live in **User Secrets** (never in committed files). Production reads **environment variables prefixed `CS__`** (ClimateScout), set per IIS site: `CS__ConnectionStrings__DefaultConnection`, `CS__Mapbox__AccessToken`, … — the prefix is stripped and `__` becomes `:`. `CS__` values override everything else. Exception: `ASPNETCORE_ENVIRONMENT` keeps its standard name (read by the framework before our code). Unprefixed variables are still read by the framework default, but `CS__` is the documented convention.
- **Strategy conflicts:** always **symmetric**. Saving A↔B writes both rows; migration unions the old one-directional data.
- **Carbon calculator (2026-09-30):** EUI and area must be **> 0** (rejected otherwise — a 0 is almost always a typo); grid intensity may be **0** (fully renewable grid) but not negative. Grid intensity is **g CO2e/kWh only** — the old page's g/kBTU and g/GJ branches were never offered and set the wrong factor, so they are not ported. EUI units: kBtu/ft², kWh/m², GJ/m²; area: ft², m².
- **WordPress import (2026-09-30):** our own zone slugs use **hyphens** (`cfa_humid-subtropical` → `cfa-humid-subtropical`); old underscore links redirect (Phase 4). **External URLs** (e.g. 2030palette.org) are imported unchanged. Reference project "description" holds a place name, so it goes into `ReferenceProject.Location`; sector is empty in the old data. **Media:** all 149 old images were downloaded once by a script (not an app command) into `wwwroot/img/media/{projects,strategies,other}/` (committed to git) under clean flat names (lowercase, non-alphanumerics → `-`, no year/month folders). `Data/Import/media-map.csv` records WordPress ID → `StoragePath` (plus title, alt text, size); `import wordpress` creates the `MediaAsset` rows from it and links images through it — no extra column, no data in migrations. Identical duplicates share one file (Chadstone 107/165 → 148 files).
- **API access:** **API key required** for all external callers. The site's own pages call the API with same-origin cookie/antiforgery auth. Admin screen to issue, name, rotate and revoke keys; keys stored hashed; per-key rate limits and usage logging.

- **Maps:** **Mapbox** under a new Arcadis-owned account (shared mailbox), style copied from the old personal account, token URL-restricted to our domains and read from server config. Rendered with **Leaflet**; tiles and address search proxied through our server so the token never reaches the browser. Temporary geocoding only (results not stored). See §5.
- **Email:** **Mailjet** (account to be set up later). Build `IEmailService` now with a Mailjet implementation + SMTP/Mailpit for development.
- **Rich-text editor:** **Quill 2**; HTML sanitized server-side.
- **jQuery:** stays installed; not used by new code in most cases.
- **Data:** no seeding — data commands built into the app initialize/import data on demand (§3.3).
- **Working style:** step-by-step walkthrough; each step is explained and reviewed before the next.
- **Sponsors:** own page **and** a logo strip in the footer on every page (per-sponsor `ShowInFooter` toggle, date-bounded visibility).
- **Styling:** Bootstrap 5.3.8 `bootstrap.min.css` + our own `site.css` using CSS variables — **no SCSS** (decided 2026-09-28).
- **Front-end libraries:** all managed by **LibMan** (`libman.json`, provider jsDelivr, exact versions), **restored on build** via `Microsoft.Web.LibraryManager.Build`; `wwwroot/lib/` is not committed. Pinned: Bootstrap 5.3.8, jQuery 3.7.1 (not 4.x — unobtrusive validation requires 3.x), jquery-validation 1.22.1, jquery-validation-unobtrusive 4.0.0, Chart.js 4.5.1, Leaflet 1.9.4, topojson-client 3.1.0, leaflet.locatecontrol 0.90.1, Quill 2.0.3.
- **Font:** **Fira Sans** (closest free match to Arcadis's FS Elliot Pro), **self-hosted** via LibMan (`@fontsource/fira-sans` 5.3.0: 400, 400 italic, 500, 700, Latin) — no calls to Google at runtime. Swap to FS Elliot Pro later if licensed font files become available.
- **Structure:** keep the name **A-U_ClimateScout**; **flat** — single web project + one test project `tests/A-U_ClimateScout.Tests`; separation by folders (§3.1). (A Core/Infrastructure/DataTool split was tried and dropped as overkill for this size.)

### Open
- Official Arcadis brand guideline/logo files

---

## 11. Step-by-step checklist

### Phase 0 — Decisions & setup
- [ ] Answer open questions (§10)
- [ ] Confirm palette against Arcadis brand guidelines; obtain official logo files
- [ ] Create Arcadis-owned Mapbox account; copy style `mdoll/cknkn6oz71ru317nvrvefoghs`; issue URL-restricted token
- [ ] Set up Mailjet account (later)

### Phase 1 — Platform setup (walkthrough, one step at a time)
- [x] 1.1 Project structure: single web project + `tests/A-U_ClimateScout.Tests` (flat; Copilot models/seeder moved to `xfer/copilot-reference`)
- [x] 1.2 Test project set up and running (`dotnet test` / Test Explorer)
- [x] 1.3 Identity & DbContext: `Identity/ApplicationUser.cs`; `ApplicationDbContext` on `ApplicationUser`; retire the template's Identity migration; update `Program.cs` and `_LoginPartial`
- [x] 1.4 Configuration: connection string in User Secrets (dev); production reads `CS__`-prefixed environment variables. Typed settings classes (Mapbox, Email, Storage, ApiKeys) are deferred — each is added with the feature that uses it (Phases 3–8).
- [x] 1.5 Cross-cutting: `.editorconfig` (code style as suggestions) and Serilog (console + daily rolling files in `logs/`, 30-day retention, per-request line). Deferred: styled error pages (1.7), API ProblemDetails (Phase 7), `/health` check (Phase 2 / deployment), analyzers (revisit once there is real code).
- [x] 1.6 Front-end setup
  - [x] change 1: move GreenIQ `site.css` and old toggle CSS to `xfer/reference/css/`
  - [x] change 2: `libman.json` — all front-end libraries at pinned versions (jsDelivr)
  - [x] change 3: `Microsoft.Web.LibraryManager.Build` restores on build; `wwwroot/lib/` git-ignored; template libs removed
  - [x] change 4: fresh `wwwroot/css/site.css` — palette (§4.1) via Bootstrap CSS variables, Fira Sans, base styles, light + dark themes
  - [x] change 5: `_Layout` links Fira Sans; no-flash theme script in `<head>`; `wwwroot/js/theme.js`; Light / Dark / Auto navbar menu; theme-aware navbar classes; template `_Layout.cshtml.css` removed
  - [x] change 6: build, run, verify in both themes; commit `dfa9179`
- [x] 1.7 Layout shell (commit `7d26c6a`)
  - [x] change 1: branding — interim logo PNGs from the old theme (`wwwroot/img/brand/logo-on-light.png` / `logo-on-dark.png`, swapped by theme); titles "Page · Arcadis ClimateScout®"
  - [x] change 2: navbar — Climate Zones (`/`), Carbon, Sponsors, About, Contact at final URLs with current-page highlight; icon-only theme menu (inline SVG sprite, Bootstrap Icons); signed-in user menu
  - [x] change 3: footer placeholder — "© {year} Arcadis" (no trademark wording until Arcadis confirms it), Contact, Arcadis privacy, Arcadis.com, Staff sign-in; template Privacy page removed
  - [x] change 4: Admin area skeleton — `Identity/Policies.cs` (roles + `AdminArea` policy), `AdminController` base class with `[Area("Admin")]` + `[Authorize(Policy = AdminArea)]`, placeholder dashboard at `/admin`
  - [x] change 5: public registration disabled — `Register` and `RegisterConfirmation` overridden to return 404
  - [x] change 6: styled error pages — `ErrorController` at `/error/{code}`, status-code re-execute, `NotFound` and `Error` views
  - [x] change 7: build, run, verify, commit
  - Follow-ups:
    - The Identity sign-in page still shows "Register as a new user" (it leads to the 404). Remove it when the Identity pages are restyled (Phase 5).
    - On re-executed 404 pages the navbar sees `/error/404`, so no menu item is highlighted. This is expected; real pages highlight correctly.
    - ~~Signed-in Admin link untested~~ — verified in Phase 2 with `tool init admin`.
    - Access-denied flow (signed-in, not Admin) is still untested; needs a non-Admin account (Phase 5 user management).
    - Close off automatic account creation through external sign-in when Entra ID is added (§9).
    - Swap in the official Arcadis logo files (SVG) when they arrive.

### Phase 2 — Domain & database
- [x] Entities + EF configurations (§3.2) — `afb54b5`
- [x] Create database from a fresh initial migration (`tool db migrate`) — `afb54b5`
- [x] Identity: `ApplicationUser`, roles; first Admin via `tool init admin` — `3fce944`
  - Also `tool init reference` (roles, zone groups, diagrams, equivalency factors, carbon sources; only adds missing rows).
  - Issued passwords are one-time: `MustChangePassword` forces a password change at first sign-in. Verified end to end.
- [x] Unit tests for conflict rules and calculator — `6588e41`, `2967d66` (11 tests, xUnit)
  - `StrategyConflictRules.MakeSymmetric` and `CarbonCalculator.Calculate` in `Services/`; tests in `tests/A-U_ClimateScout.Tests/Services/`.
  - Candidate tests not yet written: negative inputs, GJ/m² EUI, equivalencies (tonnes → trees, gallons, from `EquivalencyFactors`).

### Phase 3 — Data command importers (WordPress + media + carbon)
- [x] Media: one-time scripted download of **all 149** attachments from `climatescout.arcadis.com` into `wwwroot/img/media/` — `projects/` (74 attachments → 73 files), `strategies/` (27), `other/` (48, unused on the old site but kept) — with clean flat names (`2021/01/cool-roof@2x-1.png` → `strategies/cool-roof-2x-1.png`). Log: `Data/Import/media-map.csv` (WordPressId, OldPath, Folder, StoragePath, Title, AltText, Width, Height). Chadstone 107/165 are identical → one file (148 files, ~52 MB).
- [ ] `import wordpress` reads `climatescout-*.sqlite`: `MediaAsset` rows from `media-map.csv`, zones, groups, strategies, zone↔strategy links, conflicts, reference projects, content pages, carbon sources
- [ ] Fix encoding (mojibake), fix known data errors, strip CRTKL references
- [ ] Import 4 diagram SVGs; verify every strategy slug has a `ds-{slug}` layer in each diagram it's used with
- [ ] Convert carbon geometry to `wwwroot/geo/carbon-regions.geojson` keyed by ISO code; `import geo` loads `CarbonRegion` + aliases
- [ ] Import the three new CSVs through the real importer (proves the importer)
- [ ] Verification report: counts & spot checks vs old site

### Phase 4 — Public site
- [ ] Layout: header/nav, footer (Arcadis, no CRTKL)
- [ ] Footer sponsor strip view component (logos link out, grayscale → color on hover, hidden when no visible sponsors, cached)
- [ ] Map module (vanilla ES): Leaflet setup, tile proxy endpoint, custom geocode search box, locate control
- [ ] Home: climate map (Leaflet + Köppen TopoJSON layer, group filter tabs, search, locate, click → zone)
- [ ] Zone page: description, diagram with strategy toggles, conflicts, shareable URL state, print view
- [ ] Strategy page: content, image, reference projects with lightbox (vanilla), 2030 Palette link
- [ ] Carbon map page (choropleth, legend, hover info, click → comparison)
- [ ] Carbon comparison: calculator (calls API), result panel, Chart.js comparison with region filters, zoomed climate map
- [ ] About, Sponsors, Contact pages
- [ ] SEO: titles, meta, Open Graph, sitemap.xml, robots.txt, redirects from old URLs (including old underscore zone slugs → hyphen form, e.g. `cfa_humid-subtropical` → `cfa-humid-subtropical`)
- [ ] Accessibility & responsive pass; print stylesheet

### Phase 5 — Admin area
- [ ] Admin layout + dashboard
- [ ] CRUD: zone groups, zones, diagrams, strategies (conflicts matrix, zone assignment, reference projects)
- [ ] CRUD: sponsors (logo upload, tier, dates, ShowInFooter, drag-to-reorder), content blocks, equivalency factors, carbon regions & aliases
- [ ] Media library (upload, alt text, replace). Uploads go to `img/media/projects/`, `strategies/` or `other/` automatically from what is being edited — no folder prompt.
- [ ] Users & roles (invite, disable, reset)
- [ ] Audit log viewer

### Phase 6 — Carbon importer
- [ ] Source profiles + header auto-detection; accept `.csv` and `.xlsx` (ClosedXML)
- [ ] "Download current data" as `.xlsx` in the upload layout (round-trip editing in Excel)
- [ ] Parse/validate/match pipeline with unit tests using the three sample CSVs
- [ ] Preview/diff screen with inline alias mapping
- [ ] Commit, history, rollback
- [ ] Import-complete email

### Phase 7 — API
- [ ] v1 endpoints (§7) with DTOs, OpenAPI + Scalar
- [ ] API key auth handler, admin key management (issue/rotate/revoke, hashed), per-key rate limits & usage log
- [ ] Output caching, CORS policy
- [ ] Server-side point lookup (NetTopologySuite)
- [ ] Geocode proxy + tile proxy: per-IP rate limit, configurable daily cap, 80%-of-cap admin email
- [ ] Integration tests for contracts

### Phase 8 — Email
- [ ] `IEmailService` + Mailjet sender (+ SMTP/Mailpit for dev), Razor templates, outbox + retry
- [ ] Contact form (with anti-spam: honeypot + rate limit / Turnstile)
- [ ] Account emails (invite, reset)

### Phase 9 — Hardening & launch
- [ ] Security headers/CSP, upload & SVG sanitization, secrets in IIS `CS__` environment variables
- [ ] Performance: IIS static compression + caching for GeoJSON, image resizing (thumbnails); shrink the 4 imported photos over 1 MB for the web
- [ ] UAT with PDD team; content review
- [ ] Deploy to IIS (hosting bundle, app pool, Data Protection key store, env-var secrets), DNS cut-over for climatescout.arcadis.com, monitor
- [ ] Decommission WordPress after sign-off
- [ ] **Owner's final fixes** — data gaps found during the import, left as on the old site and fixed by hand in Admin before launch:
  - [ ] Am Tropical Monsoon: set its diagram (none on the old site; the other tropical zones use Hot-Humid). The import logs a warning for it.
  - [ ] Clerestories and Skylights: add its 2030 Palette link (none on the old site).
  - [ ] East-West Shading: fix its 2030 Palette link (points to earth-sheltering).
  - [ ] Solar Greenhouse: review its conflicts (it conflicts with 24 of the other 26 strategies; may be intended).
  - [ ] Stack Ventilation → reference project "NCI Tower Competition": add its location (empty on the old site).

### Later
- [ ] Microsoft Entra ID SSO for admin
- [ ] Scheduled Ember API pull
- [ ] Additional climate classifications (ASHRAE) if needed
