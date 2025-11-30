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

            var currentGameDay = teams[0].GameDays?.Last(gd => gd.ReportUrl != null)?.Datum;
            if (currentGameDay == null)
            {
                return [];
            }

            var currentGameDayReport = new List<GameDay>();
            foreach (var team in teams)
            {
                var gameDay = team.GameDays?.FirstOrDefault(gd => gd.Datum == currentGameDay);
                if (gameDay != null && !currentGameDayReport.Any(gd => gd.HeimMannschaft == gameDay.HeimMannschaft && gd.GastMannschaft == gameDay.GastMannschaft))
                {
                    currentGameDayReport.Add(gameDay);
                }
            }

            return currentGameDayReport;
        }
    }
}
