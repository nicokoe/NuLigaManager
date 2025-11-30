using CsvHelper.Configuration;
using System.Globalization;

namespace NuLigaCore.Data
{
    public class Pairing
    {
        public int Brett { get; set; }
        public string? HeimSpieler { get; set; }
        public int HeimSpielerDWZ { get; set; }
        public string? GastSpieler { get; set; }
        public int GastSpielerDWZ { get; set; }
        public string? Ergebnis { get; set; }
        public BoardPoints BoardPoints => Ergebnis?.AsBoardPoints() ?? BoardPoints.NotPlayed;
    }

    public sealed class PairingMap : ClassMap<Pairing>
    {
        public PairingMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.BoardPoints).Ignore();
        }
    }
}