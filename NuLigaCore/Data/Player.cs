using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json.Serialization;

namespace NuLigaCore.Data
{
    public class Player
    {
        public int Brett { get; set; }
        public string? Name { get; set; }
        public int DWZ { get; set; }

        [JsonIgnore]
        public int Games { get; set; }

        public double[]? PunkteProSpieltag { get; set; }

        public override string ToString()
        {
            return $"{Brett}. {Name} (DWZ: {DWZ}) - Games: {Games}";
        }
    }

    public sealed class PlayerMap : ClassMap<Player>
    {
        public PlayerMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.Games).Ignore();
            Map(m => m.PunkteProSpieltag);
        }
    }
}