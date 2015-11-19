using System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using QMunicate.Core.Extensions;

namespace QMunicate.Behaviors
{
    /// <summary>
    /// StatusBarBehavior class.
    /// </summary>
    public class StatusBarBehavior : DependencyObject, IBehavior
    {

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register("IsVisible", typeof(Boolean), typeof(StatusBarBehavior), new PropertyMetadata(true, OnIsVisibleChanged));

        public Boolean IsVisible
        {
            get { return (Boolean)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static readonly DependencyProperty BackgroundOpacityProperty = DependencyProperty.Register("BackgroundOpacity", typeof(double), typeof(StatusBarBehavior), new PropertyMetadata(1d, OnOpacityChanged));
        
        public double BackgroundOpacity
        {
            get { return (double)GetValue(BackgroundOpacityProperty); }
            set { SetValue(BackgroundOpacityProperty, value); }
        }

        public static readonly DependencyProperty ForegroundColorProperty = DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(StatusBarBehavior), new PropertyMetadata(null, OnForegroundColorChanged));

        public Color ForegroundColor
        {
            get { return (Color)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(StatusBarBehavior), new PropertyMetadata(null, OnBackgroundChanged));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading", typeof(Boolean), typeof(StatusBarBehavior), new PropertyMetadata(true, OnIsLoadingChanged));

        public Boolean IsLoading
        {
            get { return (Boolean)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public void Attach(DependencyObject associatedObject)
        {
            //ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        }

        public void Detach()
        {
            //ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
        }

        public DependencyObject AssociatedObject { get; private set; }

        private static void OnIsVisibleChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            var isvisible = (Boolean)args.NewValue;
            if (isvisible)
            {
                StatusBar.GetForCurrentView().ShowAsync().Forget();
            }
            else
            {
                StatusBar.GetForCurrentView().HideAsync().Forget();
            }
        }

        private static void OnIsLoadingChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            var isvisible = (Boolean)args.NewValue;
            if (isvisible)
            {
                StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync().Forget();
            }
            else
            {
                StatusBar.GetForCurrentView().ProgressIndicator.HideAsync().Forget();
            }
        }

        private static void OnOpacityChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            StatusBar.GetForCurrentView().BackgroundOpacity = (double)args.NewValue;
        }

        private static void OnForegroundColorChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            StatusBar.GetForCurrentView().ForegroundColor = (Color)args.NewValue;
        }

        private static void OnBackgroundChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            var behavior = (StatusBarBehavior)target;
            StatusBar.GetForCurrentView().BackgroundColor = behavior.BackgroundColor;

            // if they have not set the opacity, we need to so the new color is shown
            if (behavior.BackgroundOpacity == 0)
            {
                behavior.BackgroundOpacity = 1;
            }
        }
    }
}
