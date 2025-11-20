using HtmlAgilityPack;
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

            var web = new HtmlWeb();
            var leagues = NuLigaParser.ParseLeagues(web);

            DataContext = new ViewModels.LeaguesViewModel(leagues);
        }
    }
}