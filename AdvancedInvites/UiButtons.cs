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

            // User Details menu
            ICustomLayoutedMenu userDetailsMenuPage = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu);
            userDetailsMenuPage.AddSimpleButton("AdvancedInvites\nBlacklist", () => BlacklistUser(CurrentSocialUser));
            userDetailsMenuPage.AddSimpleButton("AdvancedInvites\nWhitelist", () => WhitelistUser(CurrentSocialUser));

            // Social menu
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SocialMenu).AddSimpleButton("AdvancedInvites\nRemove...", ShowUsers);
            
            // Worlds Menu
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.WorldMenu).AddSimpleButton("AdvancedInvites\nRemove...", ShowWorlds);

            // World Details menu
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.WorldDetailsMenu).AddSimpleButton("AdvancedInvites\nBlacklist", BlacklistWorld);
        }

        private static void ShowUsers()
        {
            var userPermissionsPopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            userPermissionsPopup.AddLabel("Blacklisted Users");
            foreach (var blacklistedUser in UserPermissionHandler.BlacklistedUsers)
            {
                userPermissionsPopup.AddSimpleButton(
                    blacklistedUser.DisplayName,
                    () =>
                        {
                            userPermissionsPopup.Hide();
                            UserPermissionHandler.RemoveFromBlacklist(blacklistedUser.UserId);
                            UserPermissionHandler.SaveSettings();
                            ShowUsers();
                        });
            }
            
            userPermissionsPopup.AddSpacer();
            userPermissionsPopup.AddLabel("Whitelisted Users");
            foreach (var whitelistedUser in UserPermissionHandler.WhitelistedUsers)
            {
                userPermissionsPopup.AddSimpleButton(
                    whitelistedUser.DisplayName,
                    () =>
                        {
                            userPermissionsPopup.Hide();
                            UserPermissionHandler.RemoveFromWhitelist(whitelistedUser.UserId);
                            UserPermissionHandler.SaveSettings();
                            ShowUsers();
                        });
            }
            
            userPermissionsPopup.AddSpacer();
            userPermissionsPopup.AddSimpleButton("Close", () => userPermissionsPopup.Hide());
            userPermissionsPopup.Show();
        }

        private static void ShowWorlds()
        {
            var worldsPermissionsPopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            worldsPermissionsPopup.AddLabel("Blacklisted Worlds");
            foreach (var blacklistedWorld in WorldPermissionHandler.BlacklistedWorlds)
            {
                worldsPermissionsPopup.AddSimpleButton(
                    blacklistedWorld.WorldName,
                    () =>
                        {
                            worldsPermissionsPopup.Hide();
                            WorldPermissionHandler.RemoveFromBlacklist(blacklistedWorld.WorldId);
                            WorldPermissionHandler.SaveSettings();
                            ShowWorlds();
                        });
            }
            
            worldsPermissionsPopup.AddSpacer();
            worldsPermissionsPopup.AddSimpleButton("Close", () => worldsPermissionsPopup.Hide());
            worldsPermissionsPopup.Show();
        }

        private static void BlacklistWorld()
        {
            ApiWorld currentWorld = Object.FindObjectOfType<PageWorldInfo>()?.field_Private_ApiWorld_0;
            if (currentWorld == null) return;

            if (WorldPermissionHandler.IsBlacklisted(currentWorld.id))
            {
                WorldPermissionHandler.RemoveFromBlacklist(currentWorld.id);
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
                UserPermissionHandler.RemoveFromBlacklist(user.id);
                MelonLogger.Log($"{user.displayName} removed from blacklist");
                Utilities.QueueHudMessage($"{user.displayName} removed from blacklist");
            }
            else
            {
                if (UserPermissionHandler.IsWhitelisted(user.id)) UserPermissionHandler.RemoveFromWhitelist(user.id);
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
                UserPermissionHandler.RemoveFromWhitelist(user.id);
                MelonLogger.Log($"{user.displayName} removed from whitelist");
                Utilities.QueueHudMessage($"{user.displayName} removed from whitelist");
            }
            else
            {
                if (UserPermissionHandler.IsBlacklisted(user.id)) UserPermissionHandler.RemoveFromBlacklist(user.id);
                UserPermissionHandler.AddToWhitelist(user);
                MelonLogger.Log($"{user.displayName} added to whitelist");
                Utilities.QueueHudMessage($"{user.displayName} added to whitelist");
            }

            UserPermissionHandler.SaveSettings();
        }

    }

}