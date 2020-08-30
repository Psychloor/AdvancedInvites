namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    using UnityEngine;

    using VRC.Core;

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
        
        private static int showPopWindowBothIndex = -1;
        public static void ShowPopupWindow(string title, string content, string button1, Action action, string button2, Action action2)
        {
            if (showPopWindowBothIndex == -1)
            {
                MethodInfo popupV2Method = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(
                    m => m.Name.StartsWith(
                             "Method_Public_Void_String_String_String_Action_String_Action_Action_1_VRCUiPopup")
                         && m.XRefScanFor("StandardPopupV2"));
                if (popupV2Method.Name.IndexOf("VRCUiPopup_0", StringComparison.OrdinalIgnoreCase) >= 0) showPopWindowBothIndex = 0;
                else if (popupV2Method.Name.IndexOf("VRCUiPopup_1", StringComparison.OrdinalIgnoreCase) >= 0) showPopWindowBothIndex = 1;
            }

            switch (showPopWindowBothIndex)
            {
                case 0:
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_String_Action_Action_1_VRCUiPopup_0(
                        title,
                        content,
                        button1,
                        action,
                        button2,
                        action2);
                    break;
                
                case 1:
                    VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_String_Action_Action_1_VRCUiPopup_1(
                        title,
                        content,
                        button1,
                        action,
                        button2,
                        action2);
                    break;
            }
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global && xref.ReadAsObject() != null && xref
                                                                                       .ReadAsObject().ToString().IndexOf(
                                                                                           searchTerm,
                                                                                           StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static Transform GetLocalPlayerTransform()
        {
            return VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
        }

        public static Notification GetCurrentActiveNotification()
        {
            return QuickMenu.prop_QuickMenu_0.field_Private_Notification_0;
        }
        
        // return value might be used later once i figure out delete notification method
        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return PortalInternal.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_Vector3_Vector3_Boolean_0(
                apiWorld,
                apiWorldInstance,
                position,
                forward,
                showAlerts);
        }

    }

}