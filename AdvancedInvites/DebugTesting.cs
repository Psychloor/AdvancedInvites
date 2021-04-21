#if DEBUG
namespace AdvancedInvites
{

    using System.Collections.Generic;
    using System.Reflection;

    using MelonLoader;

    using UnhollowerRuntimeLib.XrefScans;

    internal static class DebugTesting
    {

        internal static void Test()
        {
            /*MelonLogger.Msg("Checking For Delete");
            foreach (MethodInfo methodInfo in typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (methodInfo.Name.StartsWith("Method_Public_Void")
                    && methodInfo.GetParameters().Length == 1
                    && methodInfo.GetParameters()[0].ParameterType == typeof(Notification))
                {
                    methodInfo.XrefDump();
                }
            }

            var deleteMethod = typeof(NotificationManager).GetMethod("Method_Public_Void_Notification_1", BindingFlags.Public | BindingFlags.Instance);
            MelonLogger.Msg("Types using delete");
            HashSet<string> collectedTypes = new HashSet<string>();
            foreach (XrefInstance instance in XrefScanner.UsedBy(deleteMethod))
            {
                if (instance.Type == XrefType.Method)
                {
                    var resolved = instance.TryResolve();
                    if (resolved == null) continue;
                    if (collectedTypes.Contains(resolved.DeclaringType?.ToString())) continue;
                    collectedTypes.Add(resolved.DeclaringType?.ToString());
                    MelonLogger.Msg(resolved.DeclaringType?.ToString());
                }
            }*/

            /*MelonLogger.Msg("Finding Streamermode");
            foreach (PropertyInfo property in typeof(VRCInputManager).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType == typeof(bool))
                {
                    property.GetSetMethod().XrefDump();
                }
            }*/
        }

        public static void DumpTypesUsedBy(this MethodBase methodBase)
        {
            if (methodBase == null) return;

            MelonLogger.Msg("Types used by method: " + methodBase.Name);
            var usedTypes = new HashSet<string>();
            foreach (XrefInstance instance in XrefScanner.UsedBy(methodBase))
                if (instance.Type == XrefType.Method)
                {
                    MethodBase resolved = instance.TryResolve();
                    if (resolved == null) continue;
                    if (usedTypes.Contains(resolved.DeclaringType?.ToString())) continue;
                    usedTypes.Add(resolved.DeclaringType?.ToString());
                    MelonLogger.Msg(resolved.DeclaringType?.ToString());
                }
        }

        internal static void XrefDump(this MethodBase methodBase)
        {
            MelonLogger.Msg("Scanning Method: " + methodBase.Name);
            foreach (XrefInstance instance in XrefScanner.XrefScan(methodBase))
                switch (instance.Type)
                {
                    case XrefType.Global:
                        MelonLogger.Msg($"\tGlobal Instance: {instance.ReadAsObject()?.ToString()}");
                        break;
                    case XrefType.Method:
                        MethodBase resolved = instance.TryResolve();
                        if (resolved == null)
                            MelonLogger.Msg("\tNull Method Instance");
                        else
                            MelonLogger.Msg($"\tMethod Instance: {resolved.DeclaringType?.Name} {resolved.Name}");
                        break;
                }

            MelonLogger.Msg("");
        }

    }

}
#endif