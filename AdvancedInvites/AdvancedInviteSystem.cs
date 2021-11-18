namespace AdvancedInvites
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerBaseLib;

    using VRC.Core;

#if DEBUG
    using HarmonyLib;

    using UnityEngine;

#endif

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

        private static AddNotificationDelegate addNotificationDelegate;

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
                HarmonyInstance.Patch(
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
                // Appears to be NotificationManager.Method_Private_Void_Notification_1
                MethodInfo acceptNotificationMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 1
                         && m.GetParameters()[0].ParameterType == typeof(Notification)
                         && m.Name.IndexOf("PDM", StringComparison.OrdinalIgnoreCase) == -1
                         && m.XRefScanFor("AcceptNotification for notification:")
                         && m.XRefScanFor("Could not accept notification because notification details is null"));
                acceptNotificationDelegate = Patch<AcceptNotificationDelegate>(acceptNotificationMethod, GetDetour(nameof(AcceptNotificationPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AcceptNotification: " + e.Message);
            }

            try
            {
                //Appears to be NotificationManager.Method_Private_String_Notification_1
                MethodInfo addNotificationMethod = typeof(NotificationManager.ObjectNPrivateSealedNoVoBonoNo0).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 1
                         && m.GetParameters()[0].ParameterType == typeof(Notification)
                         && m.Name.IndexOf("addNotification", StringComparison.OrdinalIgnoreCase) >= 0);
                addNotificationDelegate = Patch<AddNotificationDelegate>(addNotificationMethod, GetDetour(nameof(AddNotificationPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AddNotification: " + e.Message);
            }

            try
            {
                // Faded to and joined and initialized room
                MethodInfo fadeMethod = typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).First(
                    m => m.Name.StartsWith("Method_Public_Void_String_Single_Action_")
                         && m.Name.IndexOf("PDM", StringComparison.OrdinalIgnoreCase) == -1
                         && m.GetParameters().Length == 3);
                origFadeTo = Patch<FadeTo>(fadeMethod, GetDetour(nameof(FadeToPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching FadeTo: " + e.Message);
            }

            UserPermissionHandler.LoadSettings();
            WorldPermissionHandler.LoadSettings();

            UiButtons.Initialize();
            SoundPlayer.Initialize();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FadeTo(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr);

        private static FadeTo origFadeTo;

        private static void FadeToPatch(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr)
        {
            if (instancePtr == IntPtr.Zero) return;
            origFadeTo(instancePtr, fadeNamePtr, fade, actionPtr, stackPtr);

            if (!IL2CPP.Il2CppStringToManaged(fadeNamePtr).Equals("BlackFade", StringComparison.Ordinal)
                || !fade.Equals(0f)
                || RoomManager.field_Internal_Static_ApiWorldInstance_0 == null) return;

            Utilities.CurrentInstanceCached = new Utilities.WorldInstanceCache(RoomManager.field_Internal_Static_ApiWorldInstance_0);
        }

        private static unsafe TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour)
        {
            IntPtr original = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(originalMethod).GetValue(null);
            MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        }

        private static IntPtr GetDetour(string name)
        {
            return typeof(AdvancedInviteSystem).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
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
        private static IntPtr AddNotificationPatch(IntPtr instancePtr, IntPtr notificationPtr, IntPtr returnedException)
        {
            if (instancePtr == IntPtr.Zero
                || notificationPtr == IntPtr.Zero) return IntPtr.Zero;

#if DEBUG
            MelonLogger.Msg("AddNotification");
#endif

            Notification notification = new Notification(notificationPtr);

            if (!HandledNotifications.Contains(notification.id))
            {
                HandledNotifications.Add(notification.id);
                HandleNotification(ref notification);
            }

            return addNotificationDelegate(instancePtr, notificationPtr, returnedException);
        }

        private static void HandleNotification(ref Notification notification)
        {
            if (Utilities.GetStreamerMode()) return;

            // Original code doesn't handle much outside worlds so
            if (Utilities.CurrentRoom() == null
                || Utilities.CurrentWorldInstance() == null) return;

            switch (notification.notificationType.ToLowerInvariant())
            {
                case "invite":
                #if DEBUG
                    if (notification.details?.keys != null)
                        foreach (string key in notification.details?.keys)
                        {
                            MelonLogger.Msg("Invite Details Key: " + key);
                            if (notification.details != null) MelonLogger.Msg("Invite Details Value: " + notification.details[key].ToString());
                        }
                #endif

                    if (APIUser.CurrentUser.statusIsSetToDoNotDisturb
                        && !ignoreBusyStatus) return;

                    string worldId = notification.details?["worldId"].ToString().Split(':')[0];
                    if (blacklistEnabled && (UserPermissionHandler.IsBlacklisted(notification.senderUserId) || WorldPermissionHandler.IsBlacklisted(worldId)))
                    {
                        Utilities.DeleteNotification(notification);
                        return;
                    }

                    if (inviteSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.Invite);
                    break;

                case "requestinvite":
                    if (blacklistEnabled && UserPermissionHandler.IsBlacklisted(notification.senderUserId))
                    {
                        Utilities.DeleteNotification(notification);
                        return;
                    }

                    if (APIUser.CurrentUser.statusIsSetToDoNotDisturb
                        && !ignoreBusyStatus) return;

                    if (whitelistEnabled && UserPermissionHandler.IsWhitelisted(notification.senderUserId))
                    {
                        if (!Utilities.AllowedToInvite())
                        {
                            if (inviteRequestSoundEnabled)
                                SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.InviteRequest);
                            return;
                        }

                        if (notification.details?.ContainsKey("platform") == true
                            && !Utilities.IsPlatformCompatibleWithCurrentWorld(notification.details["platform"].ToString()))
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
                            Utilities.AcceptInviteRequest(notification.senderUserId, notification.senderUsername);
                            Utilities.DeleteNotification(notification);
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

                // ReSharper disable StringLiteralTypo
                case "votetokick":
                    if (voteToKickSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.VoteToKick);
                    break;

                case "friendrequest":
                    if (friendRequestSoundEnabled)
                        SoundPlayer.PlayNotificationSound(SoundPlayer.NotificationType.FriendRequest);
                    break;

                default:
                    return;
            }
        }

        private static void AcceptNotificationPatch(IntPtr thisPtr, IntPtr notificationPtr, IntPtr returnedException)
        {
            try
            {
                if (thisPtr == IntPtr.Zero
                    || notificationPtr == IntPtr.Zero) return;
#if DEBUG
                MelonLogger.Msg("AcceptNotification");
#endif

                if (Utilities.GetStreamerMode())
                {
                    acceptNotificationDelegate(thisPtr, notificationPtr, returnedException);
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

            acceptNotificationDelegate(thisPtr, notificationPtr, returnedException);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr AddNotificationDelegate(IntPtr instancePtr, IntPtr notificationPtr, IntPtr returnedException);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void AcceptNotificationDelegate(IntPtr thisPtr, IntPtr notification, IntPtr returnedException);

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