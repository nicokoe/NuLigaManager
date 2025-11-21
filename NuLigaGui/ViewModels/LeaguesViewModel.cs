using HtmlAgilityPack;
using NuLigaCore;
using NuLigaCore.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace NuLigaGui.ViewModels
{
    public class LeaguesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<League> Leagues { get; }

        public ObservableCollection<TeamViewModel> Teams { get; } = new();

        private readonly Dictionary<string, List<Team>> _teamsCache = new();
        private readonly object _cacheLock = new();

        private readonly RelayCommand _copyCommand;
        public ICommand CopySelectedLeagueCommand => _copyCommand;

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
                Teams.FirstOrDefault(t => t.ContainsGameDay(gameDay))?.Refresh();
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}