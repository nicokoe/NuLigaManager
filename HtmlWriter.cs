using ScrapeNuLigaChess.Data;
using System.Text;

namespace NuLigaScraper
{
    public class HtmlTableWriter
    {
        public static string StartTable()
        {
            return "<figure class=\"styled-table\"><table>";
        }

        public static string GenerateTableHeader()
        {
            return $"<thead><tr><td width=\"7%\">Rang</td><td>Mannschaft</td><td>DWZ</td><td>Spiele</td><td>Punkte</td><td>BP</td><td>BW</td></tr></thead>";
        }

        public static string GenerateBody(List<Team> teams)
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

        public static string GenerateTeamHtmlTableRow(Team team, string style)
        {
            var averageDwz = team.TeamPlayers?.Where(player => player.Games > 0).Average(x => x.DWZ) ?? 0;
            return $"<tr{style}><td>{team.Rank}</td><td>{team.Name}</td><td>{Math.Round(averageDwz)}</td><td>{team.Games}</td><td>{team.Points}</td><td>{team.BoardPointsSum}</td><td>0</td></tr>";
        }

        public static string EndTable()
        {
            return "</table></figure>";
        }
    }
}
