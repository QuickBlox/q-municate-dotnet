# Q-municate

Q-municate is an open source chat application with full range of communication features on board.

We are inspired to give you chat application out of the box. You can customize this application depending on your needs. As always QuickBlox backend is at your service: http://quickblox.com/plans/

Find the source code and more information about Q-municate in our Developers section: http://quickblox.com/developers/q-municate

This guide is created by QuickBlox team to explain how you can build a communication app for Windows Phone with Quickblox API. Enjoy and please get in touch, if you need assistance from QuickBlox team.

### Q-municate Windows Phone application uses following QuickBlox modules:

* Chat
* Users
* Content
* Messages

### It includes such features as:

* Two sign-up methods – Facebook and with email/password
* Login using Facebook account and email/password
* View Friends list
* Settings (edit users profile, reset password, logout)
* Create a private/group chat
* Participate in Private Chat
* Participate in Group Chat
* View list of all active chats with chat history (private chats and group chats)
* View and edit group chat info (title, logo, add friend or leave a group chat)
* Allow users to edit their profiles

Please note all these features are available in open source code, so you can customize your app depending on your needs.

**User Sign up page:**

![Signup](/images/signup.png "Signup")

**Chats list page:**

![ChatsList](/images/chatsList.png "Chats List")

**Private chat page:**

![Chat](/images/chat.png "Chat")

**New group page:**

![NewGroup](/images/newGroup.png "New group")

**User's profile page:**

![Profile](/images/profile.png "Profile")

## Important - how to build your own Chat app

If you want to build your own app using Q-municate as a basis, please do the following:

 1. Download the project from here (Github)
 2. Register a QuickBlox account (if you don't have one yet): http://admin.quickblox.com/register
 3. Log in to QuickBlox admin panel: http://admin.quickblox.com/signin
 4. Create a new app
 5. Click on the app title in the list to reveal the app details:
   ![App credentials](http://files.quickblox.com/app_credentials.png)
 6. Copy credentials (App ID, Authorization key, Authorization secret) into your Q-municate project code in `ApplicationKeys.cs`
 7. Enjoy!
 