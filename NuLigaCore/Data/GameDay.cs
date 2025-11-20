namespace NuLigaCore.Data
{
    public class GameDay
    {
        public DateTime Date { get; set; }
        public int Round { get; set; }
        public string? HomeTeam { get; set; }
        public string? GuestTeam { get; set; }
        public string? BoardPoints { get; set; }
        public GameReport? Report { get; set; }

        public override string ToString()
        {
            return $"Round {Round} on {Date.ToShortDateString()}: {HomeTeam} vs {GuestTeam} - BoardPoints: {BoardPoints}";
        }
    }
}