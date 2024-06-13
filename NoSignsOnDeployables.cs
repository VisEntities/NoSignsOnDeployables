using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Signs On Deployables", "VisEntities", "1.0.0")]
    [Description("Prevents placing wooden signs on certain deployables.")]
    public class NoSignsOnDeployables : RustPlugin
    {
        #region Fields

        private static NoSignsOnDeployables _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Deployable Short Prefab Names")]
            public List<string> DeployableShortPrefabNames { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                DeployableShortPrefabNames = new List<string>
                {
                    "locker.deployed",
                    "woodbox_deployed",
                    "box.wooden.large",
                    "furnace",
                    "workbench1.deployed",
                    "workbench2.deployed",
                    "workbench3.deployed",
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null)
                return null;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return null;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return null;

            Item activeItem = player.GetActiveItem();
            if (activeItem == null || !activeItem.info.shortname.Contains("sign.wooden"))
                return null;

            if (target.entity != null && _config.DeployableShortPrefabNames.Contains(target.entity.ShortPrefabName))
            {
                SendMessage(player, Lang.CannotPlaceSign);
                return true;
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "nosignsondeployables.ignore";
            private static readonly List<string> _permissions = new List<string>
            {
                IGNORE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions

        #region Localization

        private class Lang
        {
            public const string CannotPlaceSign = "CannotPlaceSign";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotPlaceSign] = "You cannot place signs on this entity."
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}