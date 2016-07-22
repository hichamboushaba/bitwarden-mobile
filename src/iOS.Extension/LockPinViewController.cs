﻿using System;
using Bit.iOS.Extension.Models;
using UIKit;
using XLabs.Ioc;
using Plugin.Settings.Abstractions;
using Foundation;
using MobileCoreServices;
using Bit.App.Abstractions;
using Bit.iOS.Core.Utilities;
using Bit.App.Resources;
using System.Diagnostics;
using Bit.App;

namespace Bit.iOS.Extension
{
    public partial class LockPinViewController : UIViewController
    {
        private ISettings _settings;
        private IAuthService _authService;

        public LockPinViewController(IntPtr handle) : base(handle)
        { }

        public Context Context { get; set; }
        public LoadingViewController LoadingViewController { get; set; }

        public override void ViewWillAppear(bool animated)
        {
            UINavigationBar.Appearance.ShadowImage = new UIImage();
            UINavigationBar.Appearance.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
            base.ViewWillAppear(animated);
        }

        public override void ViewDidLoad()
        {
            _settings = Resolver.Resolve<ISettings>();
            _authService = Resolver.Resolve<IAuthService>();

            View.BackgroundColor = new UIColor(red: 0.94f, green: 0.94f, blue: 0.96f, alpha: 1.0f);

            var descriptor = UIFontDescriptor.PreferredBody;
            PinLabel.Font = UIFont.FromName("Courier", descriptor.PointSize * 1.3f);

            PinTextField.EditingChanged += PinTextField_EditingChanged;

            base.ViewDidLoad();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            PinTextField.BecomeFirstResponder();
        }

        private void PinTextField_EditingChanged(object sender, EventArgs e)
        {
            SetLabelText();

            if(PinTextField.Text.Length >= 4)
            {
                if(PinTextField.Text == _authService.PIN)
                {
                    Debug.WriteLine("BW Log, Start Dismiss PIN controller.");
                    _settings.AddOrUpdateValue(Constants.SettingLocked, false);
                    PinTextField.ResignFirstResponder();
                    LoadingViewController.DismissLockAndContinue();
                }
                else
                {
                    // TODO: keep track of invalid attempts and logout?

                    var alert = Dialogs.CreateAlert(null, "Invalid PIN. Try again.", AppResources.Ok, (a) =>
                    {
                        PinTextField.Text = string.Empty;
                        SetLabelText();
                        PinTextField.BecomeFirstResponder();
                    });
                    PresentViewController(alert, true, null);
                }
            }
        }

        private void SetLabelText()
        {
            var newText = string.Empty;
            for(int i = 0; i < 4; i++)
            {
                newText += PinTextField.Text.Length <= i ? "- " : "● ";
            }

            PinLabel.Text = newText.TrimEnd();
        }

        partial void CancelButton_Activated(UIBarButtonItem sender)
        {
            CompleteRequest();
        }

        private void CompleteRequest()
        {
            var resultsProvider = new NSItemProvider(null, UTType.PropertyList);
            var resultsItem = new NSExtensionItem { Attachments = new NSItemProvider[] { resultsProvider } };
            var returningItems = new NSExtensionItem[] { resultsItem };

            Context.ExtContext.CompleteRequest(returningItems, null);
        }
    }
}