namespace AdvancedInvites
{

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    using VRC.Core;

    public static class WorldPermissionHandler
    {

        private const string BlacklistedPath = "UserData/AdvancedInvites/BlacklistedWorlds.json";

        internal static readonly List<PermissionEntry> BlacklistedWorlds = new List<PermissionEntry>();

        internal static bool IsBlacklisted(string worldId)
        {
            foreach (PermissionEntry blacklistedWorld in BlacklistedWorlds)
                if (blacklistedWorld.WorldId.Equals(worldId, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        internal static void AddToBlacklist(ApiWorld apiWorld)
        {
            if (IsBlacklisted(apiWorld.id)) return;
            BlacklistedWorlds.Add(new PermissionEntry { WorldName = apiWorld.name, WorldId = apiWorld.id });
        }

        internal static void RemoveFromBlacklist(string worldId)
        {
            if (!IsBlacklisted(worldId)) return;
            BlacklistedWorlds.RemoveAll(entry => entry.WorldId.Equals(worldId, StringComparison.OrdinalIgnoreCase));
        }

        internal static void LoadSettings()
        {
            if (!Directory.Exists("UserData")) Directory.CreateDirectory("UserData");
            if (!Directory.Exists("UserData/AdvancedInvites")) Directory.CreateDirectory("UserData/AdvancedInvites");

            if (!File.Exists(BlacklistedPath))
                File.WriteAllText(BlacklistedPath, "[]", Encoding.UTF8);

            JsonConvert.PopulateObject(
                File.ReadAllText(BlacklistedPath, Encoding.UTF8),
                BlacklistedWorlds,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
        }

        public static void SaveSettings()
        {
            File.WriteAllText(BlacklistedPath, JsonConvert.SerializeObject(BlacklistedWorlds, Formatting.Indented), Encoding.UTF8);
        }

        internal class PermissionEntry
        {

            [JsonProperty("WorldID")]
            public string WorldId;

            [JsonProperty("WorldName")]
            public string WorldName;

        }

    }

}