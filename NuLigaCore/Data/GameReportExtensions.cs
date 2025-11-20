namespace NuLigaCore.Data
{
    public static class GameReportExtensions
    {
        public static BoardPoints AsBoardPoints(this string bp)
        {
            return bp switch
            {
                "1:0" => BoardPoints.HomeWin,
                "+:-" => BoardPoints.HomeWin,
                "0:1" => BoardPoints.GuestWin,
                "-:+" => BoardPoints.GuestWin,
                "½:½" => BoardPoints.Draw,
                "-:-" => BoardPoints.NotPlayed,
                _ => BoardPoints.NotPlayed,
            };
        }
    }
}