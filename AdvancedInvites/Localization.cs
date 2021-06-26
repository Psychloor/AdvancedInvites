namespace AdvancedInvites
{

    using System;
    using System.IO;
    using System.Text;

    using Il2CppSystem.Text.RegularExpressions;

    using MelonLoader;

    using Newtonsoft.Json;

    public static class Localization
    {

        private const string FilePath = "UserData/AdvancedInvites/Localization.json";

        private static LocalizedText localizedText;

        private static Regex userRegex, worldRegex, instanceRegex, regionRegex;

        public static void Load()
        {
            localizedText = new LocalizedText();
            if (!Directory.Exists("UserData")) Directory.CreateDirectory("UserData");
            if (!Directory.Exists("UserData/AdvancedInvites")) Directory.CreateDirectory("UserData/AdvancedInvites");

            if (!File.Exists(FilePath))
            {
                MelonLogger.Msg("Localization File Not Found. Creating It");
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(localizedText, Formatting.Indented), Encoding.UTF8);
            }

            /*if (File.GetLastWriteTime(FilePath).Date < new DateTime(2021, 6, 23))
            {
                MelonLogger.Msg("Old localization found, resetting it");
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(localizedText, Formatting.Indented), Encoding.UTF8);
            }*/

            try
            {
                localizedText = JsonConvert.DeserializeObject<LocalizedText>(
                    File.ReadAllText(FilePath, Encoding.UTF8),
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Loading Localization, loading defaults:\n" + e);
                localizedText = new LocalizedText();
            }

            userRegex = new Regex("@Username", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            worldRegex = new Regex("@WorldName", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            instanceRegex = new Regex("@InstanceType", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            regionRegex = new Regex("@Region", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public static string GetTitle(string username, string worldName, string instanceType, string region)
        {
            return ReplaceAllTags(localizedText.Title, username, worldName, instanceType, region);
        }

        public static string GetPublicPopup(string username, string worldName, string instanceType, string region)
        {
            return ReplaceAllTags(localizedText.PublicPopup, username, worldName, instanceType, region);
        }

        public static string GetPrivatePopup(string username, string worldName, string instanceType, string region)
        {
            return ReplaceAllTags(localizedText.PrivatePopup, username, worldName, instanceType, region);
        }

        // Just because some people might forget it's Case-SensiTivE
        private static string ReplaceAllTags(string text, string username, string worldName, string instanceType, string region)
        {
            text = userRegex.Replace(text, username);
            text = worldRegex.Replace(text, worldName);
            text = regionRegex.Replace(text, region);
            return instanceRegex.Replace(text, instanceType);
        }

        public static string GetJoinButton()
        {
            return localizedText.JoinButton;
        }

        public static string GetDropPortalButton()
        {
            return localizedText.DropPortalButton;
        }

        private class LocalizedText
        {

            [JsonProperty(Order = 5)]
            public string DropPortalButton = "Drop Portal";

            [JsonProperty(Order = 0)]
            public string Info = @"Tags to use: @Username, @WorldName, @InstanceType, and @Region. for a new line type \n and if you need a tab then type \t";

            [JsonProperty(Order = 4)]
            public string JoinButton = "Join Yourself";

            [JsonProperty(Order = 3)]
            public string PrivatePopup =
                "You have officially been invited to:\n@WorldName\nInstance Type: @InstanceType\nRegion: @Region\nPrivate Instance so can't drop a portal";

            [JsonProperty(Order = 2)]
            public string PublicPopup =
                "You have officially been invited to:\n@WorldName\nInstance Type: @InstanceType\nRegion: @Region\nWanna go by yourself or drop a portal for the lads?";

            [JsonProperty(Order = 1)]
            public string Title = "Invitation from @Username";

        }

    }

}