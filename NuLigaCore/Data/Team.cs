using CsvHelper.Configuration;
using System.Globalization;
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

        public void GameDayReportLoaded(GameDay gameDay)
        {
            if (gameDay.Report == null)
            {
                return;
            }

            var isHomeTeam = gameDay.HomeTeam == Name;

            foreach (var player in TeamPlayers ?? Enumerable.Empty<Player>())
            {
                var pairing = gameDay.Report.GetPairingForPlayer(player.Name, isHomeTeam);
                var result = pairing?.BoardPoints.ToDouble(isHomeTeam) ?? -1;
                player.PointsPerGameDay?[gameDay.Round - 1] = result;
            }
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

    public sealed class TeamMap : ClassMap<Team>
    {
        public TeamMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.TeamUrl).Ignore();
        }
    }
}