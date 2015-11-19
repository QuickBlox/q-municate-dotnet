using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace QMunicate.Controls
{
    /// <summary>
    /// RadTextBox class.
    /// </summary>
    public class RadTextBox : TextBox
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ValidationState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationStateProperty =
            DependencyProperty.Register("ValidationState", typeof(ValidationState), typeof(RadTextBox), new PropertyMetadata(ValidationState.NotValidated));

        /// <summary>
        /// Gets the current ValidationState of RadTextBox.
        /// </summary>
        public ValidationState ValidationState
        {
            get { return (ValidationState)GetValue(ValidationStateProperty); }
            private set { SetValue(ValidationStateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ValidationMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.Register("ValidationMessage", typeof(String), typeof(RadTextBox), new PropertyMetadata(String.Empty));

        /// <summary>
        /// Gets the current ValidationMessage of RadTextBox.
        /// </summary>
        public String ValidationMessage
        {
            get { return (String)GetValue(ValidationMessageProperty); }
            private set { SetValue(ValidationMessageProperty, value); }
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RadTextBox"/> class.
        /// </summary>
        public RadTextBox()
        {
            DefaultStyleKey = typeof(RadTextBox);
        }

        #endregion

        #region Base Members

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes
        /// (such as a rebuilding layout pass) call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// In simplest terms, this means the method is called just before a UI element displays in an application. For more information, see Remarks.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (ValidationState != ValidationState.Invalid)
            {
                GoToState(ValidationState.ToString(), true);
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Used to change the ValidationState of the control and provide a custom message that will be displayed to clarify the state.
        /// </summary>
        public void ChangeValidationState(ValidationState validationState, String validationMessage)
        {
            if (validationState != ValidationState)
            {
                var newStateName = validationState.ToString();
                GoToState(newStateName, true);
                SetValidationMessage(validationMessage);
                SetValidationState(validationState);
                //if (this.owner.IsFocused)
                //{
                //    this.GoToFocusedState();
                //}
            }
        }

        #endregion

        #region Private Members

        private void GoToState(String stateName, Boolean useTransitions)
        {
            VisualStateManager.GoToState(this, stateName, useTransitions);
        }

        private void SetValidationState(ValidationState validationState)
        {
            ValidationState = validationState;
        }

        private void SetValidationMessage(String validationMessage)
        {
            ValidationMessage = validationMessage;
        }

        //internal void GoToFocusedState()
        //{
        //    if (this.owner.ValidationState == ValidationState.Valid)
        //    {
        //        this.owner.GoToState("FocusedValid", true);
        //    }
        //    else if (this.owner.ValidationState == ValidationState.Invalid)
        //    {
        //        this.owner.GoToState("FocusedInvalid", true);
        //    }
        //    else
        //    {
        //        this.owner.GoToState("Focused", true);
        //    }
        //}

        #endregion
    }
}
