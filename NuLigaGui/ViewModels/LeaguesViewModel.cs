using HtmlAgilityPack;
using NuLigaCore;
using NuLigaCore.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace NuLigaGui.ViewModels
{
    public class LeaguesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<League> Leagues { get; }

        public ObservableCollection<TeamViewModel> Teams { get; } = new();

        private readonly Dictionary<string, List<Team>> _teamsCache = new();
        private readonly object _cacheLock = new();

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

        private async Task LoadTeamsAsync(League? league)
        {
            if (league == null || string.IsNullOrWhiteSpace(league.Url))
            {
                return;
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
                return;
            }

            try
            {
                IsLoading = true;

                var teams = await Task.Run(() =>
                {
                    var web = new HtmlWeb();
                    var doc = web.Load(league.Url!);
                    return NuLigaParser.ParseTeams(web, doc) ?? new List<Team>();
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
            }
            catch (Exception)
            {
                // Consider logging or surface error via another property/command.
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}