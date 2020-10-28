namespace AdvancedInvites
{

    using System.Linq;
    using System.Reflection;

    using Il2CppSystem;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC.Core;

    using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
    using Delegate = System.Delegate;
    using Exception = System.Exception;
    using StringComparison = System.StringComparison;
    using Type = System.Type;

    public static class Utilities
    {

        private static CreatePortalDelegate ourCreatePortalDelegate;

        private static DeleteNotificationDelegate ourDeleteNotificationDelegate;

        private static ShowAlertDelegate ourShowAlertDelegate;

        private static ShowPopupWindowBothDelegate ourShowPopupWindowBothDelegate;

        private static ShowPopupWindowSingleDelegate ourShowPopupWindowSingleDelegate;

        private static VRCUiManagerDelegate ourVRCUiManagerDelegate;

        internal delegate VRCUiManager VRCUiManagerDelegate();

        internal static VRCUiManagerDelegate GetVRCUiManager
        {
            get
            {
                if (ourVRCUiManagerDelegate != null) return ourVRCUiManagerDelegate;
                MethodInfo vrcUiManagerInstance = typeof(VRCUiManager).GetMethods().First(x => x.ReturnType == typeof(VRCUiManager));
                ourVRCUiManagerDelegate = (VRCUiManagerDelegate)Delegate.CreateDelegate(typeof(VRCUiManagerDelegate), vrcUiManagerInstance);
                return ourVRCUiManagerDelegate;
            }
        }

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts);

        private delegate void DeleteNotificationDelegate(Notification notification);

        private delegate void ShowAlertDelegate(string title, string content, float timeOut);

        private delegate void ShowPopupWindowBothDelegate(
            string title,
            string content,
            string button1,
            Action action,
            string button2,
            Action action2,
            Action<VRCUiPopup> onCreated = null);

        private delegate void ShowPopupWindowSingleDelegate(string title, string content, string button, Action action, Action<VRCUiPopup> onCreated = null);

        private static CreatePortalDelegate GetCreatePortalDelegate
        {
            get
            {
                if (ourCreatePortalDelegate != null) return ourCreatePortalDelegate;
                MethodInfo portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static).First(
                    m => m.ReturnType == typeof(bool) && m.HasParameters(
                             typeof(ApiWorld),
                             typeof(ApiWorldInstance),
                             typeof(Vector3),
                             typeof(Vector3),
                             typeof(bool)) && m.XRefScanFor("admin_dont_allow_portal"));
                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }

        public static ApiWorldInstance.AccessType GetAccessType(string tags)
        {
            if (tags.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.FriendsOfGuests;
            if (tags.IndexOf("friends", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.FriendsOnly;
            if (tags.IndexOf("request", StringComparison.OrdinalIgnoreCase) >= 0) return ApiWorldInstance.AccessType.InvitePlus;
            return tags.IndexOf("private", StringComparison.OrdinalIgnoreCase) >= 0 ? ApiWorldInstance.AccessType.InviteOnly : ApiWorldInstance.AccessType.Public;
        }
        
        public static string GetAccessName(ApiWorldInstance.AccessType accessType)
        {
            // Switch expression. yes c# 8 works in MelonLoader as it compiles differently
            return accessType switch
                {
                    ApiWorldInstance.AccessType.Public => "Public",
                    ApiWorldInstance.AccessType.FriendsOfGuests => "Friends+",
                    ApiWorldInstance.AccessType.FriendsOnly => "Friends Only",
                    ApiWorldInstance.AccessType.InviteOnly => "Invite Only",
                    ApiWorldInstance.AccessType.InvitePlus => "Invite+",
                    ApiWorldInstance.AccessType.Counter => "Coun... wait wut?",
                    _ => throw new ArgumentOutOfRangeException(nameof(accessType), accessType, "what the fuck happened?")
                };
        }

        private static DeleteNotificationDelegate GetDeleteNotificationDelegate
        {
            get
            {
                if (ourDeleteNotificationDelegate != null) return ourDeleteNotificationDelegate;

                MethodInfo deleteMethod = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.XRefScanFor("voteToKick") && m.XRefScanForMethod(null, nameof(VRCWebSocketsManager))
                                                     && m.XRefScanMethodCount(null, nameof(NotificationManager))
                                                     == 2);
                ourDeleteNotificationDelegate = (DeleteNotificationDelegate)Delegate.CreateDelegate(
                    typeof(DeleteNotificationDelegate),
                    NotificationManager.field_Private_Static_NotificationManager_0,
                    deleteMethod);
                return ourDeleteNotificationDelegate;
            }
        }

        private static ShowAlertDelegate GetShowAlertDelegate
        {
            get
            {
                if (ourShowAlertDelegate != null) return ourShowAlertDelegate;
                MethodInfo alertMethod = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                                  .First(m => m.GetParameters().Length == 3 && m.XRefScanFor("Popups/AlertPopup"));
                ourShowAlertDelegate = (ShowAlertDelegate)Delegate.CreateDelegate(
                    typeof(ShowAlertDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    alertMethod);
                return ourShowAlertDelegate;
            }
        }

        private static ShowPopupWindowBothDelegate GetShowPopupWindowBothDelegate
        {
            get
            {
                if (ourShowPopupWindowBothDelegate != null) return ourShowPopupWindowBothDelegate;
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 7 && m.XRefScanFor("Popups/StandardPopupV2"));

                ourShowPopupWindowBothDelegate = (ShowPopupWindowBothDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowBothDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowBothDelegate;
            }
        }

        private static ShowPopupWindowSingleDelegate GetShowPopupWindowSingleDelegate
        {
            get
            {
                if (ourShowPopupWindowSingleDelegate != null) return ourShowPopupWindowSingleDelegate;
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length == 5 && m.XRefScanFor("Popups/StandardPopupV2"));

                ourShowPopupWindowSingleDelegate = (ShowPopupWindowSingleDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowSingleDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowSingleDelegate;
            }
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

        public static void DeleteNotification(Notification notification)
        {
            GetDeleteNotificationDelegate(notification);
        }

        public static Notification GetCurrentActiveNotification()
        {
            return QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
        }

        public static Transform GetLocalPlayerTransform()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
        }
        
        public static void HideCurrentPopup()
        {
            GetVRCUiManager().HideScreen("POPUP");
        }
        public static void ShowAlert(string title, string content, float timeOut = 10f)
        {
            GetShowAlertDelegate(title, content, timeOut);
        }

        public static void ShowPopupWindow(
            string title,
            string content,
            string button1,
            System.Action action,
            string button2,
            System.Action action2,
            System.Action<VRCUiPopup> onCreated = null)
        {
            GetShowPopupWindowBothDelegate(title, content, button1, action, button2, action2, onCreated);
        }

        public static void ShowPopupWindow(string title, string content, string button1, System.Action action, System.Action<VRCUiPopup> onCreated = null)
        {
            GetShowPopupWindowSingleDelegate(title, content, button1, action, onCreated);
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global
                        && xref.ReadAsObject()?.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool HasParameters(this MethodBase methodBase, params Type[] types)
        {
            ParameterInfo[] parameters = methodBase.GetParameters();
            int typesLength = types.Length;
            if (parameters.Length < typesLength) return false;

            for (var i = 0; i < typesLength; ++i)
                if (parameters[i].ParameterType != types[i])
                    return false;

            return true;
        }

        private static bool XRefScanForMethod(this MethodBase methodBase, string methodName = null, string parentType = null, bool ignoreCase = true)
        {
            if (!string.IsNullOrEmpty(methodName)
                || !string.IsNullOrEmpty(parentType))
                return XrefScanner.XrefScan(methodBase).Any(
                    xref =>
                        {
                            if (xref.Type != XrefType.Method) return false;

                            var found = false;
                            MethodBase resolved = xref.TryResolve();
                            if (resolved == null) return false;

                            if (!string.IsNullOrEmpty(methodName))
                                found = !string.IsNullOrEmpty(resolved.Name) && resolved.Name.IndexOf(
                                            methodName,
                                            ignoreCase
                                                ? StringComparison.OrdinalIgnoreCase
                                                : StringComparison.Ordinal) >= 0;

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name) && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase
                                                ? StringComparison
                                                    .OrdinalIgnoreCase
                                                : StringComparison.Ordinal)
                                        >= 0;

                            return found;
                        });
            MelonLogger.LogWarning($"XRefScanForMethod \"{methodBase}\" has all null/empty parameters. Returning false");
            return false;
        }

        private static int XRefScanMethodCount(this MethodBase methodBase, string methodName = null, string parentType = null, bool ignoreCase = true)
        {
            if (!string.IsNullOrEmpty(methodName)
                || !string.IsNullOrEmpty(parentType))
                return XrefScanner.XrefScan(methodBase).Count(
                    xref =>
                        {
                            if (xref.Type != XrefType.Method) return false;

                            var found = false;
                            MethodBase resolved = xref.TryResolve();
                            if (resolved == null) return false;

                            if (!string.IsNullOrEmpty(methodName))
                                found = !string.IsNullOrEmpty(resolved.Name) && resolved.Name.IndexOf(
                                            methodName,
                                            ignoreCase
                                                ? StringComparison.OrdinalIgnoreCase
                                                : StringComparison.Ordinal) >= 0;

                            if (!string.IsNullOrEmpty(parentType))
                                found = !string.IsNullOrEmpty(resolved.ReflectedType?.Name) && resolved.ReflectedType.Name.IndexOf(
                                            parentType,
                                            ignoreCase
                                                ? StringComparison
                                                    .OrdinalIgnoreCase
                                                : StringComparison.Ordinal)
                                        >= 0;

                            return found;
                        });
            MelonLogger.LogWarning($"XRefScanMethodCount \"{methodBase}\" has all null/empty parameters. Returning -1");
            return -1;
        }

    }

}