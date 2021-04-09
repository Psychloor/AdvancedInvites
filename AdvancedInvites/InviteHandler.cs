namespace AdvancedInvites
{

    using System;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnityEngine;

    using VRC.Core;
    using VRC.SDKBase;

    public static class InviteHandler
    {

        public static bool DeleteNotifications;

        private static Notification currentNotification;

        // for the current notification being handled
        private static string worldId;
        private static string instanceIdWithTags;

        public static void HandleInvite(Notification notification)
        {
            currentNotification = notification;
            worldId = notification.details["worldId"].ToString().Split(':')[0];
            
            // Whenever vrchat moves away from keeping both same and actually do keep instance as instance only
            string[] instanceIdDetails = notification.details["instanceId"].ToString().Split(':');
            instanceIdWithTags = instanceIdDetails.Length > 0 ? instanceIdDetails[1] : instanceIdDetails[0];

            ApiWorldInstance.AccessType accessType = Utilities.GetAccessType(instanceIdWithTags);

            var worldName = notification.details["worldName"].ToString();
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
            const int PlayerCount = 0;
            const bool ShowAlerts = true;

            // Fetch the world to know it exists and also needed for the world tags during the portal creation stage
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
                            if (created && DeleteNotifications)
                                try
                                {
                                    Utilities.DeleteNotification(currentNotification);
                                }
                                catch (Exception e)
                                {
                                    MelonLogger.Error("Couldn't delete the notification:\n"+e);
                                }
                        }),

                // On Failure
                new Action<ApiContainer>(container => Utilities.ShowAlert("Error Fetching World", container.Error)));
        }

        private static void JoinYourself()
        {
            Utilities.HideCurrentPopup();

            if (DeleteNotifications)
                try
                {
                    Utilities.DeleteNotification(currentNotification);
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Couldn't delete the notification:\n"+e);
                }
            
            Networking.GoToRoom($"{worldId}:{instanceIdWithTags}");
        }

    }

}