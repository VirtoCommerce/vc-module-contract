// Call this to register your module to main application
var moduleName = 'Contract';

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
                                controller: 'Contract.contractsListController',
                                template: 'Modules/$(VirtoCommerce.Contract)/Scripts/blades/contracts-list.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state', 'platformWebApp.widgetService',
        function (mainMenuService, $state, widgetService) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/Contract',
                icon: 'fa fa-cube',
                title: 'Contract',
                priority: 100,
                action: function () { $state.go('workspace.ContractState'); },
                permission: 'Contract:access',
            };
            mainMenuService.addMenuItem(menuItem);

            widgetService.registerWidget({
                controller: 'Contract.contractAttachmentsWidgetController',
                template: 'Modules/$(VirtoCommerce.Contract)/Scripts/widgets/contract-attachments-widget.html'
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'Contract.contractCustomersWidgetController',
                template: 'Modules/$(VirtoCommerce.Contract)/Scripts/widgets/contract-customers-widget.html'
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'Contract.contractDynamicPropertiesWidgetController',
                template: 'Modules/$(VirtoCommerce.Contract)/Scripts/widgets/contract-dynamic-properties-widget.html'
            }, 'contractDetail');

            widgetService.registerWidget({
                controller: 'Contract.contractPricesWidgetController',
                template: 'Modules/$(VirtoCommerce.Contract)/Scripts/widgets/contract-prices-widget.html'
            }, 'contractDetail');
        }
    ]);
