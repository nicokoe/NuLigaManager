using CsvHelper;
using Microsoft.Win32;
using NuLigaCore;
using NuLigaCore.Data;
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
        public ObservableCollection<Player> SelectedTeamPlayers { get; } = new();

        private readonly Dictionary<string, List<Team>> _teamsCache = new();
        private readonly object _cacheLock = new();

        private readonly RelayCommand _copyCommand;
        private readonly RelayCommand<League?> _exportJsonCommand;
        private readonly RelayCommand<League?> _exportCsvCommand;
        public ICommand CopySelectedLeagueCommand => _copyCommand;
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
                    _exportJsonCommand.RaiseCanExecuteChanged();
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
                    UpdateSelectedTeamPlayers();
                }
            }
        }

        private void UpdateSelectedTeamPlayers()
        {
            SelectedTeamPlayers.Clear();
            if (SelectedTeamView == null) return;

            foreach (var p in SelectedTeamView.Players)
            {
                SelectedTeamPlayers.Add(p);
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
            _exportJsonCommand = new RelayCommand<League?>(ExportSelectedLeagueJsonAsync, l => l != null);
            _exportCsvCommand = new RelayCommand<League?>(ExportSelectedLeagueCsvAsync, l => l != null);

            NuLigaParser.GameDayReportLoaded += NuLigaParser_GameDayReportLoaded;
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

                    if (SelectedTeamView == vm)
                    {
                        UpdateSelectedTeamPlayers();
                    }
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Teams.Clear();
                    foreach (var vm in ToViewModels(cached))
                    {
                        Teams.Add(vm);
                    }
                    SelectedTeamView = null;
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Teams.Clear();
                    foreach (var vm in ToViewModels(teams))
                    {
                        Teams.Add(vm);
                    }
                    SelectedTeamView = null;
                });

                return teams;
            }
            catch (Exception)
            {
                // Consider logging or surface error via another property/command.
                return new List<Team>();
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
                       + HtmlTableWriter.GenerateTableHeader()
                       + HtmlTableWriter.GenerateBody(teams)
                       + HtmlTableWriter.EndTable();
            
            // Copy to clipboard on UI thread
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

            string? path = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new SaveFileDialog
                {
                    FileName = string.IsNullOrWhiteSpace(league.Name) ? "teams" : $"{SanitizeFileName(league.Name)}.json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    AddExtension = true
                };

                if (dlg.ShowDialog() == true)
                {
                    path = dlg.FileName;
                }
            });

            if (string.IsNullOrWhiteSpace(path)) return;

            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task ExportSelectedLeagueCsvAsync(League? league)
        {
            if (league == null) return;

            var teams = await LoadTeamsAsync(league).ConfigureAwait(false);

            string? path = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new SaveFileDialog
                {
                    FileName = string.IsNullOrWhiteSpace(league.Name) ? "teams" : $"{SanitizeFileName(league.Name)}.csv",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    AddExtension = true
                };

                if (dlg.ShowDialog() == true)
                {
                    path = dlg.FileName;
                }
            });

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

        private static string SanitizeFileName(string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input.Replace(' ', '_');
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}