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

    using UnhollowerBaseLib;

#endif

    public sealed class AdvancedInviteSystem : MelonMod
    {

        private static bool whitelistEnabled = true;

        private static bool blacklistEnabled = true;

        private static bool joinMeNotifyRequest = true;

        private static bool ignoreBusyStatus;

        private static MelonPreferences_Category settingsCategory;

        private static readonly HashSet<string> HandledNotifications = new HashSet<string>();



        public override void OnApplicationStart()
        {
            settingsCategory = MelonPreferences.CreateCategory("AdvancedInvites", "Advanced Invites");
            settingsCategory.CreateEntry("DeleteNotifications", InviteHandler.DeleteNotifications, "Delete Notification After Successful Use");
            settingsCategory.CreateEntry("BlacklistEnabled", blacklistEnabled, "Blacklist System");
            settingsCategory.CreateEntry("WhitelistEnabled", whitelistEnabled, "Whitelist System");
            settingsCategory.CreateEntry("NotificationVolume", .8f, "Notification Volume");
            settingsCategory.CreateEntry("JoinMeNotifyRequest", joinMeNotifyRequest, "Join Me Req Notification Sound");
            settingsCategory.CreateEntry("IgnoreBusyStatus", ignoreBusyStatus, "Ignore Busy Status");
            OnPreferencesLoaded();

        #if DEBUG

            
            // Method_Private_Void_4 build 1048
            /*foreach (MethodInfo method in typeof(QuickMenu).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(method => !method.IsAbstract && !method.IsVirtual && !method.IsGenericMethod && !method.IsGenericMethodDefinition && method.Name.IndexOf("method", StringComparison.OrdinalIgnoreCase) != -1))
                try
                {
                    // these all 4 got spammed constantly, really really spammed
                    if (method.Name.IndexOf("boolean", StringComparison.OrdinalIgnoreCase) != -1) continue;
                    if (method.Name.Equals("Method_Private_Void_3")) continue;
                    if (method.Name.Equals("Method_Private_Void_12")) continue;
                    if (method.Name.Equals("Method_Private_Void_PDM_7")) continue;
                        
                    Harmony.Patch(method, GetPatch(nameof(LogMethodPatch)));
                }
                catch
                {
                    // Ignored
                }*/
            //typeof(QuickMenu).GetMethod("Method_Private_Void_4", BindingFlags.Public | 
            
            try
            {
                MethodInfo sendNotificationMethod = typeof(NotificationManager).GetMethod(
                    nameof(NotificationManager.Method_Public_Void_String_String_String_String_NotificationDetails_ArrayOf_Byte_0),
                    BindingFlags.Public | BindingFlags.Instance);
                Harmony.Patch(sendNotificationMethod, GetPatch(nameof(SendNotificationPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching SendNotification: " + e.Message);
            }

            
            /*var methods = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                m => m.Name.StartsWith("method_public", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1
                                                                                      && m.GetParameters()[0].ParameterType == typeof(Notification));
            foreach (MethodInfo method in methods)
            {
                MelonLogger.Msg("Scanning Method: " + method.Name);
                var xrefInstances = XrefScanner.XrefScan(method);
                foreach (XrefInstance instance in xrefInstances)
                {
                    switch (instance.Type)
                    {
                        case XrefType.Global:
                            MelonLogger.Msg($"Global Instance: {instance.ReadAsObject()?.ToString()}");
                            break;
                        case XrefType.Method:
                            var resolved = instance.TryResolve();
                            if (resolved != null)
                                MelonLogger.Msg($"Method Instance: {resolved.DeclaringType?.Name.ToString()} {resolved.Name.ToString()}");
                            else
                                MelonLogger.Msg("Method Instance: Null");
                            break;
                        default:
                            break;
                    }
                }
                MelonLogger.Msg("");
            }*/

        #endif


            /*try
            {
                // Accept Notification
                IEnumerable<MethodInfo> acceptNotificationMethod = typeof(QuickMenu).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                    m => m.GetParameters().Length == 0 && m.XRefScanFor("AcceptNotification"));

                foreach (MethodInfo methodInfo in acceptNotificationMethod)
                {
                    if(methodInfo.IsAbstract || methodInfo.IsVirtual || methodInfo.IsGenericMethod || methodInfo.Name.IndexOf("method", StringComparison.OrdinalIgnoreCase) == -1) continue;
                    Harmony.Patch(methodInfo, GetPatch(nameof(AcceptNotificationPatch)));
                }
                
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AcceptNotification: " + e.Message);
            }*/

            try
            {
                // AddNotification - Method_Public_Void_Notification_EnumNPublicSealedvaAlReLo4vUnique_PDM_0 as of build 1010
                // Also seems to be the first one each time more. otherwise, could use Where as the other ones are fake
                IEnumerable<MethodInfo> addNotificationMethods = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                    m =>
                        {
                            if (!m.Name.StartsWith("Method_Public_Void_Notification_Enum")
                                || m.GetParameters().Length != 2
                                || m.GetParameters()[0].ParameterType != typeof(Notification)) return false;

                            // Some other ones for deleting? or something has strings in it
                            return XrefScanner.XrefScan(m).All(xrefInstance => xrefInstance.Type != XrefType.Global);
                        });

                foreach (MethodInfo notificationMethod in addNotificationMethods)
                    Harmony.Patch(notificationMethod, postfix: GetPatch(nameof(AddNotificationPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Patching AddNotification: " + e.Message);
            }

            UserPermissionHandler.LoadSettings();
            WorldPermissionHandler.LoadSettings();
        }

    #if DEBUG

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
            MelonLogger.Msg($"\tBytes: {__5}");
            MelonLogger.Msg("");

            return true;
        }
    #endif
        public override void VRChat_OnUiManagerInit()
        {
            UiButtons.Initialize();
            SoundPlayer.Initialize();
        }

        private static HarmonyMethod GetPatch(string method)
        {
            return new HarmonyMethod(typeof(AdvancedInviteSystem).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void LoadSettings()
        {
            InviteHandler.DeleteNotifications = settingsCategory.GetEntry<bool>("DeleteNotifications").Value;
            blacklistEnabled = settingsCategory.GetEntry<bool>("BlacklistEnabled").Value;
            whitelistEnabled = settingsCategory.GetEntry<bool>("WhitelistEnabled").Value;
            joinMeNotifyRequest = settingsCategory.GetEntry<bool>("JoinMeNotifyRequest").Value;
            ignoreBusyStatus = settingsCategory.GetEntry<bool>("IgnoreBusyStatus").Value;
            SoundPlayer.Volume = settingsCategory.GetEntry<float>("NotificationVolume").Value;

            // Since floats are weird with this new configuration update it seems to skip the "0." and just earrape you instead
            // also limits it to within 0-1 range
            if (SoundPlayer.Volume <= 1.0f) return;

            // .45 would turn into 45 (once updated to melonloader 0.3+) which would turn into 4.5 and then back to .45 again
            while (settingsCategory.GetEntry<float>("NotificationVolume").Value > 1.0f) settingsCategory.GetEntry<float>("NotificationVolume").Value *= .1f;
            settingsCategory.GetEntry<float>("NotificationVolume").Save();
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
            if (__0 == null) return;

            // Original code doesn't handle much outside worlds so
            if (Utilities.CurrentRoom() == null
                || Utilities.CurrentWorldInstance() == null) return;

            switch (__0.notificationType.ToLowerInvariant())
            {
                case "invite":
                    if (HandledNotifications.Contains(__0.id)) return;
                    HandledNotifications.Add(__0.id);

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

                                // Bool's doesn't work and closes the game. just let it through
                                //Utilities.SendIncompatiblePlatformNotification(__0.senderUserId);
                                //Utilities.DeleteNotification(__0);
                                SoundPlayer.PlayNotificationSound();

                            return;
                        }

                        // Double Sending
                        if (!APIUser.CurrentUser.statusIsSetToJoinMe)
                        {
                            Utilities.AcceptInviteRequest(__0.senderUserId, __0.senderUsername);
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

        private static bool AcceptNotificationPatch(MethodBase __originalMethod)
        {
        #if DEBUG
            MelonLogger.Msg("Entered Accept Notification Patch: "+__originalMethod?.Name);
        #endif

            Notification notification = Utilities.GetCurrentActiveNotification();

        #if DEBUG
            MelonLogger.Msg($"Current Notification Null: {notification == null}");

            if (notification != null) MelonLogger.Msg($"Notification Details: {notification.details?.ToString()}");
        #endif

            if (notification?.notificationType.Equals("invite", StringComparison.OrdinalIgnoreCase) == false) return true;
            InviteHandler.HandleInvite(notification);
            return false;
        }

    #if DEBUG
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P)) Utilities.Request();
        }

        // ReSharper disable once InconsistentNaming
        private static bool LogMethodPatch(QuickMenu __instance, MethodBase __originalMethod)
        {
            MelonLogger.Msg($"Method Called: {__originalMethod?.Name}");
            return true;
        }
    #endif

    }

}