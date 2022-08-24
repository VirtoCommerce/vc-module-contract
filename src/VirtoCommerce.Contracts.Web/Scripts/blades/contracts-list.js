angular.module('Contracts')
    .controller('Contracts.contractsListController',
        ['$scope', 'Contracts.webApi', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper',
            function ($scope, contracts, bladeUtils, dialogService, gridOptionExtension, uiGridHelper) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-list';
                blade.title = 'Contract.blades.contracts-list.title';

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var bladeNavigationService = bladeUtils.bladeNavigationService;

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.refresh",
                        icon: 'fa fa-refresh',
                        executeMethod: function () {
                            blade.refresh();
                        },
                        canExecuteMethod: function () {
                            return true;
                        }
                    },
                    {
                        name: "platform.commands.add",
                        icon: 'fas fa-plus',
                        executeMethod: createContract,
                        canExecuteMethod: function () {
                            return true;
                        },
                        permission: 'Contract:create'
                    },
                    {
                        name: "platform.commands.delete",
                        icon: 'fas fa-trash-alt',
                        executeMethod: function () {
                            deleteList($scope.gridApi.selection.getSelectedRows());
                        },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'Contract:delete'
                    }
                ];

                // filtering
                var filter = $scope.filter = {};

                filter.criteriaChanged = function () {
                    if ($scope.pageSettings.currentPage > 1) {
                        $scope.pageSettings.currentPage = 1;
                    } else {
                        blade.refresh();
                    }
                };

                function getSearchCriteria() {
                    return {
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };
                }

                blade.refresh = function () {
                    var searchCriteria = getSearchCriteria();

                    contracts.searchContracts(searchCriteria, function (data) {
                        blade.isLoading = false;

                        $scope.pageSettings.totalItems = data.totalCount;
                        $scope.listEntries = data.results ? data.results : [];
                    });
                };

                $scope.selectNode = function (contract) {
                    $scope.selectedNodeId = contract.id;
                    blade.selectedConctact = contract;
                    blade.editContract(contract);
                };

                function createContract() {
                    var newBlade = {
                        id: 'createContract',
                        isNew: true,
                        controller: 'Contracts.contractController',
                        template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-detail.html'
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                }


                blade.editContract = function (contract) {
                    var newBlade = {
                        id: 'editContract',
                        currentEntity: contract,
                        currentEntityId: contract.id,
                        controller: 'Contracts.contractController',
                        template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-detail.html'
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                };

                function deleteList(selection) {
                    var dialog = {
                        id: "confirmDeleteContracts",
                        title: "Contract.dialogs.notification-contract-delete.title",
                        message: "Contract.dialogs.notification-contract-delete.message",
                        callback: function (remove) {
                            if (remove) {
                                bladeNavigationService.closeChildrenBlades(blade, function () {
                                    var itemIds = _.pluck(selection, 'id');
                                    contracts.deleteContract({ ids: itemIds }, function () {
                                        blade.refresh();
                                    },
                                        function (error) {
                                            bladeNavigationService.setError('Error ' + error.status, blade);
                                        });
                                });
                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                }

                // ui-grid
                $scope.setGridOptions = function (gridId, gridOptions) {
                    $scope.gridOptions = gridOptions;
                    gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);

                    uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
                        uiGridHelper.bindRefreshOnSortChanged($scope);
                    });

                    gridOptions.onRegisterApi = function (gridApi) {
                        $scope.gridApi = gridApi;
                    };

                    bladeUtils.initializePagination($scope);

                    return gridOptions;
                };
            }]);
