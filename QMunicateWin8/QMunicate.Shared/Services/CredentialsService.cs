using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Security.Credentials;

namespace QMunicate.Services
{
    public interface ICredentialsService
    {
        /// <summary>
        /// In memory place for current user ID. Is not persisted anywhere.
        /// </summary>
        int CurrentUserId { get; set; }

        /// <summary>
        /// In memory place for current user password. Is not persisted anywhere.
        /// </summary>
        string CurrentPassword { get; set; }

        Credentials GetSavedCredentials();
        void SaveCredentials(Credentials credentials);
        void DeleteSavedCredentials();
    }

    public class Credentials
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class CredentialsService : ICredentialsService
    {
        private const string PasswordVaultResourceName = "QMunicateCredentials";

        /// <summary>
        /// In memory place for current user ID. Is not persisted anywhere.
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        /// In memory place for current user password. Is not persisted anywhere.
        /// </summary>
        public string CurrentPassword { get; set; }

        public Credentials GetSavedCredentials()
        {
            try
            {
                var passwordVault = new PasswordVault();
                var passwordCredentials = passwordVault.FindAllByResource(PasswordVaultResourceName);

                if (passwordCredentials == null || !passwordCredentials.Any()) return null;

                passwordCredentials[0].RetrievePassword();

                return new Credentials
                {
                    Login = passwordCredentials[0].UserName,
                    Password = passwordCredentials[0].Password
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SaveCredentials(Credentials credentials)
        {
            var passwordVault = new PasswordVault();
            var passwordCredentials = new PasswordCredential(PasswordVaultResourceName, credentials.Login, credentials.Password);
            passwordVault.Add(passwordCredentials);
        }

        public void DeleteSavedCredentials()
        {
            try
            {
                var passwordVault = new PasswordVault();
                var credentials = passwordVault.FindAllByResource(PasswordVaultResourceName);
                if (credentials != null && credentials.Any())
                {
                    passwordVault.Remove(credentials[0]);
                }
            }
            catch (Exception) { }
        }
    }
}
