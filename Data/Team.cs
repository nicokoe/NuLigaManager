namespace NuLigaManager.Data
{
    public class Team
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public double[]? BoardPointsPerRank { get; set; }
        public int Games { get; set; }
        public int Points { get; set; }
        public double BoardPointsSum { get; set; }

        public List<Player>? TeamPlayers { get; set; }
        public List<GameDay>? GameDays { get; set; }

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

            return $"{Rank}. {Name} - Games: {Games}, Points: {Points}, BoardPoints: {BoardPointsSum}, BoardPointsPerRank: {boardPointsPerRankStr}, {playersStr}";
        }
    }
}