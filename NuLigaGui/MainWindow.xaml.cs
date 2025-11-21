using NuLigaCore;
using System.Windows;

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
    }
}