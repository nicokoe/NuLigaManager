namespace NuLigaCore.Data
{
    public class Player
    {
        public int Brett { get; set; }
        public string? Name { get; set; }
        public int DWZ { get; set; }
        public int Games { get; set; }
        public string? BoardPoints { get; set; }

        public double[]? PointsPerGameDay { get; set; }

        public override string ToString()
        {
            return $"{Brett}. {Name} (DWZ: {DWZ}) - Games: {Games}, BoardPoints: {BoardPoints}";
        }
    }
}