﻿using System;
using System.ComponentModel;
using UIKit;

namespace Xamarin.Forms.Platform.iOS
{
	public class TabletShellFlyoutRenderer : UISplitViewController, IShellFlyoutRenderer, IFlyoutBehaviorObserver
	{
		#region IShellFlyoutRenderer

		UIViewController IShellFlyoutRenderer.ViewController => this;

		UIView IShellFlyoutRenderer.View => View;

		void IShellFlyoutRenderer.AttachFlyout(IShellContext context, UIViewController content)
		{
			if(_context != null)
				_context.Shell.PropertyChanged -= HandleShellPropertyChanged;

			_context = context;
			_content = content;

			FlyoutContent = _context.CreateShellFlyoutContentRenderer();
			FlyoutContent.WillAppear += OnFlyoutContentWillAppear;
			FlyoutContent.WillDisappear += OnFlyoutContentWillDisappear;

			((IShellController)_context.Shell).AddFlyoutBehaviorObserver(this);

			ViewControllers = new UIViewController[]
			{
				FlyoutContent.ViewController,
				content
			};
		}

		#endregion

		#region IFlyoutBehaviorObserver

		void IFlyoutBehaviorObserver.OnFlyoutBehaviorChanged(FlyoutBehavior behavior)
		{
			switch (behavior)
			{
				case FlyoutBehavior.Disabled:
					PreferredDisplayMode = UISplitViewControllerDisplayMode.PrimaryHidden;
					PresentsWithGesture = false;
					break;
				case FlyoutBehavior.Flyout:
					PreferredDisplayMode = UISplitViewControllerDisplayMode.PrimaryHidden;
					PresentsWithGesture = true;
					_isPresented = false;
					_context.Shell.SetValueFromRenderer(Shell.FlyoutIsPresentedProperty, false);
					break;
				case FlyoutBehavior.Locked:
					PreferredDisplayMode = UISplitViewControllerDisplayMode.AllVisible;
					break;
			}

			_flyoutBehavior = behavior;
		}

		#endregion

		IShellContext _context;
		UIViewController _content;
		bool _isPresented;
		FlyoutBehavior _flyoutBehavior;
		bool _disposed;

		IShellFlyoutContentRenderer FlyoutContent { get; set; }

		void OnFlyoutContentWillAppear(object sender, EventArgs e)
		{
			_isPresented = true;
			_context.Shell.SetValueFromRenderer(Shell.FlyoutIsPresentedProperty, true);
		}

		void OnFlyoutContentWillDisappear(object sender, EventArgs e)
		{
			_isPresented = false;
			_context.Shell.SetValueFromRenderer(Shell.FlyoutIsPresentedProperty, false);
		}

		protected virtual void HandleShellPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Shell.FlyoutIsPresentedProperty.PropertyName)
			{
				OnFlyoutIsPresentedChanged();
			}
			else if (e.PropertyName == VisualElement.FlowDirectionProperty.PropertyName)
			{
				UpdateFlowDirection(true);
			}
		}

		public override void ViewWillAppear(bool animated)
		{
			UpdateFlowDirection();
			base.ViewWillAppear(animated);
		}

		void UpdateFlowDirection(bool readdViews = false)
		{
			bool update = View.UpdateFlowDirection(_context.Shell);
			update = FlyoutContent.ViewController.View.UpdateFlowDirection(_context.Shell) == true || update;
			if (update && readdViews && Forms.IsiOS13OrNewer && View != null)
			{
				this.InvokeOnMainThread(() =>
				{
					var view = View.Superview;
					View.RemoveFromSuperview();
					view.AddSubview(View);
				});
			}
		}

		protected virtual void ToggleFlyout()
		{
			var barButtonItem = DisplayModeButtonItem;
			UIApplication.SharedApplication.SendAction(barButtonItem.Action, barButtonItem.Target, null, null);
		}

		protected virtual void OnFlyoutIsPresentedChanged()
		{
			var presented = _context.Shell.FlyoutIsPresented;

			if (_isPresented != presented)
				ToggleFlyout();
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			_context.Shell.PropertyChanged -= HandleShellPropertyChanged;
			_context.Shell.PropertyChanged += HandleShellPropertyChanged;
			UpdateFlowDirection();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				if (!_disposed)
				{
					_disposed = true;
					FlyoutContent.WillAppear -= OnFlyoutContentWillAppear;
					FlyoutContent.WillDisappear -= OnFlyoutContentWillDisappear;
					((IShellController)_context.Shell).RemoveFlyoutBehaviorObserver(this);
					_context.Shell.PropertyChanged -= HandleShellPropertyChanged;
				}

				FlyoutContent = null;
				_content = null;
				_context = null;
			}
		}
	}
}