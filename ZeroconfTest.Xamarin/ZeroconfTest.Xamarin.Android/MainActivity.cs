using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;
using ZeroconfTest.Xam;

namespace ZeroconfTest.Xamarin.Droid
{
	[Activity (Label = "ZeroconfTest.Xamarin", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : FormsApplicationActivity
	{
        WifiManager wifi;
        WifiManager.MulticastLock mlock;
        protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

            wifi = (WifiManager)ApplicationContext.GetSystemService(Context.WifiService);
            mlock = wifi.CreateMulticastLock("Zeroconf lock");
            mlock.Acquire();
#pragma warning disable CS0618 // Type or member is obsolete
            SetPage(App.GetMainPage ());
#pragma warning restore CS0618 // Type or member is obsolete
        }

	    protected override void OnDestroy()
	    {
            if (mlock != null)
            {
                mlock.Release();
                mlock = null;
            }
            base.OnDestroy();
	    }
	}
}

