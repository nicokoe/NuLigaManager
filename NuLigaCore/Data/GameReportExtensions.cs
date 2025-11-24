namespace NuLigaCore.Data
{
    public static class GameReportExtensions
    {
        public static BoardPoints AsBoardPoints(this string bp)
        {
            return bp switch
            {
                "1:0" => BoardPoints.HomeWin,
                "+:-" => BoardPoints.HomeWinByDefault,
                "0:1" => BoardPoints.GuestWin,
                "-:+" => BoardPoints.GuestWinByDefault,
                "½:½" => BoardPoints.Draw,
                "-:-" => BoardPoints.NotPlayed,
                _ => BoardPoints.NotPlayed,
            };
        }

        public static double ToDouble(this BoardPoints boardPoints, bool isHomeTeam)
        {
            if (!isHomeTeam)
            {
                return boardPoints switch
                {
                    BoardPoints.GuestWinByDefault => 1000,
                    BoardPoints.GuestWin => 1.0,
                    BoardPoints.HomeWin => 0.0,
                    BoardPoints.HomeWinByDefault => 0.0,
                    BoardPoints.Draw => 0.5,
                    _ => -1,
                };
            }

            return boardPoints switch
            {
                BoardPoints.HomeWinByDefault => 1000,
                BoardPoints.HomeWin => 1.0,
                BoardPoints.GuestWin => 0.0,
                BoardPoints.GuestWinByDefault => 0.0,
                BoardPoints.Draw => 0.5,
                _ => -1,
            };
        }
    }
}