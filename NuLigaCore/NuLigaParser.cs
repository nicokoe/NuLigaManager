using HtmlAgilityPack;
using NuLigaCore.Data;

namespace NuLigaCore
{
    public static class NuLigaParser
    {
        private static readonly string urlRoot = "https://bsv-schach.liga.nu/";

        public static List<League> ParseLeagues(HtmlWeb web)
        {
            var badenUrl = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/leaguePage?championship=Baden+25%2F26";
            var karlsruheUrl = "https://bsv-schach.liga.nu/cgi-bin/WebObjects/nuLigaSCHACHDE.woa/wa/leaguePage?championship=Karlsruhe+25%2F26";

            var leagues = ParseLeaguesFromUrl(web, badenUrl);
            leagues.AddRange(ParseLeaguesFromUrl(web, karlsruheUrl));

            return leagues;
        }

        private static List<League> ParseLeaguesFromUrl(HtmlWeb web, string url)
        {
            var doc = web.Load(url);
            var crossTableList = doc.DocumentNode.SelectNodes("//table[@class='matrix']");
            if (crossTableList == null || crossTableList.Count < 1)
            {
                return [];
            }

            var leagueList = crossTableList[0];
            var leagues = new List<League>();
            var rows = leagueList.SelectNodes(".//a[starts-with(@href, '/cgi')]");
            for (var row = 0; row < rows.Count; row++)
            {
                var league = new League
                {
                    Name = rows[row].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' '),
                    Url = urlRoot + rows[row].Attributes["href"].Value.TrimStart('/').Replace("amp;", ""),
                };
                leagues.Add(league);
            }

            return leagues;
        }

        public static List<Team> ParseTeams(HtmlWeb web, HtmlDocument doc)
        {
            var crossTableList = doc.DocumentNode.SelectNodes("//table[@class='cross-table']");
            if (crossTableList == null || crossTableList.Count < 1)
            {
                return [];
            }

            var teams = new List<Team>();
            var rankingTable = crossTableList[0];
            var rows = rankingTable.SelectNodes("tr");
            var numberOfTeams = rows.Count - 1; // BW Liga has 12 teams, others 10, KKC has 8 (+ header)

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
                    GameDays = ParseGameDays(web, teamDetails),
                    Games = int.Parse(cells[numberOfTeams + 3].InnerText),
                    Points = int.Parse(cells[numberOfTeams + 4].InnerText),
                    BoardPointsSum = double.Parse(cells[numberOfTeams + 5].InnerText),
                    BoardPointsPerRank = new double[numberOfTeams - 1]
                };

                var rankIndex = 0;
                for (var i = 0; i < numberOfTeams; i++)
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

        public static List<GameDay>? ParseGameDays(HtmlWeb web, HtmlDocument doc)
        {
            var resultSetList = doc.DocumentNode.SelectNodes("//table[@class='result-set']");
            if (resultSetList == null || resultSetList.Count < 3)
            {
                return null;
            }

            var gameDays = new List<GameDay>();
            var gameDayTable = resultSetList[1];

            var rows = gameDayTable.SelectNodes("tr");
            for (var row = 1; row < rows.Count; row++)
            {
                var cells = rows[row].SelectNodes("th|td");
                var date = cells[1].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');
                var round = cells[4].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');
                var homeTeam = cells[6].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');
                var guestTeam = cells[7].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ').Replace("&nbsp;", "");
                var boardPoints = cells[8].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');

                var gameDay = new GameDay
                {
                    Date = DateTime.Parse(date),
                    Round = int.Parse(round),
                    HomeTeam = homeTeam,
                    GuestTeam = guestTeam,
                    BoardPoints = boardPoints
                };

                var gameReportLink = cells[8].QuerySelector("a")?.Attributes["href"].Value.TrimStart('/').Replace("amp;", "");
                var gameReport = web.Load(urlRoot + gameReportLink);
                gameDay.Report = ParseGameReport(gameReport);

                gameDays.Add(gameDay);
            }

            return gameDays;
        }

        public static GameReport? ParseGameReport(HtmlDocument doc)
        {
            var resultSetList = doc.DocumentNode.SelectNodes("//table[@class='result-set']");
            if (resultSetList == null || resultSetList.Count < 0)
            {
                return null;
            }

            var pairings = new List<Pairing>();
            var gameReportTable = resultSetList[0];

            var rows = gameReportTable.SelectNodes("tr");
            for (var row = 1; row < rows.Count; row++)
            {
                var cells = rows[row].SelectNodes("th|td");
                if (cells.Count < 6)
                {
                    continue;
                }
                var homePlayerDWZ = cells[2].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');
                var guestPlayerDWZ = cells[4].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ');

                var pairing = new Pairing
                {
                    BoardNumber = int.Parse(cells[0].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ')),
                    HomePlayer = cells[1].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' '),
                    HomePlayerDWZ = int.Parse(string.IsNullOrEmpty(homePlayerDWZ) ? "1000" : homePlayerDWZ),
                    GuestPlayer = cells[3].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' '),
                    GuestPlayerDWZ = int.Parse(string.IsNullOrEmpty(guestPlayerDWZ) ? "1000" : guestPlayerDWZ),
                    BoardPoints = cells[5].InnerText.TrimStart('\n', '\t', ' ').TrimEnd('\n', '\t', ' ').AsBoardPoints()
                };
                pairings.Add(pairing);
            }

            return new GameReport { Pairings = pairings };
        }

        public static List<Player>? ParsePlayers(HtmlDocument doc)
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
    }
}
