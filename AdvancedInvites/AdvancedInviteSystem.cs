using AdvancedInvites;

using MelonLoader;

[assembly: MelonInfo(typeof(AdvancedInviteSystem), AdvancedInvites.BuildInfo.Name, AdvancedInvites.BuildInfo.Version, AdvancedInvites.BuildInfo.Author, AdvancedInvites.BuildInfo.DownloadLink)]
[assembly: MelonGame("VRChat", "VRChat")]

namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using Transmtn.DTO.Notifications;

    public class AdvancedInviteSystem : MelonMod
    {

        public override void OnApplicationStart()
        {
            // Accept Notification
            MethodInfo acceptNotificationMethod = typeof(QuickMenu).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                m => m.GetParameters().Length == 0 && m.XRefScanFor("AcceptNotification"));

            harmonyInstance.Patch(
                acceptNotificationMethod,
                new HarmonyMethod(typeof(AdvancedInviteSystem).GetMethod(nameof(AcceptNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static bool AcceptNotificationPatch()
        {
            Notification notification = QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
            if (notification.notificationType.Equals("invite", StringComparison.OrdinalIgnoreCase))
            {
                InviteHandler.HandleInvite(notification);
                return false;
            }

            return true;
        }

    }

}