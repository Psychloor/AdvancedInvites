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

        // for the current notification being handled
        private static string worldId, instanceIdWithTags;

        public static void HandleInvite(Notification notification)
        {
            currentNotification = notification;
            worldId = notification.details["worldId"].ToString().Split(':')[0];

            // hmm it gets sent but it's not included when accepting an invite.....
            if (notification.details.ContainsKey("instanceId"))
            {
                string[] instanceIdDetails = notification.details["instanceId"].ToString().Split(':');
                instanceIdWithTags = instanceIdDetails.Length > 0 ? instanceIdDetails[1] : instanceIdDetails[0];
            }
            else
            {
                instanceIdWithTags = notification.details["worldId"].ToString().Split(':')[1];
            }

            InstanceAccessType accessType = Utilities.GetAccessType(instanceIdWithTags);
            Utilities.InstanceRegion region = Utilities.GetInstanceRegion(instanceIdWithTags);

            var worldName = notification.details["worldName"].ToString();
            string instanceType = Utilities.GetAccessName(accessType);

            switch (accessType)
            {
                case InstanceAccessType.Public:
                case InstanceAccessType.FriendsOfGuests:
                case InstanceAccessType.InvitePlus:
                    Utilities.ShowPopupWindow(
                        Localization.GetTitle(notification.senderUsername, worldName, instanceType, Utilities.RegionToName(region)),
                        Localization.GetPublicPopup(notification.senderUsername, worldName, instanceType, Utilities.RegionToName(region)),
                        Localization.GetJoinButton(),
                        JoinYourself,
                        Localization.GetDropPortalButton(),
                        DropPortal);
                    break;

                case InstanceAccessType.FriendsOnly:
                case InstanceAccessType.InviteOnly:
                    Utilities.ShowPopupWindow(
                        Localization.GetTitle(notification.senderUsername, worldName, instanceType, Utilities.RegionToName(region)),
                        Localization.GetPrivatePopup(notification.senderUsername, worldName, instanceType, Utilities.RegionToName(region)),
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
                            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(apiWorld, instanceIdWithTags);

                            Transform playerTransform = Utilities.GetLocalPlayerTransform();

                            // CreatePortal (before il2cpp)
                            bool created = Utilities.CreatePortal(apiWorld, apiWorldInstance, playerTransform.position, playerTransform.forward, ShowAlerts);
                            if (created && DeleteNotifications)
                                Utilities.DeleteNotification(currentNotification);
                        }),

                // On Failure
                new Action<ApiContainer>(container => Utilities.ShowAlert("Error Fetching World", container.Error)));
        }

        private static void JoinYourself()
        {
            Utilities.HideCurrentPopup();

            if (DeleteNotifications)
                Utilities.DeleteNotification(currentNotification);

            Networking.GoToRoom($"{worldId}:{instanceIdWithTags}");
        }

    }

}