using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace QMunicate.Controls
{
    public class TypingIndicator : Control
    {
        #region Fields

        private const string JumpAnimationName = "JumpAnimation";
        private const string PointsStackPanelName = "PointsStackPanel";
        private Storyboard jumpAnimation;
        private StackPanel pointsStackPanel;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsUserTypingProperty = DependencyProperty.Register("IsUserTyping",
            typeof(bool), typeof(TypingIndicator), new PropertyMetadata(VirtualKey.None, OnIsUserTypingChanged));

        public bool IsUserTyping
        {
            get { return (bool)GetValue(IsUserTypingProperty); }
            set { SetValue(IsUserTypingProperty, value); }
        }

        #endregion

        #region Base Members

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            jumpAnimation = GetTemplateChild(JumpAnimationName) as Storyboard;
            pointsStackPanel = GetTemplateChild(PointsStackPanelName) as StackPanel;

            if (jumpAnimation != null)
                jumpAnimation.Completed += jumpAnimation_Completed;
        }

        #endregion

        #region Private methods

        private static void OnIsUserTypingChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            (dependencyObject as TypingIndicator).IsUserTypingChanged();
        }

        private void IsUserTypingChanged()
        {
            if (IsUserTyping)
            {
                if (pointsStackPanel != null && pointsStackPanel.Visibility == Visibility.Collapsed)
                    pointsStackPanel.Visibility = Visibility.Visible;

                if (jumpAnimation != null && jumpAnimation.GetCurrentState() != ClockState.Active)
                    jumpAnimation.Begin();

            }
            else
            {
                if (jumpAnimation != null)
                    jumpAnimation.Stop();

                if (pointsStackPanel != null && pointsStackPanel.Visibility == Visibility.Visible)
                    pointsStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void jumpAnimation_Completed(object sender, object e)
        {
            if (IsUserTyping && jumpAnimation != null)
                jumpAnimation.Begin();
        }

        #endregion
    }
}
