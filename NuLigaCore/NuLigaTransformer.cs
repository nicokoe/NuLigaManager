using NuLigaCore.Data;

namespace NuLigaCore
{
    public static class NuLigaTransformer
    {
        public static List<GameDay> TransformTeamsToGameDayReport(List<Team> teams)
        {
            if (teams.Count < 1)
            {
                return [];
            }

            var currentGameDay = teams[0].GameDays?.Last(gd => gd.ReportUrl != null)?.Date;
            if (currentGameDay == null)
            {
                return [];
            }

            var currentGameDayReport = new List<GameDay>();
            foreach (var team in teams)
            {
                var gameDay = team.GameDays?.FirstOrDefault(gd => gd.Date == currentGameDay);
                if (gameDay != null && !currentGameDayReport.Any(gd => gd.HomeTeam == gameDay.HomeTeam && gd.GuestTeam == gameDay.GuestTeam))
                {
                    currentGameDayReport.Add(gameDay);
                }
            }

            return currentGameDayReport;
        }
    }
}
