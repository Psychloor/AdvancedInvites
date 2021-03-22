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

    using VRC.Core;
#if DEBUG
    using UnityEngine;
#endif
    
    
    public sealed class AdvancedInviteSystem : MelonMod
    {

        private static bool whitelistEnabled = true;

        private static bool blacklistEnabled = true;

        private static bool joinMeNotifyRequest = true;

        private static bool ignoreBusyStatus;

        private static readonly HashSet<string> HandledNotifications = new HashSet<string>();

    #if DEBUG
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P)) Utilities.Request();
        }
    #endif

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("AdvancedInvites", "Advanced Invites");
            MelonPreferences.CreateEntry("AdvancedInvites", "DeleteNotifications", InviteHandler.DeleteNotifications, "Delete Notification After Successful Use");
            MelonPreferences.CreateEntry("AdvancedInvites", "BlacklistEnabled", blacklistEnabled, "Blacklist System");
            MelonPreferences.CreateEntry("AdvancedInvites", "WhitelistEnabled", whitelistEnabled, "Whitelist System");
            MelonPreferences.CreateEntry("AdvancedInvites", "NotificationVolume", .8f, "Notification Volume");
            MelonPreferences.CreateEntry("AdvancedInvites", "JoinMeNotifyRequest", joinMeNotifyRequest, "Join Me Req Notification Sound");
            MelonPreferences.CreateEntry("AdvancedInvites", "IgnoreBusyStatus", ignoreBusyStatus, "Ignore Busy Status");
            OnPreferencesLoaded();

            try
            {
                // Appears to be NotificationManager.Method_Private_Void_Notification_1
                var acceptNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                        m => 
                             m.GetParameters().Length == 1 && 
                             m.GetParameters()[0].ParameterType == typeof(Notification) && 
                             m.XRefScanFor("AcceptNotification for notification:")
                             );
                Harmony.Patch(acceptNotificationMethod,
                    new HarmonyMethod(typeof(AdvancedInviteSystem).GetMethod(nameof(AcceptNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AcceptNotification: " + e.Message);
            }

            try
            {
                //Appears to be NotificationManager.Method_Private_String_Notification_1
                var addNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => 
                        m.Name.StartsWith("Method_Private_") &&
                        m.ReturnType == typeof(String) &&
                        m.GetParameters().Length == 1 && 
                        m.GetParameters()[0].ParameterType == typeof(Notification) && 
                        m.XRefScanFor("imageUrl")
                );
                Harmony.Patch(
                    addNotificationMethod,
                    postfix: new HarmonyMethod(
                        typeof(AdvancedInviteSystem).GetMethod(nameof(AddNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AddNotification: " + e.Message);
            }

            UserPermissionHandler.LoadSettings();
            WorldPermissionHandler.LoadSettings();
        }

        public override void VRChat_OnUiManagerInit()
        {
            UiButtons.Initialize();
            SoundPlayer.Initialize();
        }

        public override void OnPreferencesSaved()
        {
            InviteHandler.DeleteNotifications = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "DeleteNotifications");
            blacklistEnabled = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "BlacklistEnabled");
            whitelistEnabled = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "WhitelistEnabled");
            SoundPlayer.Volume = MelonPreferences.GetEntryValue<float>("AdvancedInvites", "NotificationVolume");
            joinMeNotifyRequest = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "JoinMeNotifyRequest");
            ignoreBusyStatus = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "IgnoreBusyStatus");
        }

        public override void OnPreferencesLoaded()
        {
            InviteHandler.DeleteNotifications = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "DeleteNotifications");
            blacklistEnabled = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "BlacklistEnabled");
            whitelistEnabled = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "WhitelistEnabled");
            SoundPlayer.Volume = MelonPreferences.GetEntryValue<float>("AdvancedInvites", "NotificationVolume");
            joinMeNotifyRequest = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "JoinMeNotifyRequest");
            ignoreBusyStatus = MelonPreferences.GetEntryValue<bool>("AdvancedInvites", "IgnoreBusyStatus");
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
                #if DEBUG
                    foreach (var key in __0.details.keys)
                    {
                        MelonLogger.Msg("Invite Details Key: " + key);
                        MelonLogger.Msg("Invite Details Value: " + __0.details[key].ToString());
                    }
                #endif

                    if (APIUser.CurrentUser.statusIsSetToDoNotDisturb
                        && !ignoreBusyStatus) return;

                    string worldId = __0.details["worldId"].ToString().Split(':')[0];
                    if (blacklistEnabled && (UserPermissionHandler.IsBlacklisted(__0.senderUserId) || WorldPermissionHandler.IsBlacklisted(worldId)))
                    {
                        Utilities.DeleteNotification(__0);
                        return;
                    }

                    SoundPlayer.PlayNotificationSound();
                    break;

                case "requestinvite":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

                    if (blacklistEnabled && UserPermissionHandler.IsBlacklisted(__0.senderUserId))
                    {
                        Utilities.DeleteNotification(__0);
                        return;
                    }

                    if (APIUser.CurrentUser.statusIsSetToDoNotDisturb
                        && !ignoreBusyStatus) return;

                    if (whitelistEnabled && UserPermissionHandler.IsWhitelisted(__0.senderUserId))
                    {

                        if (!Utilities.AllowedToInvite())
                        {
                            SoundPlayer.PlayNotificationSound();
                            return;
                        }

                        if (__0.details?.ContainsKey("platform") == true
                            && !Utilities.IsPlatformCompatibleWithCurrentWorld(__0.details["platform"].ToString()))
                        {
                            if (!APIUser.CurrentUser.statusIsSetToJoinMe)
                            {
                                // Bool's doesn't work and closes the game. just let it through
                                //Utilities.SendIncompatiblePlatformNotification(__0.senderUserId);
                                //Utilities.DeleteNotification(__0);
                                SoundPlayer.PlayNotificationSound();
                            }

                            return;
                        }

                        // Double Sending
                        if (!APIUser.CurrentUser.statusIsSetToJoinMe)
                        {
                            Utilities.AcceptInviteRequest(__0.senderUserId);
                            Utilities.DeleteNotification(__0);
                        }

                        if (APIUser.CurrentUser.statusIsSetToJoinMe && joinMeNotifyRequest)
                            SoundPlayer.PlayNotificationSound();
                    }
                    else
                    {
                        if (Utilities.AllowedToInvite())
                            if (APIUser.CurrentUser.statusIsSetToJoinMe
                                && !joinMeNotifyRequest)
                                return;

                        SoundPlayer.PlayNotificationSound();
                    }

                    return;

                default:
                    return;
            }
        }

        private static bool AcceptNotificationPatch(Notification __0)
        {
            if (!__0.notificationType.Equals("invite", StringComparison.OrdinalIgnoreCase)) return true;
            InviteHandler.HandleInvite(__0);
            return false;
        }

    }

}