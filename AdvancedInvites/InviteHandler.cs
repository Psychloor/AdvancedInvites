namespace AdvancedInvites
{

    using System;

    using Transmtn.DTO.Notifications;

    using UnityEngine;

    using VRC.Core;
    using VRC.SDKBase;

    public static class InviteHandler
    {

        private static string worldInstance;

        public static void HandleInvite(Notification notification)
        {
            worldInstance = notification.details["worldId"].ToString();

            var instanceType = "Public";
            if (worldInstance.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0) instanceType = "Friends+";
            else if (worldInstance.IndexOf("friends", StringComparison.OrdinalIgnoreCase) >= 0) JoinYourself();
            else if (worldInstance.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0) instanceType = "Invite+";
            else if (worldInstance.IndexOf("private", StringComparison.OrdinalIgnoreCase) >= 0) JoinYourself();

            Utilities.ShowPopupWindow(
                "Invitation from " + notification.senderUsername,
                $"You have officially been invited to: \n{notification.details["worldName"].ToString()}\nInstance Type: {instanceType}\n\nWanna go by yourself or drop a portal for the lads?",
                "Go Yourself",
                JoinYourself,
                "Drop Portal",
                DropPortal);
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
                            Utilities.CloseUi();
                            ApiWorld apiWorld = container.Model.Cast<ApiWorld>();
                            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(apiWorld, instanceIdWithTags, PlayerCount);
                            
                            Transform playerTransform = Utilities.GetLocalPlayerTransform();

                            // CreatePortal (before il2cpp) and ignore portal successfully created as of now
                            Utilities.CreatePortal(apiWorld, apiWorldInstance, playerTransform.position, playerTransform.forward, ShowAlerts);
                        }));
        }

        private static void JoinYourself()
        {
            Utilities.CloseUi();
            Networking.GoToRoom(worldInstance);
        }

    }

}