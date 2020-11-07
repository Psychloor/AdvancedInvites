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
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.UserQuickMenu, "AdvancedInvites Blacklist", () => BlacklistUser(CurrentSelectedUser));
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.UserQuickMenu, "AdvancedInvites Whitelist", () => WhitelistUser(CurrentSelectedUser));

            // Social menu
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.UserDetailsMenu, "AdvancedInvites Blacklist", () => BlacklistUser(CurrentSocialUser));
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.UserDetailsMenu, "AdvancedInvites Whitelist", () => WhitelistUser(CurrentSocialUser));
        }

        private static void BlacklistUser(APIUser user)
        {
            if (user == null) return;

            if (PermissionHandler.IsBlacklisted(user.id))
            {
                PermissionHandler.RemoveFromBlacklist(user);
                MelonLogger.Log($"{user.displayName} removed from blacklist");
                Utilities.QueueHudMessage($"{user.displayName} removed from blacklist");
            }
            else
            {
                if (PermissionHandler.IsWhitelisted(user.id)) PermissionHandler.RemoveFromWhitelist(user);
                PermissionHandler.AddToBlacklist(user);
                MelonLogger.Log($"{user.displayName} added to blacklist");
                Utilities.QueueHudMessage($"{user.displayName} added to blacklist");
            }

            PermissionHandler.SaveSettings();
        }

        private static void WhitelistUser(APIUser user)
        {
            if (user == null) return;

            if (PermissionHandler.IsWhitelisted(user.id))
            {
                PermissionHandler.RemoveFromWhitelist(user);
                MelonLogger.Log($"{user.displayName} removed from whitelist");
                Utilities.QueueHudMessage($"{user.displayName} removed from whitelist");
            }
            else
            {
                if (PermissionHandler.IsBlacklisted(user.id)) PermissionHandler.RemoveFromBlacklist(user);
                PermissionHandler.AddToWhitelist(user);
                MelonLogger.Log($"{user.displayName} added to whitelist");
                Utilities.QueueHudMessage($"{user.displayName} added to whitelist");
            }

            PermissionHandler.SaveSettings();
        }

    }

}