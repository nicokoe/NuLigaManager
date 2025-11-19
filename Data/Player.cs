namespace NuLigaManager.Data
{
    public class Player
    {
        public int BoardNumber { get; set; }
        public string? Name { get; set; }
        public int DWZ { get; set; }
        public int Games { get; set; }
        public string? BoardPoints { get; set; }

        public override string ToString()
        {
            return $"{BoardNumber}. {Name} (DWZ: {DWZ}) - Games: {Games}, BoardPoints: {BoardPoints}";
        }
    }
}