using HtmlAgilityPack;

namespace NuLigaScraper
{

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // var web = new HtmlWeb();
            // var document = web.Load("https://bsv-schach.liga.nu/");

            // // selecting all HTML product elements from the current page 
            // var productHTMLElements = document.DocumentNode.QuerySelectorAll("table cross-table");

            string url = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/groupPage?championship=Baden+25%2F26&group=4610";
            string html = await GetHTMLAsync(url);
            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(html);
            HtmlNodeCollection items = htmlDoc.DocumentNode.SelectNodes("//table[@class='cross-table']");

            var teams = new List<Team>();
            foreach (HtmlNode table in items)
            {
                var rows = table.SelectNodes("tr");

                // start with 1, skip headers in 0
                for (var row = 1; row < rows.Count; row++)
                {
                    var newTeam = new Team();
                    var cells = rows[row].SelectNodes("th|td");

                    newTeam.Rank = int.Parse(cells[1].InnerText);
                    newTeam.Name = cells[2].InnerText;
                    newTeam.Games = int.Parse(cells[13].InnerText);
                    newTeam.Points = int.Parse(cells[14].InnerText);
                    newTeam.BoardPointsSum = double.Parse(cells[15].InnerText);
                    teams.Add(newTeam);
                }
            }

            foreach(var team in teams)
            {
                Console.WriteLine(team);
            }
            // table class = "cross-table"
            Console.WriteLine("Hello, World!");

            // scraping logic... 
            return 0;
        }

        static async Task<string> GetHTMLAsync(string url)
        {
            using HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, url);
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            throw new Exception($"Request to {url} failed.");
        }
    }
}