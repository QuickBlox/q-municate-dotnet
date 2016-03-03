using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace QMunicate.Controls
{
    public class SelectionsTextBlock : Control
    {
        #region Fields

        private const string TextStackPanelName = "TextStackPanel";
        private StackPanel textStackPanel;

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof (string),
            typeof (SelectionsTextBlock), new PropertyMetadata(String.Empty, PropertyChangedCallback));

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty SelectionTextProperty = DependencyProperty.Register("SelectionText",
            typeof (string), typeof (SelectionsTextBlock), new PropertyMetadata(String.Empty, PropertyChangedCallback));

        public string SelectionText
        {
            get { return (string) GetValue(SelectionTextProperty); }
            set { SetValue(SelectionTextProperty, value); }
        }

        public static readonly DependencyProperty SelectionColorProperty = DependencyProperty.Register(
            "SelectionColor", typeof (Color), typeof (SelectionsTextBlock),
            new PropertyMetadata(default(Color), PropertyChangedCallback));

        public Color SelectionColor
        {
            get { return (Color) GetValue(SelectionColorProperty); }
            set { SetValue(SelectionColorProperty, value); }
        }

        #endregion

        #region Ctor

        public SelectionsTextBlock()
        {
            DefaultStyleKey = typeof (SelectionsTextBlock);
        }

        #endregion

        #region Base Members

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            textStackPanel = (StackPanel)GetTemplateChild(TextStackPanelName);
            FormatSelection(this);
        }

        #endregion

        #region Private methods

        private static void PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var thisControl = dependencyObject as SelectionsTextBlock;
            FormatSelection(thisControl);
        }

        private static void FormatSelection(SelectionsTextBlock thisControl, bool ignoreCase = true)
        {
            if (thisControl.textStackPanel == null) return;

            thisControl.textStackPanel.Children.Clear();

            int selectionIndex = thisControl.Text.IndexOf(thisControl.SelectionText, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            if (string.IsNullOrWhiteSpace(thisControl.Text) || string.IsNullOrWhiteSpace(thisControl.SelectionText) ||
                selectionIndex == -1)
            {
                thisControl.textStackPanel.Children.Add(new TextBlock() { Text = thisControl.Text });
                return;
            }

            if (selectionIndex != 0)
            {
                var beforePart = thisControl.Text.Substring(0, selectionIndex);
                thisControl.textStackPanel.Children.Add(new TextBlock() { Text = beforePart });

            }

            var selectedPart = thisControl.Text.Substring(selectionIndex, thisControl.SelectionText.Length);
            thisControl.textStackPanel.Children.Add(new TextBlock() { Text = selectedPart, Foreground = new SolidColorBrush(thisControl.SelectionColor) });

            if (selectionIndex + thisControl.SelectionText.Length < thisControl.Text.Length)
            {
                var afterPart = thisControl.Text.Substring(selectionIndex + thisControl.SelectionText.Length);
                thisControl.textStackPanel.Children.Add(new TextBlock() { Text = afterPart });

            }
        }

        #endregion

    }
}
