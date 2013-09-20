using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Zeroconf;

namespace ZeroconfTest.NetFx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            //Action<IZeroconfRecord> onMessage = record => Console.WriteLine("On Message: {0}", record);


            var domains = await ZeroconfResolver.BrowseDomainsAsync();
            
            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));
            // var responses = await ZeroconfResolver.ResolveAsync("_http._tcp.local.");
            
            foreach (var resp in responses)
                Console.WriteLine(resp);
        }

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var responses = await ZeroconfResolver.BrowseDomainsAsync();
            
            foreach (var service in responses)
            {
                Console.WriteLine(service.Key);

                foreach (var host in service)
                    Console.WriteLine("\tIP: " + host);

            }
        }
    }
}
