using NuLigaCore.Data;
using System.Text;

namespace NuLigaCore
{
    public class HtmlTableWriter
    {
        public static string StartTable()
        {
            return "<figure class=\"styled-table\"><table>";
        }

        public static string GenerateLeagueTableHeader()
        {
            return $"<thead><tr><td width=\"7%\">Rang</td><td>Mannschaft</td><td>DWZ</td><td>Spiele</td><td>Punkte</td><td>BP</td><td>BW</td></tr></thead>";
        }

        public static string GeneratePlayerTableHeader(int rounds)
        {
            var header = "<thead><tr><td width=\"7%\">Brett</td><td width=\"15%\">Name</td><td>DWZ</td>";
            for (var i = 1; i <= rounds; i++)
            {
                header += $"<td>{i}</td>";
            }
            return header += "<td>Total</td></tr></thead>";
        }

        public static string GenerateTeamsBody(List<Team> teams)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<tbody>");

            var index = 1;
            foreach (var team in teams)
            {
                var style = "";

                if (index == 1)
                {
                    style = team.Name.Contains("Pfinztal") ? " style='color:green;font-weight:bold;'" : " style='color:green;'";
                    stringBuilder.Append(GenerateTeamHtmlTableRow(team, style));
                }
                else if (index == 9 || index == 10)
                {
                    style = team.Name.Contains("Pfinztal") ? " style='color:red;font-weight:bold;'" : " style='color:red;'";
                    stringBuilder.Append(GenerateTeamHtmlTableRow(team, style));
                }
                else
                {
                    if (team.Name.Contains("Pfinztal"))
                    {
                        style = " style='font-weight:bold;'";
                    }
                    stringBuilder.Append(GenerateTeamHtmlTableRow(team, style));
                }
                index++;
            }

            stringBuilder.Append("</tbody>");
            return stringBuilder.ToString();
        }

        public static string GeneratePlayersBody(IEnumerable<Player> players)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<tbody>");

            foreach (var player in players)
            {
                var dwzDisplay = player.DWZ == 1000 ? "-" : player.DWZ.ToString();
                var row = $"<tr><td>{player.Brett}</td><td>{player.Name}</td><td>{dwzDisplay}</td>";

                var totalPoints = 0.0;
                var totalGames = 0;
                foreach (var points in player.PunkteProSpieltag!)
                {
                    if (points >= 0)
                    {
                        totalPoints += (points == 1000 ? 1 : points);
                        totalGames++;
                    }
                    var pointsString = points == -1 ? "-" : (points == 1000 ? "+" : points.ToString());
                    row += $"<td>{pointsString}</td>";
                }
                var total = totalPoints.ToString() + "/" + totalGames.ToString();
                row += $"<td>{total}</td></tr>";

                stringBuilder.Append(row);
            }

            stringBuilder.Append("</tbody>");
            return stringBuilder.ToString();
        }

        private static string GenerateTeamHtmlTableRow(Team team, string style)
        {
            var teamBw = team.ComputeBerlinTieBreakSumOverAllGameDays();
            return $"<tr{style}><td>{team.Rang}</td><td>{team.Name}</td><td>{team.DWZ}</td><td>{team.Spiele}</td><td>{team.Punkte}</td><td>{team.BP}</td><td>{teamBw}</td></tr>";
        }

        public static string EndTable()
        {
            return "</table></figure>";
        }
    }
}
