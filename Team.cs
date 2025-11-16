namespace NuLigaScraper
{
    public class Team
    {
        public int Rank {get; set; }
        public string? Name { get; set; }
        public int[] BoardPointsPerRank = new int[9];
        public int Games { get; set; }
        public int Points { get; set; }
        public double BoardPointsSum { get; set; }

        public override string ToString()
        {
            return $"{Rank}. {Name} - Games: {Games}, Points: {Points}, BoardPoints: {BoardPointsSum}";
        }
    }
}