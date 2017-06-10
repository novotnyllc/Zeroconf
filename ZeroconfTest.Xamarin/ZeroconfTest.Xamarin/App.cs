using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Zeroconf;

namespace ZeroconfTest.Xam  
{
#pragma warning disable RECS0014 // If all fields, properties and methods members are static, the class can be made static.
    public class App
#pragma warning restore RECS0014 // If all fields, properties and methods members are static, the class can be made static.
    {
        public static Page GetMainPage()
        {
            var output = new Label();

            var resolve = new Button
            {
                Text = "Resolve"
            };
            resolve.Clicked += (s, e) => ResolveOnClicked(output);
            var browse = new Button
            {
                Text = "Browse"
            };
            browse.Clicked += (s, e) => BrowseOnClicked(output);

            

            return new ContentPage
            {
                Content = new ScrollView
                {
                    Content = new StackLayout()
                    {
                        Children =
                    {
                        new Label{Text = "Zeroconf Test"},
                        resolve,
                        browse,
                        output
                    }
                    }
                }
            };
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        static async void BrowseOnClicked(Label output)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            ILookup<string, string> responses = null;
            //await Task.Run(async () =>
            //{
               
            //});

            responses = await ZeroconfResolver.BrowseDomainsAsync();
            foreach (var service in responses)
            {
                output.Text += (service.Key + Environment.NewLine);

                foreach (var host in service)
                    output.Text += (("\tIP: " + host) + Environment.NewLine);

            }
        }

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        static async void ResolveOnClicked(Label output)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
        {
            IReadOnlyList<IZeroconfHost> responses = null;
            //await Task.Run(async () =>
            //{

            //});


            var domains = await ZeroconfResolver.BrowseDomainsAsync();

            responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));
                

            foreach (var resp in responses)
            {
                output.Text += (resp + Environment.NewLine);
            }
            
        }
    }
}
