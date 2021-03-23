namespace AdvancedInvites
{

    using System;
    using System.IO;
    using System.Text;

    using MelonLoader;

    using Newtonsoft.Json;

    public static class Localization
    {

        private const string FilePath = "UserData/AdvancedInvites/Localization.json";
        
        private static LocalizedText localizedText;

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

            try
            {
                localizedText = JsonConvert.DeserializeObject<LocalizedText>(
                    File.ReadAllText(FilePath, Encoding.UTF8),
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error Loading Localization, loading defaults:\n"+e);
                localizedText = new LocalizedText();
            }
        }

        public static string GetTitle(string username, string worldName, string instanceType)
        {
            return localizedText.Title.Replace("@Username", username).Replace("@WorldName", worldName).Replace("@InstanceType", instanceType);
        }

        public static string GetPublicPopup(string username,string worldName, string instanceType)
        {
            return localizedText.PublicPopup.Replace("@Username", username).Replace("@WorldName", worldName).Replace("@InstanceType", instanceType);
        }
        
        public static string GetPrivatePopup(string username,string worldName, string instanceType)
        {
            return localizedText.PrivatePopup.Replace("@Username", username).Replace("@WorldName", worldName).Replace("@InstanceType", instanceType);
        }

        public static string GetJoinButton() => localizedText.JoinButton;
        public static string GetDropPortalButton() => localizedText.DropPortalButton;
        
        private class LocalizedText
        {

            [JsonProperty(Order = 0)]
            public string Title = "Invitation from @Username";

            [JsonProperty(Order = 1)]
            public string PublicPopup =
                "You have officially been invited to: @WorldName\nInstance Type: @InstanceType\n\nWanna go by yourself or drop a portal for the lads?";

            [JsonProperty(Order = 2)]
            public string PrivatePopup =
                "You have officially been invited to: @WorldName\nInstance Type: @InstanceType\n\nPrivate Instance so can't drop a portal";

            [JsonProperty(Order = 3)]
            public string JoinButton = "Join Yourself";

            [JsonProperty(Order = 4)]
            public string DropPortalButton = "Drop Portal";

        }

    }

}