namespace ScrapeNuLigaChess.Data
{
    public class Team
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public double[] BoardPointsPerRank = new double[9];
        public int Games { get; set; }
        public int Points { get; set; }
        public double BoardPointsSum { get; set; }

        public List<Player>? TeamPlayers { get; set; }

        public override string ToString()
        {
            var boardPointsPerRankStr = string.Join(", ", BoardPointsPerRank);

            var playersStr = "Players:";
            foreach (var player in TeamPlayers ?? Enumerable.Empty<Player>())
            {
                playersStr += $"\n  {player}";
            }

            return $"{Rank}. {Name} - Games: {Games}, Points: {Points}, BoardPoints: {BoardPointsSum}, BoardPointsPerRank: {boardPointsPerRankStr}, {playersStr}";
        }
    }
}