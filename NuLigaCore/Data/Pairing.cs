namespace NuLigaCore.Data
{
    public class Pairing
    {
        public int BoardNumber { get; set; }
        public string? HomePlayer { get; set; }
        public int HomePlayerDWZ { get; set; }
        public string? GuestPlayer { get; set; }
        public int GuestPlayerDWZ { get; set; }
        public string? BoardPointsRaw { get; set; }
        public BoardPoints BoardPoints => BoardPointsRaw?.AsBoardPoints() ?? BoardPoints.NotPlayed;
    }
}