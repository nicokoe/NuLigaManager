using HtmlAgilityPack;
using NuLigaCore.Data;

namespace NuLigaCore
{
    public static class NuLigaParser
    {
        private static readonly string urlRoot = "https://bsv-schach.liga.nu/";

        public static event Action<GameDay>? GameDayReportLoaded;

        private static readonly HtmlWeb web = new();

        public static List<League> ParseLeagues()
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

        public static List<Team> ParseTeams(string url)
        {
            var doc = web.Load(url);
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
                var teamUrl = cells[2].QuerySelector("a").Attributes["href"].Value;

                var newTeam = new Team
                {
                    Rang = int.Parse(cells[1].InnerText),
                    Name = cells[2].InnerText,
                    TeamUrl = string.IsNullOrEmpty(teamUrl) ? null : urlRoot + teamUrl,
                    Spiele = int.Parse(cells[numberOfTeams + 3].InnerText),
                    Punkte = int.Parse(cells[numberOfTeams + 4].InnerText),
                    BP = double.Parse(cells[numberOfTeams + 5].InnerText),
                    BoardPointsPerRank = new double[numberOfTeams - 1]
                };

                if (newTeam.TeamUrl != null)
                {
                    var teamDoc = web.Load(newTeam.TeamUrl);
                    newTeam.TeamPlayers = ParsePlayers(teamDoc);
                    newTeam.GameDays = ParseGameDays(teamDoc);
                }

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

        public static List<GameDay>? ParseGameDays(HtmlDocument doc)
        {
            var resultSetList = doc.DocumentNode.SelectNodes("//table[@class='result-set']");
            if (resultSetList == null || resultSetList.Count < 2)
            {
                var errorReason = resultSetList == null ? "resultSetList is null" : "resultSetList's count < 2";
                System.Diagnostics.Debug.WriteLine($"Error in loaded data for game days: {errorReason}");
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
                var reportUrl = cells[8].QuerySelector("a")?.Attributes["href"].Value.TrimStart('/').Replace("amp;", "");

                var gameDay = new GameDay
                {
                    Date = DateTime.Parse(date),
                    Round = int.Parse(round),
                    HomeTeam = homeTeam,
                    GuestTeam = guestTeam,
                    BoardPoints = boardPoints,
                    ReportUrl = string.IsNullOrEmpty(reportUrl) ? null : urlRoot + reportUrl
                };

                gameDays.Add(gameDay);

                _ = LoadGameReportAsync(gameDay);
            }

            return gameDays;
        }

        private static async Task LoadGameReportAsync(GameDay? gameDay)
        {
            if (gameDay == null || string.IsNullOrWhiteSpace(gameDay.ReportUrl))
            {
                return;
            }

            await Task.Run(() =>
            {
                var web = new HtmlWeb();
                var doc = web.Load(gameDay.ReportUrl);
                gameDay.Report = ParseGameReport(doc);

                GameDayReportLoaded?.Invoke(gameDay);
            });
        }

        public static GameReport? ParseGameReport(HtmlDocument doc)
        {
            var resultSetList = doc.DocumentNode.SelectNodes("//table[@class='result-set']");
            if (resultSetList == null || resultSetList.Count < 1)
            {
                var errorReason = resultSetList == null ? "resultSetList is null" : "resultSetList's count < 1";
                System.Diagnostics.Debug.WriteLine($"Error in loaded data for the game report: {errorReason}");
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
                var errorReason = resultSetList == null ? "resultSetList is null" : "resultSetList's count < 3";
                System.Diagnostics.Debug.WriteLine($"Error in loaded data for the players: {errorReason}");
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
