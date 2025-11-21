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

        public ObservableCollection<Team> Teams { get; } = new();

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
                    foreach (var t in cached)
                    {
                        Teams.Add(t);
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

                // Cache the parsed teams
                lock (_cacheLock)
                {
                    _teamsCache[key] = teams;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Teams.Clear();
                    foreach (var t in teams)
                    {
                        Teams.Add(t);
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