using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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

        async void Button_Click(object sender, RoutedEventArgs e)
        {

            //Action<IZeroconfRecord> onMessage = record => Console.WriteLogLine("On Message: {0}", record);


            var domains = await ZeroconfResolver.BrowseDomainsAsync();
            
            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));
            // var responses = await ZeroconfResolver.ResolveAsync("_http._tcp.local.");
            
            foreach (var resp in responses)
                WriteLogLine(resp.ToString());
        }

        async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var responses = await ZeroconfResolver.BrowseDomainsAsync();
            
            foreach (var service in responses)
            {
                WriteLogLine(service.Key);

                foreach (var host in service)
                    WriteLogLine("\tIP: " + host);

            }
        }

        private void WriteLogLine(string text, params object[] args)
        {
            if (Log.Dispatcher.CheckAccess())
            {
                Log.AppendText(string.Format(text, args) + "\r\n");
                Log.ScrollToEnd();
            }
            else
            {
                Log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => WriteLogLine(text, args)));
            }
        }

        private void OnAnnouncement(AdapterInformation info, IZeroconfHost host)
        {
            Log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                WriteLogLine("---- Announced on {0} ({1}) ----", info.Name, info.Address);
                WriteLogLine(host.ToString());
            }));
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (m_listenTask != null)
            {
                m_cancellationTokenSource.Cancel();

                try
                {
                    m_listenTask.Wait();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private CancellationTokenSource m_cancellationTokenSource;
        private Task m_listenTask;

        private async void StartStopListener_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListenButton.IsEnabled = false;

                if (m_listenTask != null)
                {
                    m_cancellationTokenSource.Cancel();
                    await m_listenTask;
                    m_cancellationTokenSource.Dispose();
                    m_cancellationTokenSource = null;
                    m_listenTask = null;
                }
                else
                {
                    m_cancellationTokenSource = new CancellationTokenSource();
                    m_listenTask = ZeroconfResolver.ListenForAnnouncementsAsync(OnAnnouncement, m_cancellationTokenSource.Token);
                }
            }
            finally
            {
                ListenButton.IsEnabled = true;
            }
        }
    }
}
