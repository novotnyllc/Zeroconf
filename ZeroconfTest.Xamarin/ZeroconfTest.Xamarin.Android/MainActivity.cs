using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using ZeroconfTest.Xam;

namespace ZeroconfTest.Xamarin.Droid
{
    [Activity(MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        WifiManager wifi;
        WifiManager.MulticastLock mlock;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            wifi = (WifiManager)ApplicationContext.GetSystemService(Context.WifiService);
            mlock = wifi.CreateMulticastLock("Zeroconf lock");
            mlock.Acquire();

            LoadApplication(new App());
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

