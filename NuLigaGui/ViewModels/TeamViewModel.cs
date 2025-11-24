using NuLigaCore.Data;
using System.ComponentModel;
using System.Data;

namespace NuLigaGui.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly Team _team;
        private DataTable? _playersTable;

        public TeamViewModel(Team team)
        {
            _team = team ?? throw new ArgumentNullException(nameof(team));
            BuildPlayersTable();
        }

        public int Rank => _team.Rang;
        public string Name => _team.Name;
        public int Games => _team.Spiele;
        public int Points => _team.Punkte;
        public double BoardPointsSum => _team.BP;
        public double AverageDwz => _team.DWZ;
        public double BerlinTieBreak => _team.BW;
        public DataView? PlayersTable => _playersTable?.DefaultView;

        public IEnumerable<Player> Players => _team.TeamPlayers ?? Enumerable.Empty<Player>();

        public bool ContainsGameDay(GameDay gameDay) =>
            _team.GameDays != null && _team.GameDays.Contains(gameDay);

        // Build DataTable with dynamic round columns (R1, R2, ...)
        private void BuildPlayersTable()
        {
            _playersTable = new DataTable();

            // fixed columns
            _playersTable.Columns.Add("Brett", typeof(int));
            _playersTable.Columns.Add("Spieler", typeof(string));
            _playersTable.Columns.Add("DWZ", typeof(int));

            // determine number of rounds from team.GameDays or first player's PointsPerGameDay
            var rounds = _team.GameDays?.Count
                         ?? _team.TeamPlayers?.FirstOrDefault()?.PointsPerGameDay?.Length
                         ?? 0;

            for (var r = 1; r <= rounds; r++)
            {
                _playersTable.Columns.Add($"{r}", typeof(string));
            }

            _playersTable.Columns.Add("Total", typeof(string));

            PopulatePlayersRows();
        }

        private void PopulatePlayersRows()
        {
            if (_playersTable == null) return;
            _playersTable.Rows.Clear();

            var rounds = _playersTable.Columns.Count - 4; // first 4 fixed columns

            foreach (var p in _team.TeamPlayers ?? Enumerable.Empty<Player>())
            {
                var row = _playersTable.NewRow();
                row["Brett"] = p.Brett;
                row["Spieler"] = p.Name ?? string.Empty;
                row["DWZ"] = p.DWZ;

                var totalPoints = 0.0;
                var totalGames = 0;
                for (var i = 0; i < rounds; i++)
                {
                    var value = "-";
                    if (p.PointsPerGameDay != null && i < p.PointsPerGameDay.Length)
                    {
                        var points = p.PointsPerGameDay[i];
                        value = (points < 0) ? "-" : points.ToString();
                        if (points >= 0)
                        {
                            totalPoints += points;
                            totalGames++;
                        }
                    }
                    row[$"{i + 1}"] = value;
                }

                row["Total"] = totalPoints.ToString() + "/" + totalGames.ToString();

                _playersTable.Rows.Add(row);
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Rank));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Games));
            OnPropertyChanged(nameof(Points));
            OnPropertyChanged(nameof(BoardPointsSum));
            OnPropertyChanged(nameof(AverageDwz));
            OnPropertyChanged(nameof(BerlinTieBreak));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}