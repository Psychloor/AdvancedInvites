#if DEBUG
namespace AdvancedInvites
{

    using System;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using Transmtn.DTO.Notifications;

    using UnhollowerRuntimeLib.XrefScans;

    internal static class DebugTesting
    {

        internal static void Test()
        {
            MelonLogger.Msg("Checking For Delete");
            foreach (MethodInfo methodInfo in typeof(NotificationManager).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (methodInfo.Name.StartsWith("Method_Public_Void")
                    && methodInfo.GetParameters().Length == 1
                    && methodInfo.GetParameters()[0].ParameterType == typeof(Notification))
                {
                    methodInfo.XrefDump();
                }
            }
        }

        internal static void XrefDump(this MethodBase methodBase)
        {
            MelonLogger.Msg("Scanning Method: "+methodBase.Name);
            foreach (XrefInstance instance in XrefScanner.XrefScan(methodBase))
            {
                switch (instance.Type)
                {
                    case XrefType.Global:
                        MelonLogger.Msg($"\tGlobal Instance: {instance.ReadAsObject()?.ToString()}");
                        break;
                    case XrefType.Method:
                        var resolved = instance.TryResolve();
                        if (resolved == null)
                            MelonLogger.Msg("\tNull Method Instance");
                        else
                            MelonLogger.Msg($"\tMethod Instance: {resolved.DeclaringType?.Name} {resolved.Name}");
                        break;
                    default:
                        break;
                }
            }
            MelonLogger.Msg("");
        }

    }

}
#endif