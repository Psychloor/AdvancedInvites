namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;

    using VRC.Core;

    public static class UserPermissionHandler
    {

        private const string BlacklistedPath = "UserData/AdvancedInvites/Blacklisted.json";

        private const string WhitelistedPath = "UserData/AdvancedInvites/Whitelisted.json";

        internal static readonly List<PermissionEntry> BlacklistedUsers = new List<PermissionEntry>();

        internal static readonly List<PermissionEntry> WhitelistedUsers = new List<PermissionEntry>();

        internal static bool IsBlacklisted(string userId)
        {
            foreach (PermissionEntry blacklistedUser in BlacklistedUsers)
                if (blacklistedUser.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        internal static bool IsWhitelisted(string userId)
        {
            foreach (PermissionEntry whitelistedUser in WhitelistedUsers)
                if (whitelistedUser.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        internal static void AddToBlacklist(APIUser apiUser)
        {
            if (IsBlacklisted(apiUser.id)) return;
            BlacklistedUsers.Add(new PermissionEntry { DisplayName = apiUser.displayName, UserId = apiUser.id });
        }

        internal static void RemoveFromBlacklist(string userId)
        {
            if (!IsBlacklisted(userId)) return;
            BlacklistedUsers.RemoveAll(entry => entry.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
        }

        internal static void AddToWhitelist(APIUser apiUser)
        {
            if (IsWhitelisted(apiUser.id)) return;
            WhitelistedUsers.Add(new PermissionEntry { DisplayName = apiUser.displayName, UserId = apiUser.id });
        }

        internal static void RemoveFromWhitelist(string userId)
        {
            if (!IsWhitelisted(userId)) return;
            WhitelistedUsers.RemoveAll(entry => entry.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
        }

        internal static void LoadSettings()
        {
            if (!Directory.Exists("UserData")) Directory.CreateDirectory("UserData");
            if (!Directory.Exists("UserData/AdvancedInvites")) Directory.CreateDirectory("UserData/AdvancedInvites");

            if (!File.Exists(BlacklistedPath))
                File.WriteAllText(BlacklistedPath, "[]", Encoding.UTF8);
            if (!File.Exists(WhitelistedPath))
                File.WriteAllText(WhitelistedPath, "[]", Encoding.UTF8);

            JsonConvert.PopulateObject(
                File.ReadAllText(BlacklistedPath, Encoding.UTF8),
                BlacklistedUsers,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

            JsonConvert.PopulateObject(
                File.ReadAllText(WhitelistedPath, Encoding.UTF8),
                WhitelistedUsers,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
        }

        public static void SaveSettings()
        {
            File.WriteAllText(BlacklistedPath, JsonConvert.SerializeObject(BlacklistedUsers, Formatting.Indented), Encoding.UTF8);
            File.WriteAllText(WhitelistedPath, JsonConvert.SerializeObject(WhitelistedUsers, Formatting.Indented), Encoding.UTF8);
        }

        internal class PermissionEntry
        {

            [JsonProperty("DisplayName")]
            public string DisplayName;

            [JsonProperty("UserID")]
            public string UserId;

        }

    }

}