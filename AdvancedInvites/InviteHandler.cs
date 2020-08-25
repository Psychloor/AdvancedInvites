namespace AdvancedInvites
{

    using System;

    using Transmtn.DTO.Notifications;

    using VRC.Core;
    using VRC.SDKBase;

    public static class InviteHandler
    {

        private static Notification currentNotification;

        private static string worldId;

        public static void HandleInvite(Notification notification)
        {
            currentNotification = notification;
            worldId = notification.details["worldId"].ToString();

            string type = "Public";
            if (worldId.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                type = "Friends+";
            }
            
            else if (worldId.IndexOf("friends", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                JoinYourself();
            }
            
            else if (worldId.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                type = "Invite+";
            }
            
            else if (worldId.IndexOf("private", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                JoinYourself();
            }
            

            
            Utilities.ShowPopupWindow(
                "Invitation from " + notification.senderUsername,
                $"You have officially been invited to: \n{notification.details["worldName"].ToString()}\nInstance Type: {type}\n\nWanna go by yourself or drop a portal for the lads?",
                "Go Yourself",
                JoinYourself,
                "Drop Portal",
                DropPortal);
        }

        private static void DropPortal()
        {
            Utilities.CloseUi();

            // not gonna share the custom portal creation so apifetching and making worldinstance it is
            API.Fetch<ApiWorld>(
                worldId.Split(':')[0],
                new Action<ApiContainer>(
                    container =>
                        {
                            ApiWorld apiWorld = container.Model.Cast<ApiWorld>();
                            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(apiWorld, worldId.Split(':')[1], 0);

                            var playerTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
                            const bool ShowAlerts = true;

                            PortalInternal.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_Vector3_Vector3_Boolean_0(
                                apiWorld,
                                apiWorldInstance,
                                playerTransform.position,
                                playerTransform.forward,
                                ShowAlerts);
                            currentNotification = null;
                        }));
        }

        private static void JoinYourself()
        {
            Utilities.CloseUi();
            Networking.GoToRoom(worldId);
            currentNotification = null;
        }

    }

}