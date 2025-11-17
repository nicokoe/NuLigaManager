using HtmlAgilityPack;
using ScrapeNuLigaChess.Data;
using System.Text.Json;

namespace NuLigaScraper
{
    class Program
    {
        private static readonly string urlRoot = "https://bsv-schach.liga.nu/";

        static async Task<int> Main(string[] args)
        {
            //var badenUrl = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/leaguePage?championship=Baden+25%2F26";
            //var karlsruheUrl = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/leaguePage?championship=Karlsruhe+25%2F26";
            //string urlLandesLiga2 = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Baden+25%2F26&group=4610";
            string urlKreisklasseA = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Karlsruhe+25%2F26&group=4646";

            var web = new HtmlWeb();
            var htmlDoc = web.Load(urlKreisklasseA);

            var teams = ParseTeams(web, htmlDoc);

            foreach (var team in teams)
            {
                Console.WriteLine(team);
            }

            //var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //var fileName = Path.Combine(path, "TableLandesLigaNord2.json");
            //var jsonString = JsonSerializer.Serialize(teams, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync(fileName, jsonString);

            //var fileNameHtml = Path.Combine(path, "TableKreisklasseA.txt");
            //var text = HtmlTableWriter.StartTable() + HtmlTableWriter.GenerateTableHeader() + HtmlTableWriter.GenerateBody(teams) + HtmlTableWriter.EndTable();
            //File.WriteAllText(fileNameHtml, text);

            return 0;
        }

        private static List<Team> ParseTeams(HtmlWeb web, HtmlDocument doc)
        {
            var crossTableList = doc.DocumentNode.SelectNodes("//table[@class='cross-table']");
            if (crossTableList == null || crossTableList.Count < 1)
            {
                return [];
            }

            var teams = new List<Team>();
            var rankingTable = crossTableList[0];
            var rows = rankingTable.SelectNodes("tr");

            // start with 1, skip headers in 0
            for (var row = 1; row < rows.Count; row++)
            {
                var cells = rows[row].SelectNodes("th|td");
                var teamLink = cells[2].QuerySelector("a").Attributes["href"].Value;
                var teamDetails = web.Load(urlRoot + teamLink);

                var newTeam = new Team
                {
                    Rank = int.Parse(cells[1].InnerText),
                    Name = cells[2].InnerText,
                    TeamPlayers = ParsePlayers(teamDetails),
                    Games = int.Parse(cells[13].InnerText),
                    Points = int.Parse(cells[14].InnerText),
                    BoardPointsSum = double.Parse(cells[15].InnerText)
                };

                var rankIndex = 0;
                for (var i = 0; i < 10; i++)
                {
                    if (row == i + 1)
                    {
                        continue;
                    }
                    var value = string.IsNullOrEmpty(cells[3 + i].InnerText) ? "0" : cells[3 + i].InnerText;
                    newTeam.BoardPointsPerRank[rankIndex] = double.Parse(value);
                    rankIndex++;
                }
                teams.Add(newTeam);
            }
            return teams;
        }

        private static List<Player>? ParsePlayers(HtmlDocument doc)
        {
            var resultSetList = doc.DocumentNode.SelectNodes("//table[@class='result-set']");
            if (resultSetList == null || resultSetList.Count < 3)
            {
                return null;
            }

            var players = new List<Player>();
            var playerTable = resultSetList[2];

            var rows = playerTable.SelectNodes("tr");
            for (var row = 0; row < rows.Count; row++)
            {
                var cells = rows[row].SelectNodes("th|td");
                if (cells.Count < 6 || cells[0].InnerText == "Brett" || int.Parse(cells[4].InnerText) < 1)
                {
                    continue;
                }

                var player = new Player
                {
                    BoardNumber = int.Parse(cells[0].InnerText),
                    Name = cells[1].InnerText.Trim().TrimStart('\n').TrimEnd('\n').Trim(),
                    DWZ = int.Parse(string.IsNullOrEmpty(cells[3].InnerText) ? "1000" : cells[3].InnerText),
                    Games = int.Parse(cells[4].InnerText),
                    BoardPoints = cells[5].InnerText
                };
                players.Add(player);
            }

            return players;
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