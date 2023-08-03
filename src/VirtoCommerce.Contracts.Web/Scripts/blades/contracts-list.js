angular.module('Contracts')
    .controller('Contracts.contractsListController',
        ['$scope', '$localStorage', 'Contracts.api', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper',
            function ($scope, $localStorage, contracts, bladeUtils, dialogService, gridOptionExtension, uiGridHelper) {
                var blade = $scope.blade;
                blade.headIcon = 'fas fa-file-contract';
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
                            $scope.deleteList($scope.gridApi.selection.getSelectedRows());
                        },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'Contract:delete'
                    }
                ];

                // filtering 
                var filter = blade.filter = $scope.filter = {};
                $scope.$localStorage = $localStorage;
                if (!$localStorage.contractSearchFilters) {
                    $localStorage.contractSearchFilters = [{ name: 'Contract.blades.contracts-list.labels.new-filter' }];
                }
                if ($localStorage.contractSearchFilterId) {
                    filter.current = _.findWhere($localStorage.contractSearchFilters, { id: $localStorage.contractSearchFilterId });
                }

                filter.change = function () {
                    $localStorage.contractSearchFilterId = filter.current ? filter.current.id : null;
                    if (filter.current && !filter.current.id) {
                        filter.current = null;
                        showFilterDetailBlade({ isNew: true });
                    } else {
                        bladeNavigationService.closeBlade({ id: 'filterDetail' });
                        filter.criteriaChanged();
                    }
                };

                filter.edit = function () {
                    if (filter.current) {
                        showFilterDetailBlade({ data: filter.current });
                    }
                };

                function showFilterDetailBlade(bladeData) {
                    var newBlade = {
                        id: 'filterDetail',
                        controller: 'Contracts.filterDetailController',
                        template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/filter-detail.tpl.html'
                    };
                    angular.extend(newBlade, bladeData);
                    bladeNavigationService.showBlade(newBlade, blade);
                }

                filter.criteriaChanged = function () {
                    if ($scope.pageSettings.currentPage > 1) {
                        $scope.pageSettings.currentPage = 1;
                    } else {
                        blade.refresh();
                    }
                };

                function getSearchCriteria() {
                    return {
                        keyword: filter.keyword ? filter.keyword : undefined,
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };
                }

                blade.refresh = function () {
                    var searchCriteria = getSearchCriteria();

                    if (filter.current) {
                        angular.extend(searchCriteria, filter.current);
                    }

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

                $scope.deleteList = function (selection) {
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
