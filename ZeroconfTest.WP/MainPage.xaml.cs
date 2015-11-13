using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Zeroconf;

namespace ZeroconfTest.WP
{
    public partial class MainPage : INotifyPropertyChanged
    {
        public MainPage()
        {
            InitializeComponent();
            Protocols = new List<string>
                            {
                                "_airplay._tcp",
                                "_http._tcp",
                                "_accessone._tcp",
                                "_accountedge._tcp",
                                "_actionitems._tcp",
                                "_addressbook._tcp",
                                "_aecoretech._tcp",
                                "_afpovertcp._tcp",
                                "_airport._tcp",
                                "_animobserver._tcp",
                                "_animolmd._tcp",
                                "_apple-sasl._tcp",
                                "_aquamon._tcp",
                                "_async._tcp",
                                "_auth._tcp",
                                "_beep._tcp",
                                "_bfagent._tcp",
                                "_bootps._udp",
                                "_bousg._tcp",
                                "_bsqdea._tcp",
                                "_cheat._tcp",
                                "_chess._tcp",
                                "_clipboard._tcp",
                                "_collection._tcp",
                                "_contactserver._tcp",
                                "_cvspserver._tcp",
                                "_cytv._tcp",
                                "_daap._tcp",
                                "_difi._tcp",
                                "_distcc._tcp",
                                "_dossier._tcp",
                                "_dpap._tcp",
                                "_earphoria._tcp",
                                "_ebms._tcp",
                                "_ebreg._tcp",
                                "_ecbyesfsgksc._tcp",
                                "_eheap._tcp",
                                "_embrace._tcp",
                                "_eppc._tcp",
                                "_eventserver._tcp",
                                "_exec._tcp",
                                "_facespan._tcp",
                                "_faxstfx._tcp",
                                "_fish._tcp",
                                "_fjork._tcp",
                                "_fmpro-internal._tcp",
                                "_ftp._tcp",
                                "_ftpcroco._tcp",
                                "_gbs-smp._tcp",
                                "_gbs-stp._tcp",
                                "_grillezvous._tcp",
                                "_h323._tcp",
                                "_hotwayd._tcp",
                                "_hydra._tcp",
                                "_ica-networking._tcp",
                                "_ichalkboard._tcp",
                                "_ichat._tcp",
                                "_iconquer._tcp",
                                "_ifolder._tcp",
                                "_ilynx._tcp",
                                "_imap._tcp",
                                "_imidi._tcp",
                                "_ipbroadcaster._tcp",
                                "_ipp._tcp",
                                "_ishare._tcp",
                                "_isparx._tcp",
                                "_ispq-vc._tcp",
                                "_isticky._tcp",
                                "_istorm._tcp",
                                "_iwork._tcp",
                                "_lan2p._tcp",
                                "_ldap._tcp",
                                "_liaison._tcp",
                                "_login._tcp",
                                "_lontalk._tcp",
                                "_lonworks._tcp",
                                "_macfoh-remote._tcp",
                                "_macminder._tcp",
                                "_moneyworks._tcp",
                                "_mp3sushi._tcp",
                                "_mttp._tcp",
                                "_ncbroadcast._tcp",
                                "_ncdirect._tcp",
                                "_ncsyncserver._tcp",
                                "_net-assistant._tcp",
                                "_newton-dock._tcp",
                                "_nfs._udp",
                                "_nssocketport._tcp",
                                "_odabsharing._tcp",
                                "_omni-bookmark._tcp",
                                "_openbase._tcp",
                                "_p2pchat._udp",
                                "_pdl-datastream._tcp",
                                "_poch._tcp",
                                "_pop3._tcp",
                                "_postgresql._tcp",
                                "_presence._tcp",
                                "_printer._tcp",
                                "_ptp._tcp",
                                "_quinn._tcp",
                                "_raop._tcp",
                                "_rce._tcp",
                                "_realplayfavs._tcp",
                                "_rfb._tcp",
                                "_riousbprint._tcp",
                                "_rtsp._tcp",
                                "_safarimenu._tcp",
                                "_sallingclicker._tcp",
                                "_scone._tcp",
                                "_sdsharing._tcp",
                                "_see._tcp",
                                "_seeCard._tcp",
                                "_serendipd._tcp",
                                "_servermgr._tcp",
                                "_sge-exec._tcp",
                                "_sge-qmaster._tcp",
                                "_shell._tcp",
                                "_shout._tcp",
                                "_shoutcast._tcp",
                                "_soap._tcp",
                                "_spike._tcp",
                                "_spincrisis._tcp",
                                "_spl-itunes._tcp",
                                "_spr-itunes._tcp",
                                "_ssh._tcp",
                                "_ssscreenshare._tcp",
                                "_stickynotes._tcp",
                                "_strateges._tcp",
                                "_sxqdea._tcp",
                                "_sybase-tds._tcp",
                                "_teamlist._tcp",
                                "_teleport._udp",
                                "_telnet._tcp",
                                "_tftp._udp",
                                "_ticonnectmgr._tcp",
                                "_tinavigator._tcp",
                                "_tryst._tcp",
                                "_upnp._tcp",
                                "_utest._tcp",
                                "_vue4rendercow._tcp",
                                "_webdav._tcp",
                                "_whamb._tcp",
                                "_wired._tcp",
                                "_workgroup._tcp",
                                "_workstation._tcp",
                                "_wormhole._tcp",
                                "_ws._tcp",
                                "_xserveraid._tcp",
                                "_xsync._tcp",
                                "_xtshapro._tcp",
                            };
            Servers = new ObservableCollection<IZeroconfHost>();
            DataContext = this;
        }

        public List<string> Protocols { get; set; }
        public string Protocol { get; set; }
        public ObservableCollection<IZeroconfHost> Servers { get; set; }

        void ProtocolSelected(object sender, SelectionChangedEventArgs e)
        {
            Protocol = e.AddedItems.Count > 0 ? (string) e.AddedItems[0] : "";
            OnPropertyChanged("Protocol");
        }

        async void BrowseClick(object sender, RoutedEventArgs e)
        {

            //var protocol = string.IsNullOrEmpty(Protocol) ? ProtocolPicker.SelectedItem : Protocol;

            //var responses = await ZeroconfResolver.ResolveAsync(protocol + ".local.", TimeSpan.FromSeconds(5));

            //foreach (var resp in responses)
            //{
            //    Servers.Add(resp);
            //    Debug.WriteLine(resp);
            //}

            var domains = await ZeroconfResolver.BrowseDomainsAsync();

            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));


            foreach (var resp in responses)
            {
                Servers.Add(resp);
                Debug.WriteLine(resp);
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}