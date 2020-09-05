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
            switch (accessType)
            {
                case ApiWorldInstance.AccessType.Public:
                case ApiWorldInstance.AccessType.FriendsOfGuests:
                case ApiWorldInstance.AccessType.InvitePlus:
                    Utilities.ShowPopupWindow(
                        "Invitation from " + notification.senderUsername,
                        $"You have officially been invited to: \n{notification.details["worldName"].ToString()}\nInstance Type: {Utilities.GetAccessName(accessType)}\n\nWanna go by yourself or drop a portal for the lads?",
                        "Go Yourself",
                        JoinYourself,
                        "Drop Portal",
                        DropPortal);
                    break;

                case ApiWorldInstance.AccessType.FriendsOnly:
                case ApiWorldInstance.AccessType.InviteOnly:
                    Utilities.ShowPopupWindow(
                        "Invitation from " + notification.senderUsername,
                        $"You have officially been invited to: \n{notification.details["worldName"].ToString()}\nInstance Type: {Utilities.GetAccessName(accessType)}\n\nPrivate Instance so can't drop a portal",
                        "Join",
                        JoinYourself);
                    break;

                case ApiWorldInstance.AccessType.Counter:
                default:
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