using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Contracts.Core
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string Access = "Contract:access";
                public const string Create = "Contract:create";
                public const string Read = "Contract:read";
                public const string Update = "Contract:update";
                public const string Delete = "Contract:delete";

                public static string[] AllPermissions { get; } =
                {
                    Access,
                    Create,
                    Read,
                    Update,
                    Delete,
                };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor ContractEnabled { get; } = new SettingDescriptor
                {
                    Name = "Contract.ContractEnabled",
                    GroupName = "Contract|General",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false,
                };

                public static SettingDescriptor ContractPassword { get; } = new SettingDescriptor
                {
                    Name = "Contract.ContractPassword",
                    GroupName = "Contract|Advanced",
                    ValueType = SettingValueType.SecureString,
                    DefaultValue = "qwerty",
                };

                public static IEnumerable<SettingDescriptor> AllGeneralSettings
                {
                    get
                    {
                        yield return ContractEnabled;
                        yield return ContractPassword;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    return General.AllGeneralSettings;
                }
            }
        }
    }
}
