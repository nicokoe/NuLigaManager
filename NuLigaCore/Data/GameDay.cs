namespace NuLigaCore.Data
{
    public class GameDay
    {
        public DateTime Date { get; set; }
        public int Round { get; set; }
        public string? HomeTeam { get; set; }
        public double HomeTeamDWZ => (Report != null && Report.Pairings.Count > 0) ? Math.Round(Report.Pairings.Average(x => x.HomePlayerDWZ)) : 0;
        public string? GuestTeam { get; set; }
        public double GuestTeamDWZ => (Report != null && Report.Pairings.Count > 0) ? Math.Round(Report.Pairings.Average(x => x.GuestPlayerDWZ)) : 0;
        public string? BoardPoints { get; set; }
        public string? ReportUrl { get; set; }
        public GameReport? Report { get; set; }

        public override string ToString()
        {
            return $"Round {Round} on {Date.ToShortDateString()}: {HomeTeam} vs {GuestTeam} - BoardPoints: {BoardPoints}";
        }
    }
}