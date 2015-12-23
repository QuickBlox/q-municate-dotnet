namespace QMunicate.ViewModels
{
    public class ViewModelLocator
    {
        #region Fields

        private DialogsViewModel dialogsViewModel;

        #endregion

        #region Properties

        public SignUpViewModel SignUpViewModel
        {
            get { return new SignUpViewModel(); }
        }

        public LoginViewModel LoginViewModel
        {
            get { return new LoginViewModel(); }
        }

        public ForgotPasswordViewModel ForgotPasswordViewModel
        {
            get { return new ForgotPasswordViewModel(); }
        }

        public DialogsViewModel DialogsViewModel
        {
            get { return dialogsViewModel ?? (dialogsViewModel = new DialogsViewModel()); }
        }

        public PrivateChatViewModel PrivateChatViewModel
        {
            get { return new PrivateChatViewModel(); }
        }

        public SettingsViewModel SettingsViewModel
        {
            get { return new SettingsViewModel(); }
        }

        public SearchViewModel SearchViewModel
        {
            get { return new SearchViewModel(); }
        }

        public SendRequestViewModel SendRequestViewModel
        {
            get { return new SendRequestViewModel(); }
        }

        public GroupChatViewModel GroupChatViewModel
        {
            get { return new GroupChatViewModel(); }
        }

        public NewMessageViewModel NewMessageViewModel
        {
            get { return new NewMessageViewModel(); }
        }

        public GroupAddMemberViewModel GroupAddMemberViewModel
        {
            get { return new GroupAddMemberViewModel(); }
        }

        public GroupInfoViewModel GroupInfoViewModel
        {
            get { return new GroupInfoViewModel(); }
        }

        public GroupEditViewModel GroupEditViewModel
        {
            get { return new GroupEditViewModel(); }
        }

        public SettingsEditViewModel SettingsEditViewModel
        {
            get { return new SettingsEditViewModel(); }
        }

        public UserInfoViewModel UserInfoViewModel
        {
            get { return new UserInfoViewModel(); }
        }

        public FirstViewModel FirstViewModel
        {
            get { return new FirstViewModel(); }
        }

        public ImagePreviewViewModel ImagePreviewViewModel
        {
            get { return new ImagePreviewViewModel(); }
        }

        #endregion

        #region Public methods

        public void Cleanup()
        {
            dialogsViewModel = null;
        }

        #endregion

    }
}