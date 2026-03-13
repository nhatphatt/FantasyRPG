using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FantasyRPG.Core;
using Microsoft.Xna.Framework;

namespace FantasyRPG.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Icon = "@drawable/icon",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.Keyboard
        | ConfigChanges.KeyboardHidden
        | ConfigChanges.ScreenSize)]
public class Activity1 : AndroidGameActivity
{
    private GameRoot _game = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _game = new GameRoot();
        var view = _game.Services.GetService(typeof(View)) as View;

        if (view is not null)
            SetContentView(view);

        _game.Run();
    }
}
