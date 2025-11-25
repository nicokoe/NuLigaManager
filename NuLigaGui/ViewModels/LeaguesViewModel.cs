using CsvHelper;
using NuLigaCore;
using NuLigaCore.Data;
using NuLigaGui.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace NuLigaGui.ViewModels
{
    public class LeaguesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<League> Leagues { get; }

        public ObservableCollection<TeamViewModel> Teams { get; } = new();
        public ObservableCollection<GameDay> GameDays { get; } = new();

        private readonly Dictionary<string, List<Team>> _teamsCache = new();
        private readonly object _cacheLock = new();

        private readonly RelayCommand _copyCommand;
        private readonly RelayCommand _copyTeamCommand;
        private readonly RelayCommand<League?> _exportJsonCommand;
        private readonly RelayCommand<League?> _exportCsvCommand;
        public ICommand CopySelectedLeagueCommand => _copyCommand;
        public ICommand CopySelectedTeamCommand => _copyTeamCommand;
        public ICommand ExportSelectedLeagueJsonCommand => _exportJsonCommand;
        public ICommand ExportSelectedLeagueCsvCommand => _exportCsvCommand;

        private League? _selectedLeague;
        public League? SelectedLeague
        {
            get => _selectedLeague;
            set
            {
                if (_selectedLeague != value)
                {
                    _selectedLeague = value;
                    OnPropertyChanged(nameof(SelectedLeague));

                    _ = LoadTeamsAsync(_selectedLeague);
                    _copyCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private TeamViewModel? _selectedTeamView;
        public TeamViewModel? SelectedTeamView
        {
            get => _selectedTeamView;
            set
            {
                if (_selectedTeamView != value)
                {
                    _selectedTeamView = value;
                    OnPropertyChanged(nameof(SelectedTeamView));

                    _copyCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public LeaguesViewModel(IEnumerable<League> leagues)
        {
            Leagues = new ObservableCollection<League>(leagues.Where(l => l is not null).ToList());

            _copyCommand = new RelayCommand(CopySelectedLeagueAsync, () => SelectedLeague != null);
            _copyTeamCommand = new RelayCommand(CopySelectedTeamPlayersAsync, () => SelectedTeamView != null);
            _exportJsonCommand = new RelayCommand<League?>(ExportSelectedLeagueJsonAsync, l => l != null);
            _exportCsvCommand = new RelayCommand<League?>(ExportSelectedLeagueCsvAsync, l => l != null);

            NuLigaParser.GameDayReportLoadedForGui += NuLigaParser_GameDayReportLoaded;
        }

        private void NuLigaParser_GameDayReportLoaded(GameDay gameDay)
        {
            if (Teams.Count == 0)
            {
                return; 
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = Teams.FirstOrDefault(t => t.ContainsGameDay(gameDay));
                if (vm != null)
                {
                    vm.Refresh();
                }
            });
        }

        private IEnumerable<TeamViewModel> ToViewModels(IEnumerable<Team> teams)
        {
            return teams.Select(t => new TeamViewModel(t));
        }

        private async Task<List<Team>> LoadTeamsAsync(League? league)
        {
            if (league == null || string.IsNullOrWhiteSpace(league.Url))
            {
                return new List<Team>();
            }

            var key = league.Url!.Trim();

            List<Team>? cached;
            lock (_cacheLock)
            {
                _teamsCache.TryGetValue(key, out cached);
            }

            if (cached is not null)
            {
                var lastGameDayReport = NuLigaTransformer.TransformTeamsToGameDayReport(cached);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Teams.Clear();
                    foreach (var vm in ToViewModels(cached))
                    {
                        Teams.Add(vm);
                    }
                    SelectedTeamView = null;

                    GameDays.Clear();
                    foreach (var gd in lastGameDayReport)
                    {
                        GameDays.Add(gd);
                    }
                });

                return cached;
            }

            try
            {
                IsLoading = true;

                var teams = await Task.Run(() =>
                {
                    return NuLigaParser.ParseTeams(league.Url) ?? new List<Team>();
                });

                lock (_cacheLock)
                {
                    _teamsCache[key] = teams;
                }

                var lastGameDayReport = NuLigaTransformer.TransformTeamsToGameDayReport(teams);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Teams.Clear();
                    foreach (var vm in ToViewModels(teams))
                    {
                        Teams.Add(vm);
                    }
                    SelectedTeamView = null;

                    GameDays.Clear();
                    foreach (var gd in lastGameDayReport)
                    {
                        GameDays.Add(gd);
                    }
                });

                return teams;
            }
            catch (Exception)
            {
                // Consider logging or surface error via another property/command.
                return [];
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CopySelectedLeagueAsync()
        {
            var league = SelectedLeague;
            if (league == null) return;

            // Ensure teams are loaded (returns cached if present)
            var teams = await LoadTeamsAsync(league).ConfigureAwait(false);

            var html = HtmlTableWriter.StartTable()
                       + HtmlTableWriter.GenerateLeagueTableHeader()
                       + HtmlTableWriter.GenerateTeamsBody(teams)
                       + HtmlTableWriter.EndTable();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var data = new DataObject();
                data.SetData(DataFormats.Html, html);
                data.SetData(DataFormats.Text, html);
                Clipboard.SetDataObject(data, true);
            });
        }

        public async Task CopySelectedTeamPlayersAsync()
        {
            var teamView = SelectedTeamView;
            if (teamView == null) return;

            var rounds = teamView.GameDays?.Count
                         ?? teamView.Players.FirstOrDefault()?.PunkteProSpieltag?.Length
                         ?? 0;

            var html = HtmlTableWriter.StartTable()
                       + HtmlTableWriter.GeneratePlayerTableHeader(rounds)
                       + HtmlTableWriter.GeneratePlayersBody(teamView.Players)
                       + HtmlTableWriter.EndTable();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var data = new DataObject();
                data.SetData(DataFormats.Html, html);
                data.SetData(DataFormats.Text, html);
                Clipboard.SetDataObject(data, true);
            });
        }

        public async Task ExportSelectedLeagueJsonAsync(League? league)
        {
            if (league == null) return;

            var teams = await LoadTeamsAsync(league).ConfigureAwait(false);
            var json = JsonSerializer.Serialize(teams, new JsonSerializerOptions { WriteIndented = true });

            var defaultFileName = string.IsNullOrWhiteSpace(league.Name) ? "teams" : $"{league.Name.ToValidFileName()}.json";
            var path = DialogExtensions.GetPathWithSaveFileDialog(defaultFileName, "JSON files (*.json)|*.json|All files (*.*)|*.*", "json");

            if (string.IsNullOrWhiteSpace(path)) return;

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task ExportSelectedLeagueCsvAsync(League? league)
        {
            if (league == null) return;

            var teams = await LoadTeamsAsync(league).ConfigureAwait(false);

            var defaultFileName = string.IsNullOrWhiteSpace(league.Name) ? "teams" : $"{league.Name.ToValidFileName()}.csv";
            var path = DialogExtensions.GetPathWithSaveFileDialog(defaultFileName, "CSV files (*.csv)|*.csv|All files (*.*)|*.*", "csv");

            if (string.IsNullOrWhiteSpace(path)) return;

            using (var writer = new StreamWriter(path))
            {
                using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<TeamMap>();
                    await csv.WriteRecordsAsync(teams).ConfigureAwait(false);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}