using NuLigaCore.Data;
using System.ComponentModel;

namespace NuLigaGui.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly Team _team;

        public TeamViewModel(Team team)
        {
            _team = team;
        }

        public int Rank => _team.Rang;
        public string Name => _team.Name;
        public int Games => _team.Spiele;
        public int Points => _team.Punkte;
        public double BoardPointsSum => _team.BP;
        public double AverageDwz => _team.DWZ;
        public double BerlinTieBreak => _team.BW;

        public bool ContainsGameDay(GameDay gameDay) =>
            _team.GameDays != null && _team.GameDays.Contains(gameDay);

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