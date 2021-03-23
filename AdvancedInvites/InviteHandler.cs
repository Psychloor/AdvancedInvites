namespace AdvancedInvites
{

    using System;

    using Transmtn.DTO.Notifications;

    using UnityEngine;

    using VRC.Core;
    using VRC.SDKBase;

    public static class InviteHandler
    {

        public static bool DeleteNotifications;

        private static Notification currentNotification;

        private static string worldInstance;

        public static void HandleInvite(Notification notification)
        {
            currentNotification = notification;
            worldInstance = notification.details["worldId"].ToString();

            ApiWorldInstance.AccessType accessType = Utilities.GetAccessType(worldInstance.Split(':')[1]);

            string worldName = notification.details["worldName"].ToString();
            string instanceType = Utilities.GetAccessName(accessType);
            
            switch (accessType)
            {
                case ApiWorldInstance.AccessType.Public:
                case ApiWorldInstance.AccessType.FriendsOfGuests:
                case ApiWorldInstance.AccessType.InvitePlus:
                    Utilities.ShowPopupWindow(
                        Localization.GetTitle(notification.senderUsername, worldName, instanceType),
                        Localization.GetPublicPopup(notification.senderUsername, worldName, instanceType),
                        Localization.GetJoinButton(),
                        JoinYourself,
                        Localization.GetDropPortalButton(),
                        DropPortal);
                    break;

                case ApiWorldInstance.AccessType.FriendsOnly:
                case ApiWorldInstance.AccessType.InviteOnly:
                    Utilities.ShowPopupWindow(
                        Localization.GetTitle(notification.senderUsername, worldName, instanceType),
                        Localization.GetPrivatePopup(notification.senderUsername, worldName, instanceType),
                        Localization.GetJoinButton(),
                        JoinYourself);
                    break;

                default:
                    Utilities.ShowAlert("Error Getting AccessType", "Did you accept a message?");
                    break;
            }
        }

        private static void DropPortal()
        {
            string worldId = worldInstance.Split(':')[0];
            string instanceIdWithTags = worldInstance.Split(':')[1];
            const int PlayerCount = 0;
            const bool ShowAlerts = true;

            // not gonna share the custom portal creation so apifetching and making worldinstance it is
            // also needed as createportal will check for tags in the apiworld too
            API.Fetch<ApiWorld>(
                worldId,

                // On Success which it'll be if valid vanilla invite
                new Action<ApiContainer>(
                    container =>
                        {
                            Utilities.HideCurrentPopup();
                            ApiWorld apiWorld = container.Model.Cast<ApiWorld>();
                            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(apiWorld, instanceIdWithTags, PlayerCount);

                            Transform playerTransform = Utilities.GetLocalPlayerTransform();

                            // CreatePortal (before il2cpp)
                            bool created = Utilities.CreatePortal(apiWorld, apiWorldInstance, playerTransform.position, playerTransform.forward, ShowAlerts);
                            if (created && DeleteNotifications) Utilities.DeleteNotification(currentNotification);
                        }),

                // On Failure
                new Action<ApiContainer>(container => Utilities.ShowAlert("Error Fetching World", container.Error)));
        }

        private static void JoinYourself()
        {
            Utilities.HideCurrentPopup();
            if (DeleteNotifications) Utilities.DeleteNotification(currentNotification);
            Networking.GoToRoom(worldInstance);
        }

    }

}