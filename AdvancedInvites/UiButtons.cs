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

        private static APIUser CurrentSocialUser => Object.FindObjectOfType<PageUserInfo>()?.field_Private_APIUser_0;

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

            // Settings Menu
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton(
                "AdvancedInvites\nReload Sounds",
                () => MelonCoroutines.Start(SoundPlayer.LoadNotificationSounds()));
        }

        private static void ShowUsers()
        {
            ICustomShowableLayoutedMenu userPermissionsPopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            if (UserPermissionHandler.BlacklistedUsers.Count > 0)
            {
                userPermissionsPopup.AddLabel("Blacklisted Users");
                foreach (UserPermissionHandler.PermissionEntry blacklistedUser in UserPermissionHandler.BlacklistedUsers)
                    userPermissionsPopup.AddSimpleButton(
                        blacklistedUser.DisplayName,
                        () =>
                            {
                                userPermissionsPopup.Hide();
                                UserPermissionHandler.RemoveFromBlacklist(blacklistedUser.UserId);
                                UserPermissionHandler.SaveSettings();
                                ShowUsers();
                            });

                userPermissionsPopup.AddSpacer();
            }

            if (UserPermissionHandler.WhitelistedUsers.Count > 0)
            {
                userPermissionsPopup.AddLabel("Whitelisted Users");
                foreach (UserPermissionHandler.PermissionEntry whitelistedUser in UserPermissionHandler.WhitelistedUsers)
                    userPermissionsPopup.AddSimpleButton(
                        whitelistedUser.DisplayName,
                        () =>
                            {
                                userPermissionsPopup.Hide();
                                UserPermissionHandler.RemoveFromWhitelist(whitelistedUser.UserId);
                                UserPermissionHandler.SaveSettings();
                                ShowUsers();
                            });

                userPermissionsPopup.AddSpacer();
            }

            userPermissionsPopup.AddSimpleButton("Close", () => userPermissionsPopup.Hide());
            userPermissionsPopup.Show();
        }

        private static void ShowWorlds()
        {
            ICustomShowableLayoutedMenu worldsPermissionsPopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            if (WorldPermissionHandler.BlacklistedWorlds.Count > 0)
            {
                worldsPermissionsPopup.AddLabel("Blacklisted Worlds");
                foreach (WorldPermissionHandler.PermissionEntry blacklistedWorld in WorldPermissionHandler.BlacklistedWorlds)
                    worldsPermissionsPopup.AddSimpleButton(
                        blacklistedWorld.WorldName,
                        () =>
                            {
                                worldsPermissionsPopup.Hide();
                                WorldPermissionHandler.RemoveFromBlacklist(blacklistedWorld.WorldId);
                                WorldPermissionHandler.SaveSettings();
                                ShowWorlds();
                            });

                worldsPermissionsPopup.AddSpacer();
            }

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
                MelonLogger.Msg($"{currentWorld.name} removed from blacklist");
                Utilities.QueueHudMessage($"{currentWorld.name} removed from blacklist");
            }
            else
            {
                WorldPermissionHandler.AddToBlacklist(currentWorld);
                MelonLogger.Msg($"{currentWorld.name} added to blacklist");
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
                MelonLogger.Msg($"{user.displayName} removed from blacklist");
                Utilities.QueueHudMessage($"{user.displayName} removed from blacklist");
            }
            else
            {
                if (UserPermissionHandler.IsWhitelisted(user.id)) UserPermissionHandler.RemoveFromWhitelist(user.id);
                UserPermissionHandler.AddToBlacklist(user);
                MelonLogger.Msg($"{user.displayName} added to blacklist");
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
                MelonLogger.Msg($"{user.displayName} removed from whitelist");
                Utilities.QueueHudMessage($"{user.displayName} removed from whitelist");
            }
            else
            {
                if (UserPermissionHandler.IsBlacklisted(user.id)) UserPermissionHandler.RemoveFromBlacklist(user.id);
                UserPermissionHandler.AddToWhitelist(user);
                MelonLogger.Msg($"{user.displayName} added to whitelist");
                Utilities.QueueHudMessage($"{user.displayName} added to whitelist");
            }

            UserPermissionHandler.SaveSettings();
        }

    }

}