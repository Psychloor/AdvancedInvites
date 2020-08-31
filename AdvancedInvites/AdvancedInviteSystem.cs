using AdvancedInvites;

using MelonLoader;

using BuildInfo = AdvancedInvites.BuildInfo;

[assembly: MelonInfo(typeof(AdvancedInviteSystem), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonGame("VRChat", "VRChat")]

namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    public sealed class AdvancedInviteSystem : MelonMod
    {

        public override void OnApplicationStart()
        {
            MelonPrefs.RegisterCategory("AdvancedInvites", "Advanced Invites");
            MelonPrefs.RegisterBool("AdvancedInvites", "RemoveNotifications", InviteHandler.RemoveNotifications, "Remove Notifications", true);
            InviteHandler.RemoveNotifications = MelonPrefs.GetBool("AdvancedInvites", "RemoveNotifications");

            try
            {
                // Accept Notification
                MethodInfo acceptNotificationMethod = typeof(QuickMenu).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.GetParameters().Length == 0 && m.XRefScanFor("AcceptNotification"));

                harmonyInstance.Patch(
                    acceptNotificationMethod,
                    new HarmonyMethod(typeof(AdvancedInviteSystem).GetMethod(nameof(AcceptNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                MelonLogger.LogError("Something went wrong patching: " + e.Message);
            }
        }

        public override void OnModSettingsApplied()
        {
            InviteHandler.RemoveNotifications = MelonPrefs.GetBool("AdvancedInvites", "RemoveNotifications");
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