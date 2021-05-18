namespace AdvancedInvites
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Harmony;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerBaseLib;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC.Core;

    public sealed class AdvancedInviteSystem : MelonMod
    {

        private static bool whitelistEnabled = true;

        private static bool blacklistEnabled = true;

        private static bool joinMeNotifyRequest = true;

        private static bool ignoreBusyStatus;

        private static bool inviteSoundEnabled, inviteRequestSoundEnabled, voteToKickSoundEnabled, friendRequestSoundEnabled;

        private static readonly HashSet<string> HandledNotifications = new HashSet<string>();

        private static MelonPreferences_Category advInvPreferencesCategory;

        private static AcceptNotificationDelegate acceptNotificationDelegate;

        public override void OnApplicationStart()
        {
            advInvPreferencesCategory = MelonPreferences.CreateCategory("AdvancedInvites", "Advanced Invites");

            advInvPreferencesCategory.CreateEntry("DeleteNotifications", InviteHandler.DeleteNotifications, "Delete Notification After Successful Use");
            advInvPreferencesCategory.CreateEntry("BlacklistEnabled", blacklistEnabled, "Blacklist System");
            advInvPreferencesCategory.CreateEntry("WhitelistEnabled", whitelistEnabled, "Whitelist System");
            advInvPreferencesCategory.CreateEntry("NotificationVolume", .8f, "Notification Volume");
            advInvPreferencesCategory.CreateEntry("JoinMeNotifyRequest", joinMeNotifyRequest, "Join Me Req Notification Sound");
            advInvPreferencesCategory.CreateEntry("IgnoreBusyStatus", ignoreBusyStatus, "Ignore Busy Status");

            advInvPreferencesCategory.CreateEntry("InviteSoundEnabled", true, "Invite Sound");
            advInvPreferencesCategory.CreateEntry("InviteRequestSoundEnabled", true, "Invite-Request Sound");
            advInvPreferencesCategory.CreateEntry("VoteToKickSoundEnabled", false, "Vote-Kick Sound", true);
            advInvPreferencesCategory.CreateEntry("FriendRequestSoundEnabled", false, "Friend-Request Sound", true);
            OnPreferencesLoaded();

            Localization.Load();

        #if DEBUG
            DebugTesting.Test();

            try
            {
                MethodInfo sendNotificationMethod = typeof(NotificationManager).GetMethod(
                    nameof(NotificationManager.Method_Public_Void_String_String_String_String_NotificationDetails_ArrayOf_Byte_0),
                    BindingFlags.Public | BindingFlags.Instance);
                Harmony.Patch(
                    sendNotificationMethod,
                    new HarmonyMethod(typeof(AdvancedInviteSystem).GetMethod(nameof(SendNotificationPatch), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching SendNotification: " + e.Message);
            }
        #endif

            try
            {
                unsafe
                {
                    // Appears to be NotificationManager.Method_Private_Void_Notification_1
                    MethodInfo acceptNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                        m => m.GetParameters().Length == 1
                             && m.GetParameters()[0].ParameterType == typeof(Notification)
                             && m.Name.IndexOf("PDM", StringComparison.OrdinalIgnoreCase) == -1
                             && m.XRefScanFor("AcceptNotification for notification:") && m.XRefScanFor("Could not accept notification because notification details is null"));
                    IntPtr originalMethod = *(IntPtr*)(IntPtr)UnhollowerUtils
                                                              .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(acceptNotificationMethod).GetValue(null);

                    MelonUtils.NativeHookAttach(
                        (IntPtr)(&originalMethod),
                        typeof(AdvancedInviteSystem).GetMethod(nameof(AcceptNotificationPatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle
                                                    .GetFunctionPointer());
                    acceptNotificationDelegate = Marshal.GetDelegateForFunctionPointer<AcceptNotificationDelegate>(originalMethod);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AcceptNotification: " + e.Message);
            }

            try
            {
                //Appears to be NotificationManager.Method_Private_String_Notification_1
                MethodInfo addNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.Name.StartsWith("Method_Private_")
                         && m.ReturnType == typeof(string)
                         && m.GetParameters().Length == 1
                         && m.GetParameters()[0].ParameterType == typeof(Notification)
                         && m.XRefScanFor("imageUrl"));
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

            UiButtons.Initialize();
            SoundPlayer.Initialize();
        }

        private static void LoadSettings()
        {
            InviteHandler.DeleteNotifications = advInvPreferencesCategory.GetEntry<bool>("DeleteNotifications").Value;
            blacklistEnabled = advInvPreferencesCategory.GetEntry<bool>("BlacklistEnabled").Value;
            whitelistEnabled = advInvPreferencesCategory.GetEntry<bool>("WhitelistEnabled").Value;
            joinMeNotifyRequest = advInvPreferencesCategory.GetEntry<bool>("JoinMeNotifyRequest").Value;
            ignoreBusyStatus = advInvPreferencesCategory.GetEntry<bool>("IgnoreBusyStatus").Value;
            SoundPlayer.Volume = advInvPreferencesCategory.GetEntry<float>("NotificationVolume").Value;

            inviteSoundEnabled = advInvPreferencesCategory.GetEntry<bool>("InviteSoundEnabled").Value;
            inviteRequestSoundEnabled = advInvPreferencesCategory.GetEntry<bool>("InviteRequestSoundEnabled").Value;
            voteToKickSoundEnabled = advInvPreferencesCategory.GetEntry<bool>("VoteToKickSoundEnabled").Value;
            friendRequestSoundEnabled = advInvPreferencesCategory.GetEntry<bool>("FriendRequestSoundEnabled").Value;

            // Since floats are weird with this new configuration update it seems to skip the "0." and just earrape you instead
            // also limits it to within 0-1 range
            if (SoundPlayer.Volume <= 1.0f) return;

            // .45 would turn into 45 (once updated to melonloader 0.3+) which would turn into 4.5 and then back to .45 again
            while (advInvPreferencesCategory.GetEntry<float>("NotificationVolume").Value > 1.0f)
                advInvPreferencesCategory.GetEntry<float>("NotificationVolume").Value *= .1f;
            advInvPreferencesCategory.GetEntry<float>("NotificationVolume").Save();
        }

        public override void OnPreferencesSaved()
        {
            LoadSettings();
        }

        public override void OnPreferencesLoaded()
        {
            LoadSettings();
        }

        // For some reason VRChat keeps doing "AddNotification" twice (AllTime and Recent) about once a second
        private static void AddNotificationPatch(Notification __0)
        {
            if (Utilities.GetStreamerMode()) return;
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
                    if (__0.details?.keys != null)
                        foreach (string key in __0.details?.keys)
                        {
                            MelonLogger.Msg("Invite Details Key: " + key);
                            if (__0.details != null) MelonLogger.Msg("Invite Details Value: " + __0.details[key].ToString());
                        }
                #endif

                    if (APIUser.CurrentUser.statusIsSetToDoNotDisturb
                        && !ignoreBusyStatus) return;

                    string worldId = __0.details?["worldId"].ToString().Split(':')[0];
                    if (blacklistEnabled && (UserPermissionHandler.IsBlacklisted(__0.senderUserId) || WorldPermissionHandler.IsBlacklisted(worldId)))
                    {
                        Utilities.DeleteNotification(__0);
                        return;
                    }

                    if (inviteSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.Invite);
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
                            if (inviteRequestSoundEnabled)
                                SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.InviteRequest);
                            return;
                        }

                        if (__0.details?.ContainsKey("platform") == true
                            && !Utilities.IsPlatformCompatibleWithCurrentWorld(__0.details["platform"].ToString()))
                        {
                            if (!APIUser.CurrentUser.statusIsSetToJoinMe)

                                // Bool's doesn't work and closes the game. just let it through
                                //Utilities.SendIncompatiblePlatformNotification(__0.senderUserId);
                                //Utilities.DeleteNotification(__0);
                                if (inviteRequestSoundEnabled)
                                    SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.InviteRequest);

                            return;
                        }

                        // Double Sending
                        if (!APIUser.CurrentUser.statusIsSetToJoinMe)
                        {
                            Utilities.AcceptInviteRequest(__0.senderUserId, __0.senderUsername);
                            Utilities.DeleteNotification(__0);
                        }

                        if (APIUser.CurrentUser.statusIsSetToJoinMe && joinMeNotifyRequest)
                            if (inviteRequestSoundEnabled)
                                SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.InviteRequest);
                    }
                    else
                    {
                        if (Utilities.AllowedToInvite())
                            if (APIUser.CurrentUser.statusIsSetToJoinMe
                                && !joinMeNotifyRequest)
                                return;
                        if (inviteRequestSoundEnabled)
                            SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.InviteRequest);
                    }

                    return;

                case "votetokick":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

                    if (voteToKickSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.VoteToKick);
                    break;

                case "friendrequest":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

                    if (friendRequestSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.FriendRequest);
                    break;

                default:
                    return;
            }
        }

        private static void AcceptNotificationPatch(IntPtr thisPtr, IntPtr notificationPtr)
        {
            try
            {
                if (thisPtr == IntPtr.Zero
                    || notificationPtr == IntPtr.Zero) return;
                if (Utilities.GetStreamerMode())
                {
                    acceptNotificationDelegate(thisPtr, notificationPtr);
                    return;
                }

                Notification notification = new Notification(notificationPtr);
                if (notification.notificationType.Equals("invite", StringComparison.OrdinalIgnoreCase))
                {
                    InviteHandler.HandleInvite(notification);
                    return;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Exception in accept notification patch: {e}");
            }

            acceptNotificationDelegate(thisPtr, notificationPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void AcceptNotificationDelegate(IntPtr thisPtr, IntPtr notification);

    #if DEBUG
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P)) Utilities.Request();
        }

        // username, userid, type, custom message, details, pic i guess?
        // details doesn't need requestslot if there's no custom message in it. probably safer to not send any
        private static bool SendNotificationPatch(string __0, string __1, string __2, string __3, NotificationDetails __4, Il2CppStructArray<byte> __5)
        {
            // Method_Public_Void_String_String_String_String_NotificationDetails_ArrayOf_Byte_0
            MelonLogger.Msg("Sending Notification:");
            MelonLogger.Msg($"\tString: {__0}");
            MelonLogger.Msg($"\tString: {__1}");
            MelonLogger.Msg($"\tString: {__2}");
            MelonLogger.Msg($"\tString: {__3}");
            MelonLogger.Msg($"\tDetails: {__4?.ToString()}");
            MelonLogger.Msg($"\tLength: {__5?.Length} Bytes: {__5}");
            MelonLogger.Msg("");

            return true;
        }

        private static void SetStreamerModePostfix(bool __0)
        {
            MelonLogger.Msg("Streamer Mode Set To " + __0);
        }
    #endif

    }

}