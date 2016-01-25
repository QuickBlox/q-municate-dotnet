using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace QMunicate.Services
{
    public interface ICachingQuickbloxClient
    {
        Task<User> GetUserById(int userId);
        void DeleteUserFromCacheById(int userId);
        void ManuallyUpdateUserInCache(User user);
        void ClearUsersCache();
    }

    public class CachingQuickbloxClient : ICachingQuickbloxClient
    {
        #region Fields

        private readonly IQuickbloxClient quickbloxClient;
        private readonly List<User> users = new List<User>();
        private readonly object usersLock = new object();

        #endregion

        #region Ctor

        public CachingQuickbloxClient(IQuickbloxClient quickbloxClient)
        {
            this.quickbloxClient = quickbloxClient;
            quickbloxClient.ChatXmppClient.OnPresenceReceived += ChatXmppClientOnOnPresenceReceived;
        }

        #endregion

        #region ICachingQuickbloxClient Members

        public async Task<User> GetUserById(int userId)
        {
            User cachedUser;
            lock (usersLock)
            {
                cachedUser = users.FirstOrDefault(u => u.Id == userId);
            }
            
            if (cachedUser != null) return cachedUser;

            var response = await quickbloxClient.UsersClient.GetUserByIdAsync(userId);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                lock (usersLock)
                {
                    users.Add(response.Result.User);
                }
                return response.Result.User;
            }

            return null;
        }

        public void DeleteUserFromCacheById(int userId)
        {
            lock (usersLock)
            {
                users.RemoveAll(u => u.Id == userId);
            }
        }

        public void ManuallyUpdateUserInCache(User user)
        {
            DeleteUserFromCacheById(user.Id);

            lock (usersLock)
            {
                users.Add(user);
            }
        }

        public void ClearUsersCache()
        {
            lock (usersLock)
            {
                users.Clear();
            }
        }

        #endregion

        #region Private methods

        private void ChatXmppClientOnOnPresenceReceived(object sender, Presence presence)
        {
            lock (usersLock)
            {
                var userToUpdate = users.FirstOrDefault(u => u.Id == presence.UserId);
                if (userToUpdate != null)
                {
                    userToUpdate.LastRequestAt = DateTime.Now;
                }
            }
        }

        #endregion
    }
}
