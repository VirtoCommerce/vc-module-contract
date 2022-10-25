// Call this to register your module to main application
var moduleName = 'Contracts';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider',
        function ($stateProvider) {
            $stateProvider
                .state('workspace.ContractState', {
                    url: '/Contract',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        'platformWebApp.bladeNavigationService',
                        function (bladeNavigationService) {
                            var newBlade = {
                                id: 'conractList',
                                controller: 'Contracts.contractsListController',
                                template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contracts-list.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state', 'platformWebApp.widgetService', 'platformWebApp.authService',
        function (mainMenuService, $state, widgetService, authService) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/Contracts',
                icon: 'fas fa-file-contract',
                title: 'Contracts',
                priority: 100,
                action: function () { $state.go('workspace.ContractState'); },
                permission: 'Contract:access',
            };
            mainMenuService.addMenuItem(menuItem);

            widgetService.registerWidget({
                controller: 'Contracts.contractAttachmentsWidgetController',
                template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/widgets/contract-attachments-widget.html',
                isVisible: function (blade) { return !blade.isNew; }
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'Contracts.contractCustomersWidgetController',
                template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/widgets/contract-customers-widget.html',
                isVisible: function (blade) { return !blade.isNew; }
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'platformWebApp.dynamicPropertyWidgetController',
                template: '$(Platform)/Scripts/app/dynamicProperties/widgets/dynamicPropertyWidget.tpl.html',
                isVisible: function (blade) { return !blade.isNew && authService.checkPermission('platform:dynamic_properties:read'); }
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'Contracts.contractPricesWidgetController',
                template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/widgets/contract-prices-widget.html',
                isVisible: function (blade) { return !blade.isNew; }
            }, 'contractDetail');
        }
    ]);
