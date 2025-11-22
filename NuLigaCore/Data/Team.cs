using System.Text.Json.Serialization;

namespace NuLigaCore.Data
{
    public class Team
    {
        public int Rang { get; set; }
        public string Name { get; set; } = string.Empty;
        public double DWZ => (TeamPlayers != null && TeamPlayers.Count > 0) ? Math.Round(TeamPlayers.Average(x => x.DWZ)) : 0;

        [JsonIgnore]
        public double[]? BoardPointsPerRank { get; set; }
        public int Spiele { get; set; }
        public int Punkte { get; set; }
        public double BP { get; set; }

        [JsonIgnore]
        public string? TeamUrl { get; set; }

        [JsonIgnore]
        public List<Player>? TeamPlayers { get; set; }

        [JsonIgnore]
        public List<GameDay>? GameDays { get; set; }
        public double BW => ComputeBerlinTieBreakSumOverAllGameDays();

        public double ComputeBerlinTieBreakSumOverAllGameDays()
        {
            var bwTotal = 0.0;
            foreach (var gameDay in GameDays ?? Enumerable.Empty<GameDay>())
            {
                if (gameDay.Report == null)
                {
                    continue;
                }

                bwTotal += gameDay.Report.ComputeBw(gameDay.HomeTeam == Name);
            }

            return bwTotal;
        }

        public override string ToString()
        {
            var boardPointsPerRankStr = string.Join(", ", BoardPointsPerRank ?? Enumerable.Empty<double>());

            var playersStr = "Players:";
            foreach (var player in TeamPlayers ?? Enumerable.Empty<Player>())
            {
                playersStr += $"\n  {player}";
            }

            return $"{Rang}. {Name} - Games: {Spiele}, Points: {Punkte}, BoardPoints: {BP}, BoardPointsPerRank: {boardPointsPerRankStr}, {playersStr}";
        }
    }
}