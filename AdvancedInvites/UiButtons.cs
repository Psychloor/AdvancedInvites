namespace AdvancedInvites
{

    using MelonLoader;

    using UIExpansionKit.API;

    using UnityEngine;

    using VRC.Core;
    using VRC.UI;

    public static class UiButtons
    {

        private static APIUser CurrentSelectedUser => QuickMenu.prop_QuickMenu_0.field_Private_APIUser_0;

        private static APIUser CurrentSocialUser => Object.FindObjectOfType<PageUserInfo>()?.user;

        public static void Initialize()
        {
            // Quickmenu
            ICustomLayoutedMenu userQuickMenuPage = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu);
            userQuickMenuPage.AddSimpleButton("AdvancedInvites\nBlacklist", () => BlacklistUser(CurrentSelectedUser));
            userQuickMenuPage.AddSimpleButton("AdvancedInvites\nWhitelist", () => WhitelistUser(CurrentSelectedUser));

            // Social menu
            ICustomLayoutedMenu userDetailsMenuPage = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu);
            userDetailsMenuPage.AddSimpleButton("AdvancedInvites\nBlacklist", () => BlacklistUser(CurrentSocialUser));
            userDetailsMenuPage.AddSimpleButton("AdvancedInvites\nWhitelist", () => WhitelistUser(CurrentSocialUser));

            // Worlds menu
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.WorldDetailsMenu).AddSimpleButton("AdvancedInvites\nBlacklist", BlacklistWorld);
        }

        private static void BlacklistWorld()
        {
            ApiWorld currentWorld = Object.FindObjectOfType<PageWorldInfo>()?.field_Private_ApiWorld_0;
            if (currentWorld == null) return;

            if (WorldPermissionHandler.IsBlacklisted(currentWorld.id))
            {
                WorldPermissionHandler.RemoveFromBlacklist(currentWorld);
                MelonLogger.Log($"{currentWorld.name} removed from blacklist");
                Utilities.QueueHudMessage($"{currentWorld.name} removed from blacklist");
            }
            else
            {
                WorldPermissionHandler.AddToBlacklist(currentWorld);
                MelonLogger.Log($"{currentWorld.name} added to blacklist");
                Utilities.QueueHudMessage($"{currentWorld.name} added to blacklist");
            }

            WorldPermissionHandler.SaveSettings();
        }

        private static void BlacklistUser(APIUser user)
        {
            if (user == null) return;

            if (UserPermissionHandler.IsBlacklisted(user.id))
            {
                UserPermissionHandler.RemoveFromBlacklist(user);
                MelonLogger.Log($"{user.displayName} removed from blacklist");
                Utilities.QueueHudMessage($"{user.displayName} removed from blacklist");
            }
            else
            {
                if (UserPermissionHandler.IsWhitelisted(user.id)) UserPermissionHandler.RemoveFromWhitelist(user);
                UserPermissionHandler.AddToBlacklist(user);
                MelonLogger.Log($"{user.displayName} added to blacklist");
                Utilities.QueueHudMessage($"{user.displayName} added to blacklist");
            }

            UserPermissionHandler.SaveSettings();
        }

        private static void WhitelistUser(APIUser user)
        {
            if (user == null) return;

            if (UserPermissionHandler.IsWhitelisted(user.id))
            {
                UserPermissionHandler.RemoveFromWhitelist(user);
                MelonLogger.Log($"{user.displayName} removed from whitelist");
                Utilities.QueueHudMessage($"{user.displayName} removed from whitelist");
            }
            else
            {
                if (UserPermissionHandler.IsBlacklisted(user.id)) UserPermissionHandler.RemoveFromBlacklist(user);
                UserPermissionHandler.AddToWhitelist(user);
                MelonLogger.Log($"{user.displayName} added to whitelist");
                Utilities.QueueHudMessage($"{user.displayName} added to whitelist");
            }

            UserPermissionHandler.SaveSettings();
        }

    }

}