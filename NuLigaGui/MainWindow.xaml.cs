using NuLigaCore;
using System.Windows;
using System.Windows.Controls;

namespace NuLigaGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var leagues = NuLigaParser.ParseLeagues();
            DataContext = new ViewModels.LeaguesViewModel(leagues);
        }

        private void PlayersDataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var header = e.Column.Header?.ToString() ?? string.Empty;
            switch (header)
            {
                case "Spieler":
                case "Name":
                    e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;
                case "Brett":
                    e.Column.Width = new DataGridLength(40);
                    break;
                case "DWZ":
                case "Total":
                    e.Column.Width = new DataGridLength(60);
                    break;
                default:
                    // round columns R1, R2 ... keep compact
                    e.Column.Width = new DataGridLength(40);
                    break;
            }
        }
    }
}