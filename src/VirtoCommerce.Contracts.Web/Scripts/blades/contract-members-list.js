angular.module('Contracts')
    .controller('Contracts.contractMembersListController',
        ['$scope', 'Contracts.membersApi', 'platformWebApp.dialogService', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'virtoCommerce.customerModule.memberTypesResolverService', 'platformWebApp.ui-grid.extension',
            function ($scope, contractMembersApi, dialogService, bladeUtils, uiGridHelper, memberTypesResolverService, gridOptionExtension) {
                var blade = $scope.blade;
                blade.title = 'Contract.blades.contract-members.title';
                blade.headIcon = 'fa fa-user __customers';
                blade.isNew = true;

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var bladeNavigationService = bladeUtils.bladeNavigationService;

                blade.refresh = function (parentRefresh) {
                    blade.isLoading = true;
                    var searchCriteria = getSearchCriteria();

                    if (blade.searchCriteria) {
                        angular.extend(searchCriteria, blade.searchCriteria);
                    }

                    contractMembersApi.searchContractMembers(searchCriteria,
                        function (data) {
                            blade.isLoading = false;
                            $scope.pageSettings.totalItems = data.totalCount;

                            // precalculate icon
                            _.each(data.results, function (x) {
                                var memberTypeDefinition = memberTypesResolverService.resolve(x.memberType);
                                if (memberTypeDefinition) {
                                    x._memberTypeIcon = memberTypeDefinition.icon;
                                }
                            });

                            $scope.listEntries = data.results ? data.results : [];
                        });

                    if (parentRefresh && blade.parentRefresh) {
                        blade.parentRefresh();
                    }
                };

                blade.showDetailBlade = function (listItem, isNew) {
                    blade.setSelectedNode(listItem);

                    var foundTemplate = memberTypesResolverService.resolve(listItem.memberType);
                    if (foundTemplate) {
                        var newBlade = angular.copy(foundTemplate.detailBlade);
                        newBlade.currentEntity = listItem;
                        newBlade.currentEntityId = listItem.id;
                        newBlade.isNew = isNew;
                        bladeNavigationService.showBlade(newBlade, blade);
                    } else {
                        dialogService.showNotificationDialog({
                            id: "error",
                            title: "customer.dialogs.unknown-member-type.title",
                            message: "customer.dialogs.unknown-member-type.message",
                            messageValues: { memberType: listItem.memberType },
                        });
                    }
                };

                $scope.delete = function (entity) {
                    deleteList([entity]);
                };

                function deleteList(selection) {
                    var dialog = {
                        id: "confirmDeleteContractMember",
                        title: "Contract.dialogs.notification-contract-member-delete.title",
                        message: "Contract.dialogs.notification-contract-member-delete.message",
                        callback: function (remove) {
                            if (remove) {
                                bladeNavigationService.closeChildrenBlades(blade, function () {
                                    blade.isLoading = true;
                                    var memberIds = _.pluck(selection, 'id');

                                    var deleteRequest = {
                                        contractId: blade.contract.id,
                                        memberIds: memberIds
                                    };

                                    contractMembersApi.deleteContractMembers(deleteRequest, function () {
                                        blade.refresh(true);
                                    }, function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
                                });
                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                }

                blade.setSelectedNode = function (listItem) {
                    $scope.selectedNodeId = listItem.id;
                };

                $scope.selectNode = function (listItem) {
                    blade.setSelectedNode(listItem);
                    blade.showDetailBlade(listItem);
                };

                $scope.openSelectBlade = function () {
                    var selection = [];

                    var options = {
                        selectedItemIds: selection,
                        checkItemCallback: function (listItem, isSelected) {
                            if (isSelected) {
                                var notContains = !_.find(selection, function (x) {
                                        return x === listItem.id;
                                    });

                                if (notContains) {
                                    selection.push(listItem.id);
                                }
                            }
                            else {
                                selection = _.reject(selection, function (x) {
                                        return x === listItem.id;
                                    });
                            }
                        },
                        pickExecutedCallback: function (selectBlade) {
                            blade.isLoading = true;

                            // create contract entry
                            var addRequest = {
                                contractId: blade.contract.id,
                                memberIds: selection
                            };

                            contractMembersApi.addContractMembers(addRequest, function () {
                                bladeNavigationService.closeBlade(selectBlade);
                                blade.refresh(true);
                            }, function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
                        }
                    };

                    var newBlade = {
                        id: 'contractMembersSelect',
                        currentEntity: { id: null },
                        contract: blade.contract,
                        controller: 'Contracts.memberSelectController',
                        template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/member-select-list.html',
                        options: options
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                }

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.refresh",
                        icon: 'fa fa-refresh',
                        executeMethod: blade.refresh,
                        canExecuteMethod: function () {
                            return true;
                        }
                    },
                    {
                        name: "platform.commands.add",
                        icon: 'fas fa-plus',
                        executeMethod: function () {
                            $scope.openSelectBlade();
                        },
                        canExecuteMethod: function () {
                            return true;
                        },
                        permission: 'Contracts:update'
                    },
                    {
                        name: "platform.commands.delete",
                        icon: 'fas fa-trash-alt',
                        executeMethod: function () { deleteList($scope.gridApi.selection.getSelectedRows()); },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'Contracts:delete'
                    }
                ];

                var filter = blade.filter = { keyword: null };
                filter.criteriaChanged = function () {
                    // temp fix to prevent double loading caused by vc-customer-search directive
                    if (blade.isNew) {
                        blade.isNew = false;
                        return;
                    }

                    if (filter.keyword === null) {
                        blade.memberType = undefined;
                    }
                    if ($scope.pageSettings.currentPage > 1) {
                        $scope.pageSettings.currentPage = 1;
                    } else {
                        blade.refresh();
                    }
                };

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

                function getSearchCriteria() {
                    return {
                        contractId: blade.contract.id,
                        keyword: filter.keyword ? filter.keyword : undefined,
                        deepSearch: true,
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };
                }
            }]);
