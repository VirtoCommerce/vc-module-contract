using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Contracts.Core
{
    [ExcludeFromCodeCoverage]
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

        public static class ContractStatuses
        {
            public const string Draft = "Draft";
            public const string PendingApproval = "Pending Approval";
            public const string Active = "Active";
            public const string Expired = "Expired";
            public const string Terminated = "Terminated";
            public const string Suspended = "Suspended";
            public const string Cancelled = "Cancelled";
        }

        public static class Settings
        {
            public static class General
            {
                public static SettingDescriptor ContractStatus = new SettingDescriptor
                {
                    Name = "Contract.Status",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Contracts|General",
                    IsDictionary = true,
                    AllowedValues = new object[]
                    {
                        ContractStatuses.Draft,
                        ContractStatuses.PendingApproval,
                        ContractStatuses.Active,
                        ContractStatuses.Expired,
                        ContractStatuses.Terminated,
                        ContractStatuses.Suspended,
                        ContractStatuses.Cancelled,
                    }
                };

                public static IEnumerable<SettingDescriptor> AllGeneralSettings
                {
                    get
                    {
                        yield return ContractStatus;
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
