using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MarsOutpost
{
    public sealed class ScienceReferenceEntry
    {
        public string Id { get; }
        public string Category { get; }
        public string Title { get; }
        public string ArchiveRecordId { get; }
        public string ArchiveTimestampUtc { get; }
        public string InstrumentOrSystem { get; }
        public string Url { get; }
        public string GameMapping { get; }
        public string ModelDisclaimer { get; }

        public ScienceReferenceEntry(
            string id,
            string category,
            string title,
            string archiveRecordId,
            string archiveTimestampUtc,
            string instrumentOrSystem,
            string url,
            string gameMapping,
            string modelDisclaimer)
        {
            Id = id;
            Category = category;
            Title = title;
            ArchiveRecordId = archiveRecordId;
            ArchiveTimestampUtc = archiveTimestampUtc;
            InstrumentOrSystem = instrumentOrSystem;
            Url = url;
            GameMapping = gameMapping;
            ModelDisclaimer = modelDisclaimer;
        }
    }

    /// <summary>
    /// Verified NASA archive references mapped to the educational Mars Outpost scenario,
    /// explicitly separating historical archive metadata from designed gameplay coefficients.
    /// </summary>
    public static class NasaScienceData
    {
        public const string EducationalDisclaimer =
            "Educational Simulation Notice: Archive IDs and UTC timestamps come from NASA records. " +
            "All 0-100 reserve units, 720s sol compression, risk scores, and dust loss factors are " +
            "designed training coefficients, NOT measured Mars surface doses or live forecasts.";

        public static readonly IReadOnlyList<ScienceReferenceEntry> Entries = new ReadOnlyCollection<ScienceReferenceEntry>(
            new[]
            {
                new ScienceReferenceEntry(
                    id: "donki-sep-1",
                    category: "NASA DONKI SEP Archive",
                    title: "Solar Energetic Particle Event #1",
                    archiveRecordId: "2024-05-09T13:59:00-SEP-001",
                    archiveTimestampUtc: "2024-05-09T13:59Z (linked flare 2024-05-09T08:45Z)",
                    instrumentOrSystem: "ACE/EPAM (REleASE) 15.8-39.8 MeV; Alert 20240509-AL-004",
                    url: "https://ccmc.gsfc.nasa.gov/DONKI/view/SEP/30609/-1",
                    gameMapping: "Drives Sol 1 radiation warning (t=60s, ~5h flare-to-SEP lead compressed to 300s) and event window [360s, 420s).",
                    modelDisclaimer: "Near-Earth/heliospheric SEP log; inside (0.15/s) and outside (1.20/s) risk scores are designed training indices."),
                new ScienceReferenceEntry(
                    id: "donki-sep-2",
                    category: "NASA DONKI SEP Archive",
                    title: "Solar Energetic Particle Event #2",
                    archiveRecordId: "2024-05-10T13:35:00-SEP-001",
                    archiveTimestampUtc: "2024-05-10T13:35Z (linked CME 2024-05-08T05:36Z)",
                    instrumentOrSystem: "GOES-P: SEISS >10 MeV",
                    url: "https://ccmc.gsfc.nasa.gov/DONKI/view/SEP/30677/-1",
                    gameMapping: "Drives second SEP wave ~24h later during Sol 1 night [660s, 720) with warning at t=600s.",
                    modelDisclaimer: "Demonstrates multi-wave particle hazard during sleep; risk index is not a Mars surface rad/Sv calculation."),
                new ScienceReferenceEntry(
                    id: "nasa-eclss",
                    category: "NASA ECLSS Reference",
                    title: "Environmental Control and Life Support System",
                    archiveRecordId: "ISS ECLSS WRS / OGS",
                    archiveTimestampUtc: "Ongoing ISS reference",
                    instrumentOrSystem: "Water Recovery System (WRS) & Oxygen Generation System (OGS)",
                    url: "https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/",
                    gameMapping: "Models electricity + water electrolysis for O2, wastewater recycling into water, and power priority (Life Support > Recycling > Research).",
                    modelDisclaimer: "0-100 game units and per-second rates are simplified for ages 7-12; real ISS water recovery is ~90% mass balance."),
                new ScienceReferenceEntry(
                    id: "nasa-mars-dust",
                    category: "NASA Mars Dust Reference",
                    title: "2018 Planet-Encircling Dust Storm (Opportunity / Curiosity)",
                    archiveRecordId: "2018 Global Dust Storm (Sol 4190+ Mars Weather context)",
                    archiveTimestampUtc: "June 2018 archive",
                    instrumentOrSystem: "Opportunity solar arrays & Curiosity atmospheric opacity (tau)",
                    url: "https://science.nasa.gov/mission/mars-exploration-rovers/",
                    gameMapping: "Drives Sol 2 Regional Dust Storm [780s, 960) reducing solar array generation separately from panel surface fouling.",
                    modelDisclaimer: "Game uses a fixed 50% (0.50) solar reduction factor for 180s rather than raw atmospheric tau curves.")
            });
    }
}
