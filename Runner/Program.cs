using HtmlAgilityPack;
using NuLigaCore;
using System.Text.Json;

namespace NuLigaRunner
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            string urlLandesLiga2 = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Baden+25%2F26&group=4610";
            string urlKreisklasseA = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Karlsruhe+25%2F26&group=4646";
            string urlBWL = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=W%C3%9C+25%2F26&group=4175";
            string urlKreisklasseC2 = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Karlsruhe+25%2F26&group=4605";

            var teams = NuLigaParser.ParseTeams(urlLandesLiga2);

            foreach (var team in teams)
            {
                Console.WriteLine(team);
            }

            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //var fileName = Path.Combine(path, "TableLandesLigaNord2.json");
            //var jsonString = JsonSerializer.Serialize(teams, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync(fileName, jsonString);

            var fileNameHtml = Path.Combine(path, "TableLandesLigaNord2.txt"); // TableLandesLigaNord2 // TableKreisklasseA
            var text = HtmlTableWriter.StartTable() + HtmlTableWriter.GenerateTableHeader() + HtmlTableWriter.GenerateBody(teams) + HtmlTableWriter.EndTable();
            File.WriteAllText(fileNameHtml, text);

            return 0;
        }

        static async Task<string> GetHTMLAsync(string url)
        {
            using HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, url);
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new Exception($"Request to {url} failed.");
        }
    }
}