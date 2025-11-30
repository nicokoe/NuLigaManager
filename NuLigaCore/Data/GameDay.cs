using CsvHelper.Configuration;
using System.Globalization;

namespace NuLigaCore.Data
{
    public class GameDay
    {
        public DateTime Datum { get; set; }
        public int Runde { get; set; }
        public string? HeimMannschaft { get; set; }
        public double HeimMannschaftDWZ => (Report != null && Report.Pairings.Count > 0) ? Math.Round(Report.Pairings.Average(x => x.HeimSpielerDWZ)) : 0;
        public string? GastMannschaft { get; set; }
        public double GastMannschaftDWZ => (Report != null && Report.Pairings.Count > 0) ? Math.Round(Report.Pairings.Average(x => x.GastSpielerDWZ)) : 0;
        public string? BrettPunkte { get; set; }
        public string? ReportUrl { get; set; }
        public GameReport? Report { get; set; }

        public override string ToString()
        {
            return $"Round {Runde} on {Datum.ToShortDateString()}: {HeimMannschaft} vs {GastMannschaft} - BoardPoints: {BrettPunkte}";
        }
    }

    public sealed class GameDayMap : ClassMap<GameDay>
    {
        public GameDayMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.ReportUrl).Ignore();
        }
    }
}