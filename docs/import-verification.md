# Import verification report

Generated 2026-10-01 by comparing the development database with the old WordPress database (`climatescout-2016-06-11.sqlite`), the image folder and the carbon data files. Every row is compared, not a sample.

**Result: 25 of 25 checks passed.**

Text is compared without HTML tags and extra spaces, because the import adds `<p>` paragraphs and changes `<acronym>` to `<abbr>`.

| Area | Check | Expected | Actual | |
|---|---|---|---|---|
| Counts | Climate zones | 31 | 31 | ✅ |
| Counts | Design strategies | 27 | 27 | ✅ |
| Counts | Zone ↔ strategy links | 627 | 627 | ✅ |
| Counts | Reference projects | 74 | 74 | ✅ |
| Counts | Conflict rows (each pair stored both ways) | 112 | 112 | ✅ |
| Counts | Images (148 old-site files + 4 diagram SVGs) | 152 | 152 | ✅ |
| Counts | Content blocks | 7 | 7 | ✅ |
| Counts | Carbon regions | 293 | 293 | ✅ |
| Counts | Region aliases | 32 | 32 | ✅ |
| Zones | Fields compared (name, slug, colour, map ID, diagram, group, description text, strategies) | 248 | 248 | ✅ |
| Strategies | Fields compared (name, summary, body text, 2030 link, both images, zones, conflicts) | 216 | 216 | ✅ |
| Reference projects | Fields compared (name, location, link, image), in order | 296 | 296 | ✅ |
| Images | Image rows whose file is missing | 0 | 0 | ✅ |
| Images | Image rows whose file size differs | 0 | 0 | ✅ |
| Images | Files in img/media with no image row | 0 | 0 | ✅ |
| Images | Diagrams linked to their SVG | 4 | 4 | ✅ |
| Content | About page text matches the old page (CallisonRTKL → Arcadis) | Why CLIMATE SCOUT Arcadis’ vision for the future is designin… | Why CLIMATE SCOUT Arcadis’ vision for the future is designin… | ✅ |
| Content | Content blocks mentioning CallisonRTKL/CRTKL | 0 | 0 | ✅ |
| Content | Lines of text left outside a paragraph or other block tag | 0 | 0 | ✅ |
| Carbon regions | Map shapes with a matching region | 264 | 264 | ✅ |
| Carbon regions | Regions shown on the map that have a shape | 264 | 264 | ✅ |
| Carbon regions | US states with parent US + provinces with parent CA | 65 | 65 | ✅ |
| Carbon regions | Names in CEI_all_countries_from Ember.csv that match a region or alias | 228 | 228 | ✅ |
| Carbon regions | Names in CEI_USA_from Ember.csv that match a region or alias | 53 | 53 | ✅ |
| Carbon regions | Names in CEI_Canada_from Canada Energy Regulator.csv that match a region or alias | 12 | 12 | ✅ |

## Differences

None.

## Known gaps (not import errors)

These come from the old site and are listed under Phase 9 "Owner's final fixes" in the modernization plan:

- Am Tropical Monsoon has no diagram (the old site fell back to Hot-Dry).
- As Tropical Savanna, Dry Summer has no map ID (not a class on the Köppen map).
- Clerestories and Skylights has no 2030 Palette link; East-West Shading's points to earth-sheltering.
- Solar Greenhouse conflicts with 24 of the other 26 strategies (to review).
- 26 reference-project links point to callisonrtkl.com.
- NCI Tower Competition has no location.
- 4 diagram layers are missing (Temperate: evaporative cooling towers; Cold: cross ventilation, solar shading, stack ventilation).
- Yukon has a map shape but no carbon data; 13 small places have carbon data but no map shape.
