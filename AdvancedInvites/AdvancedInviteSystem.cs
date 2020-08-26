using AdvancedInvites;

using MelonLoader;

using BuildInfo = AdvancedInvites.BuildInfo;

[assembly: MelonInfo(typeof(AdvancedInviteSystem), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]

namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using MelonLoader;

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
            Notification notification = Utilities.GetCurrentActiveNotification();
            if (!notification.notificationType.Equals("invite", StringComparison.OrdinalIgnoreCase)) return true;
            InviteHandler.HandleInvite(notification);
            return false;
        }

    }

}