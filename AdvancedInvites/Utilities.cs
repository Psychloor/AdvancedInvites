namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC;
    using VRC.Core;
    using VRC.SDKBase;

    public static class Utilities
    {
        
        // don't use this in your client if used for teleporting/moving your position. you'll get earraped
        // perfectly fine for closing ui in menues without that
        public static void CloseUi()
        {
            foreach (MethodInfo methodInfo in typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                m =>
                    {
                        if (!m.Name.StartsWith("Method_Public_Void_Boolean_")) return false;
                        return m.GetParameters().Length == 1 && m.GetParameters()[0].HasDefaultValue;
                    }))
                try
                {
                    methodInfo.Invoke(VRCUiManager.prop_VRCUiManager_0, new object[] { false });
                }
                catch
                {
                    // ignored
                }
        }

        private delegate void ShowPopupWindowBothDelegate(string title, string content, string button1, Il2CppSystem.Action action, string button2, Il2CppSystem.Action action2, Il2CppSystem.Action<VRCUiPopup> onCreated = null);

        private static ShowPopupWindowBothDelegate ourShowPopupWindowBothDelegate;

        private static ShowPopupWindowBothDelegate GetShowPopupWindowBothDelegate
        {
            get
            {
                if (ourShowPopupWindowBothDelegate != null) return ourShowPopupWindowBothDelegate;
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(
                    m => m.GetParameters().Length >= 6 && m.XRefScanFor("Popups/StandardPopupV2"));

                ourShowPopupWindowBothDelegate = (ShowPopupWindowBothDelegate)Delegate.CreateDelegate(
                    typeof(ShowPopupWindowBothDelegate),
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0,
                    popupV2Method);
                return ourShowPopupWindowBothDelegate;
            }
        }

        public static void ShowPopupWindow(string title, string content, string button1, Action action, string button2, Action action2, Action<VRCUiPopup> onCreated = null)
        {
            GetShowPopupWindowBothDelegate(title, content, button1, action, button2, action2, onCreated);
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global && xref.ReadAsObject()?.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static Transform GetLocalPlayerTransform()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
        }

        public static Notification GetCurrentActiveNotification()
        {
            return QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
        }

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts);

        private static CreatePortalDelegate ourCreatePortalDelegate;

        private static CreatePortalDelegate GetCreatePortalDelegate
        {
            get
            {
                if (ourCreatePortalDelegate != null) return ourCreatePortalDelegate;
                
                var portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static).First(
                    m => m.ReturnType == typeof(bool) && m.GetParameters().Length == 5 && m.GetParameters()[0].ParameterType == typeof(ApiWorld)
                         && m.XRefScanFor("admin_dont_allow_portal"));

                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }
        
        // return value might be used later once i figure out delete notification method
        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
           return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

    }

}