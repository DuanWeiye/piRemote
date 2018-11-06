// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace RaspiRemote
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnAircon { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnPic { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnSync { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton btnTV { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel labelTitle { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextView statusBox { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch swAircon { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch swSync { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch swTV { get; set; }

        [Action ("clickAircon:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void clickAircon (UIKit.UIButton sender);

        [Action ("clickPic:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void clickPic (UIKit.UIButton sender);

        [Action ("clickSync:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void clickSync (UIKit.UIButton sender);

        [Action ("clickTV:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void clickTV (UIKit.UIButton sender);

        [Action ("swAircon_Changed:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void swAircon_Changed (UIKit.UISwitch sender);

        [Action ("swSync_Changed:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void swSync_Changed (UIKit.UISwitch sender);

        [Action ("swTV_Changed:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void swTV_Changed (UIKit.UISwitch sender);

        void ReleaseDesignerOutlets ()
        {
            if (btnAircon != null) {
                btnAircon.Dispose ();
                btnAircon = null;
            }

            if (btnPic != null) {
                btnPic.Dispose ();
                btnPic = null;
            }

            if (btnSync != null) {
                btnSync.Dispose ();
                btnSync = null;
            }

            if (btnTV != null) {
                btnTV.Dispose ();
                btnTV = null;
            }

            if (labelTitle != null) {
                labelTitle.Dispose ();
                labelTitle = null;
            }

            if (statusBox != null) {
                statusBox.Dispose ();
                statusBox = null;
            }

            if (swAircon != null) {
                swAircon.Dispose ();
                swAircon = null;
            }

            if (swSync != null) {
                swSync.Dispose ();
                swSync = null;
            }

            if (swTV != null) {
                swTV.Dispose ();
                swTV = null;
            }
        }
    }
}