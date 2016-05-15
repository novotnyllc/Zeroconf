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
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			global::Xamarin.Forms.Forms.Init (this, bundle);

            wifi = (WifiManager)ApplicationContext.GetSystemService(Context.WifiService);
            mlock = wifi.CreateMulticastLock("Zeroconf lock");
            mlock.Acquire();
            SetPage (App.GetMainPage ());
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

