namespace NuLigaCore.Data
{
    public class GameReport
    {
        public List<Pairing> Pairings { get; set; } = [];

        public double ComputeBw(bool forHomeTeam = true)
        {
            var boardCount = Pairings.Count;
            var bwTotal = 0.0;
            foreach (var pairing in Pairings)
            {
                var points = (boardCount + 1) - pairing.BoardNumber;
                bwTotal += points * FactorForBoardResult(pairing.BoardPoints, forHomeTeam);
            }

            return bwTotal;
        }

        private static double FactorForBoardResult(BoardPoints boardPoints, bool forHomeTeam)
        {
            if (!forHomeTeam)
            {
                return boardPoints switch
                {
                    BoardPoints.GuestWin => 1.0,
                    BoardPoints.Draw => 0.5,
                    _ => 0.0,
                };
            }
            return boardPoints switch
            {
                BoardPoints.HomeWin => 1.0,
                BoardPoints.Draw => 0.5,
                _ => 0.0,
            };
        }
    }

    public class Pairing
    {
        public int BoardNumber { get; set; }
        public string? HomePlayer { get; set; }
        public int HomePlayerDWZ { get; set; }
        public string? GuestPlayer { get; set; }
        public int GuestPlayerDWZ { get; set; }
        public BoardPoints BoardPoints { get; set; }
    }

    public enum BoardPoints
    {
        HomeWin,
        GuestWin,
        Draw,
        NotPlayed
    }
}