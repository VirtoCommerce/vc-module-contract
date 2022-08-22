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
    .run(['platformWebApp.mainMenuService', '$state',
        function (mainMenuService, $state) {
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
        }
    ]);
