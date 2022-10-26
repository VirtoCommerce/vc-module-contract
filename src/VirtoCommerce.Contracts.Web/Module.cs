using System;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Contracts.Core;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Contracts.Data.Handlers;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Contracts.Data.Services;
using VirtoCommerce.Contracts.Data.Validation;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Contracts.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            // Initialize database
            var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ??
                                   Configuration.GetConnectionString("VirtoCommerce");

            serviceCollection.AddDbContext<ContractDbContext>(options => options.UseSqlServer(connectionString));

            serviceCollection.AddTransient<IContractRepository, ContractRepository>();
            serviceCollection.AddTransient<Func<IContractRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IContractRepository>());

            // Register services
            serviceCollection.AddTransient<ICrudService<Contract>, ContractService>();
            serviceCollection.AddTransient<ISearchService<ContractSearchCriteria, ContractSearchResult, Contract>, ContractSearchService>();

            // register search service like a factory to avoid circular dependencies errors
            serviceCollection.AddTransient<Func<ISearchService<ContractSearchCriteria, ContractSearchResult, Contract>>>(provider => ()
                => provider.CreateScope().ServiceProvider.GetRequiredService<ISearchService<ContractSearchCriteria, ContractSearchResult, Contract>>());

            serviceCollection.AddTransient<IContractMembersService, ContractMembersService>();
            serviceCollection.AddTransient<IContractMembersSearchService, ContractMembersService>();

            serviceCollection.AddTransient<IContractPricesService, ContractPricesService>();

            serviceCollection.AddTransient<DeleteContractHandler>();

            // Validation services
            serviceCollection.AddTransient<AbstractValidator<Contract>, ContractValidator>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;

            // Register settings
            var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            // register dynamic properties
            var dynamicPropertyRegistrar = appBuilder.ApplicationServices.GetRequiredService<IDynamicPropertyRegistrar>();
            dynamicPropertyRegistrar.RegisterType<Contract>();

            // Register permissions
            var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions
                .Select(x => new Permission { ModuleId = ModuleInfo.Id, GroupName = "Contract", Name = x })
                .ToArray());

            // Apply migrations
            using var serviceScope = serviceProvider.CreateScope();
            using var dbContext = serviceScope.ServiceProvider.GetRequiredService<ContractDbContext>();
            dbContext.Database.Migrate();

            var inProcessBus = appBuilder.ApplicationServices.GetService<IHandlerRegistrar>();
            inProcessBus.RegisterHandler<ContractChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.GetService<DeleteContractHandler>().Handle(message));
        }

        public void Uninstall()
        {
            // Method intentionally left empty
        }
    }
}
