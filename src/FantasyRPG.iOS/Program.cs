using System;
using Foundation;
using UIKit;

namespace FantasyRPG.iOS;

[Register("AppDelegate")]
internal sealed class Program : UIApplicationDelegate
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication application)
    {
        using var game = new Core.GameRoot();
        game.Run();
    }
}
