namespace AdvancedInvites
{

    using System.Linq;
    using System.Reflection;

    using Il2CppSystem;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC.Core;

    using Delegate = System.Delegate;
    using StringComparison = System.StringComparison;
    using Type = System.Type;

    public static class Utilities
    {

        private static MethodInfo[] closeUiMethods;

        private static CreatePortalDelegate ourCreatePortalDelegate;

        private static HideNotificationDelegate ourHideNotificationDelegate;

        private static RemoveNotificationDelegate ourRemoveNotificationDelegate;

        private static ShowAlertDelegate ourShowAlertDelegate;

        private static ShowPopupWindowBothDelegate ourShowPopupWindowBothDelegate;

        private static ShowPopupWindowSingleDelegate ourShowPopupWindowSingleDelegate;

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts);

        private delegate void HideNotificationDelegate(Notification notification);

        private delegate void RemoveNotificationDelegate(Notification notification, NotificationManager.EnumNPublicSealedvaAlReLo4vUnique timeEnum);

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

        private static HideNotificationDelegate GetHideNotificationDelegate
        {
            get
            {
                if (ourHideNotificationDelegate != null) return ourHideNotificationDelegate;

                MethodInfo method = typeof(VRCWebSocketsManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => !m.IsAbstract && !m.IsVirtual && m.GetParameters().Length == 1
                         && m.GetParameters()[0].ParameterType == typeof(Notification)
                         && m.XRefScanFor("HideNotification"));

                ourHideNotificationDelegate = (HideNotificationDelegate)Delegate.CreateDelegate(
                    typeof(HideNotificationDelegate),
                    VRCWebSocketsManager.field_Private_Static_VRCWebSocketsManager_0,
                    method);

                return ourHideNotificationDelegate;
            }
        }

        private static RemoveNotificationDelegate GetRemoveNotificationDelegate
        {
            get
            {
                if (ourRemoveNotificationDelegate != null) return ourRemoveNotificationDelegate;
                MethodInfo method = typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => !m.IsAbstract && !m.IsVirtual && m.GetParameters().Length == 2
                         && m.GetParameters()[0].ParameterType == typeof(Notification)
                         && m.GetParameters()[1].ParameterType.IsEnum);

                ourRemoveNotificationDelegate = (RemoveNotificationDelegate)Delegate.CreateDelegate(
                    typeof(RemoveNotificationDelegate),
                    NotificationManager.field_Private_Static_NotificationManager_0,
                    method);

                return ourRemoveNotificationDelegate;
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

        // don't use this in your client if used for teleporting/moving your position. you'll get earraped
        // perfectly fine for closing ui in menues without that
        public static void CloseUi()
        {
            // if null grab methods
            closeUiMethods ??= typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                m =>
                    {
                        if (m.IsAbstract
                            || m.IsVirtual) return false;
                        if (m.ReturnType != typeof(void)) return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool)
                                                      && parameters[0].HasDefaultValue;
                    }).ToArray();

            foreach (MethodInfo methodInfo in closeUiMethods)
                try
                {
                    methodInfo.Invoke(VRCUiManager.prop_VRCUiManager_0, new object[] { false });
                }
                catch
                {
                    // ignored
                }
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

        public static void DeleteNotification(Notification notification)
        {
            if (!notification.notificationType.Equals("voteToKick", StringComparison.OrdinalIgnoreCase)) GetHideNotificationDelegate(notification);

            // Currently not working
            GetRemoveNotificationDelegate(notification, NotificationManager.EnumNPublicSealedvaAlReLo4vUnique.AllTime);
            GetRemoveNotificationDelegate(notification, NotificationManager.EnumNPublicSealedvaAlReLo4vUnique.Recent);
        }

        public static Notification GetCurrentActiveNotification()
        {
            return QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
        }

        public static Transform GetLocalPlayerTransform()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
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

    }

}