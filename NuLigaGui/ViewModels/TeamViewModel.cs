using CsvHelper;
using NuLigaCore.Data;
using NuLigaGui.Utilities;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace NuLigaGui.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly Team _team;
        private DataTable? _playersTable;
        private readonly RelayCommand<TeamViewModel?> _exportTeamJsonCommand;
        private readonly RelayCommand<TeamViewModel?> _exportTeamCsvCommand;
        public ICommand ExportSelectedTeamJsonCommand => _exportTeamJsonCommand;
        public ICommand ExportSelectedTeamCsvCommand => _exportTeamCsvCommand;

        public TeamViewModel(Team team)
        {
            _team = team ?? throw new ArgumentNullException(nameof(team));
            BuildPlayersTable();

            _exportTeamJsonCommand = new RelayCommand<TeamViewModel?>(ExportSelectedTeamJsonAsync, l => l != null);
            _exportTeamCsvCommand = new RelayCommand<TeamViewModel?>(ExportSelectedTeamCsvAsync, l => l != null);
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
        public IList<GameDay>? GameDays => _team.GameDays;

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
                         ?? _team.TeamPlayers?.FirstOrDefault()?.PunkteProSpieltag?.Length
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
                    var pointsString = "-";
                    if (p.PunkteProSpieltag != null && i < p.PunkteProSpieltag.Length)
                    {
                        var points = p.PunkteProSpieltag[i];
                        pointsString = points == -1 ? "-" : (points == 1000 ? "+" : points.ToString());
                        if (points >= 0)
                        {
                            totalPoints += (points == 1000 ? 1 : points);
                            totalGames++;
                        }
                    }
                    row[$"{i + 1}"] = pointsString;
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

            PopulatePlayersRows();
        }

        public async Task ExportSelectedTeamJsonAsync(TeamViewModel? team)
        {
            if (team == null) return;

            var json = JsonSerializer.Serialize(team.Players, new JsonSerializerOptions { WriteIndented = true });

            var defaultFileName = string.IsNullOrWhiteSpace(team.Name) ? "team" : $"{team.Name.ToValidFileName()}.json";
            var path = DialogExtensions.GetPathWithSaveFileDialog(defaultFileName, "JSON files (*.json)|*.json|All files (*.*)|*.*", "json");

            if (string.IsNullOrWhiteSpace(path)) return;

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task ExportSelectedTeamCsvAsync(TeamViewModel? team)
        {
            if (team == null) return;

            var defaultFileName = string.IsNullOrWhiteSpace(team.Name) ? "team" : $"{team.Name.ToValidFileName()}.csv";
            var path = DialogExtensions.GetPathWithSaveFileDialog(defaultFileName, "CSV files (*.csv)|*.csv|All files (*.*)|*.*", "csv");

            if (string.IsNullOrWhiteSpace(path)) return;

            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<PlayerMap>();
                    await csv.WriteRecordsAsync(team.Players).ConfigureAwait(false);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}