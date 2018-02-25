using System.Linq;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Org.Libsdl.App;
using Urho;
using Urho.Droid;

namespace Sample.Droid
{
	[Activity(Label = "Sample",
		Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
		Name = "com.Sample.GameActivity",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
		ScreenOrientation = ScreenOrientation.Landscape)]
	public class GameActivity : Activity
	{
		private UrhoSurfacePlaceholder _surface;
        Urho.Application _application;

		protected override async void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			var mLayout = new AbsoluteLayout(this);
			var appType = Assembly.Load("Sample").GetTypes()
                .Where(_ => _.IsSubclassOf(typeof(Urho.Application)))
                .Where(_ => !_.IsAbstract)
                .FirstOrDefault();
            _surface = UrhoSurface.CreateSurface(this);
			mLayout.AddView(_surface);
			SetContentView(mLayout);

            _application = await _surface.Show(appType, new ApplicationOptions("Data"));
		}

		protected override void OnResume()
		{
			UrhoSurface.OnResume();
			base.OnResume();
		}

		protected override void OnPause()
		{
			UrhoSurface.OnPause();
			base.OnPause();
		}

		public override void OnLowMemory()
		{
			UrhoSurface.OnLowMemory();
			base.OnLowMemory();
		}

		protected override void OnDestroy()
		{
			UrhoSurface.OnDestroy();
			base.OnDestroy();
		}

		public override bool DispatchKeyEvent(KeyEvent e)
		{
			if (!UrhoSurface.DispatchKeyEvent(e))
				return false;
			return base.DispatchKeyEvent(e);
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			UrhoSurface.OnWindowFocusChanged(hasFocus);
			base.OnWindowFocusChanged(hasFocus);
		}
	}
}