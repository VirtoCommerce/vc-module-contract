using System;
using FluentValidation;
using GraphQL.Server;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Contracts.Core;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Contracts.Data.Handlers;
using VirtoCommerce.Contracts.Data.MySql;
using VirtoCommerce.Contracts.Data.PostgreSql;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Contracts.Data.Services;
using VirtoCommerce.Contracts.Data.SqlServer;
using VirtoCommerce.Contracts.Data.Validation;
using VirtoCommerce.Contracts.ExperienceApi;
using VirtoCommerce.Contracts.ExperienceApi.Authorization;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Events;
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
            serviceCollection.AddDbContext<ContractDbContext>(options =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString);
                        break;
                }
            });


            serviceCollection.AddTransient<IContractRepository, ContractRepository>();
            serviceCollection.AddTransient<Func<IContractRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IContractRepository>());

            // Register services
            serviceCollection.AddTransient<IContractService, ContractService>();
            serviceCollection.AddTransient<IContractSearchService, ContractSearchService>();

            // register search service like a factory to avoid circular dependencies errors
            serviceCollection.AddTransient<Func<IContractSearchService>>(provider => ()
                => provider.CreateScope().ServiceProvider.GetRequiredService<IContractSearchService>());

            serviceCollection.AddTransient<IContractMembersService, ContractMembersService>();
            serviceCollection.AddTransient<IContractMembersSearchService, ContractMembersService>();

            serviceCollection.AddTransient<IContractPricesService, ContractPricesService>();

            serviceCollection.AddTransient<DeleteContractHandler>();

            // Validation services
            serviceCollection.AddTransient<AbstractValidator<Contract>, ContractValidator>();

            // GraphQL
            var assemblyMarker = typeof(AssemblyMarker);
            var graphQlBuilder = new CustomGraphQLBuilder(serviceCollection);
            graphQlBuilder.AddGraphTypes(assemblyMarker);
            serviceCollection.AddMediatR(assemblyMarker);
            serviceCollection.AddAutoMapper(assemblyMarker);
            serviceCollection.AddSchemaBuilders(assemblyMarker);
            serviceCollection.AddSingleton<IAuthorizationHandler, ContractAuthorizationHandler>();
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
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Contract", ModuleConstants.Security.Permissions.AllPermissions);

            // Apply migrations
            using var serviceScope = serviceProvider.CreateScope();
            using var dbContext = serviceScope.ServiceProvider.GetRequiredService<ContractDbContext>();
            dbContext.Database.Migrate();

            appBuilder.RegisterEventHandler<ContractChangedEvent, DeleteContractHandler>();
        }

        public void Uninstall()
        {
            // Method intentionally left empty
        }
    }
}
