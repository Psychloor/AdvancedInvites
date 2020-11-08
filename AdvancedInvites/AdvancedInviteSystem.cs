namespace AdvancedInvites
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

#if DEBUG
    using UnityEngine;
#endif

    public sealed class AdvancedInviteSystem : MelonMod
    {

    #if DEBUG
        public override void OnUpdate()
        {
            if(Input.GetKeyDown(KeyCode.P)) Utilities.Request();
        }
    #endif
        private static bool whitelistEnabled = true;

        private static bool blacklistEnabled = true;

        private static readonly HashSet<string> HandledNotifications = new HashSet<string>();

        public override void OnApplicationStart()
        {
            MelonPrefs.RegisterCategory("AdvancedInvites", "Advanced Invites");

            MelonPrefs.RegisterBool("AdvancedInvites", "DeleteNotifications", InviteHandler.DeleteNotifications, "Delete Notification After Successful Use");
            MelonPrefs.RegisterBool("AdvancedInvites", "BlacklistEnabled", blacklistEnabled, "Blacklist System");
            MelonPrefs.RegisterBool("AdvancedInvites", "WhitelistEnabled", whitelistEnabled, "Whitelist System");
            OnModSettingsApplied();

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
                MelonLogger.LogError("Error Patching AcceptNotification: " + e.Message);
            }

            try
            {
                // AddNotification - Method_Public_Void_Notification_EnumNPublicSealedvaAlReLo4vUnique_PDM_0 as of build 1010
                // Also seems to be the first one each time more. otherwise, could use Where as the other ones are fake
                MethodInfo addNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m =>
                        {
                            if (!m.Name.StartsWith("Method_Public_Void_Notification_Enum")
                                || m.GetParameters().Length != 2
                                || m.GetParameters()[0].ParameterType != typeof(Notification)) return false;

                            // Some other ones for deleting? or something has strings in it
                            return XrefScanner.XrefScan(m).All(xrefInstance => xrefInstance.Type != XrefType.Global);
                        });

                harmonyInstance.Patch(
                    addNotificationMethod,
                    postfix: new HarmonyMethod(
                        typeof(AdvancedInviteSystem).GetMethod(nameof(AddNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                MelonLogger.LogError("Error Patching AddNotification: " + e.Message);
            }

            UserPermissionHandler.LoadSettings();
            WorldPermissionHandler.LoadSettings();
        }

        public override void VRChat_OnUiManagerInit()
        {
            UiButtons.Initialize();
        }

        public override void OnModSettingsApplied()
        {
            InviteHandler.DeleteNotifications = MelonPrefs.GetBool("AdvancedInvites", "DeleteNotifications");
            blacklistEnabled = MelonPrefs.GetBool("AdvancedInvites", "BlacklistEnabled");
            whitelistEnabled = MelonPrefs.GetBool("AdvancedInvites", "WhitelistEnabled");
        }

        // For some reason VRChat keeps doing "AddNotification" twice (AllTime and Recent) about once a second
        private static void AddNotificationPatch(Notification __0)
        {
            if (__0 == null) return;

            // Original code doesn't handle much outside worlds so
            if (Utilities.CurrentRoom() == null
                || Utilities.CurrentWorldInstance() == null) return;

            switch (__0.notificationType.ToLowerInvariant())
            {
                case "invite":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

                    string worldId = __0.details["worldId"].ToString().Split(':')[0];
                    if (blacklistEnabled && (UserPermissionHandler.IsBlacklisted(__0.senderUserId) || WorldPermissionHandler.IsBlacklisted(worldId)))
                        Utilities.DeleteNotification(__0);
                    break;

                case "requestinvite":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

                    if (blacklistEnabled && UserPermissionHandler.IsBlacklisted(__0.senderUserId))
                    {
                        Utilities.DeleteNotification(__0);
                        return;
                    }

                    if (!whitelistEnabled
                        || !UserPermissionHandler.IsWhitelisted(__0.senderUserId)) return;
                    if (!Utilities.AllowedToInvite()) return;

                    if (__0.details?.ContainsKey("platform") == true
                        && !Utilities.IsPlatformCompatibleWithCurrentWorld(__0.details["platform"].ToString()))

                        // Bool's doesn't work and closes the game. just let it through
                        //Utilities.SendIncompatiblePlatformNotification(__0.senderUserId);
                        //Utilities.DeleteNotification(__0);
                        return;

                    Utilities.AcceptInviteRequest(__0.senderUserId);
                    Utilities.DeleteNotification(__0);
                    return;

                default:
                    return;
            }
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