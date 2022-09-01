angular.module('Contracts')
    .controller('Contracts.memberSelectController',
        ['$scope', '$timeout', 'virtoCommerce.customerModule.members', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'uiGridConstants', 'virtoCommerce.customerModule.memberTypesResolverService', 'platformWebApp.ui-grid.extension',
            function ($scope, $timeout, members, bladeUtils, uiGridHelper, uiGridConstants, memberTypesResolverService, gridOptionExtension) {
                var blade = $scope.blade;
                blade.title = 'customer.blades.member-list.title';
                blade.headIcon = 'fa fa-user __customers';
                blade.isNew = true;

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                $scope.options = angular.extend({
                    selectedItemIds: []
                }, blade.options);

                var bladeNavigationService = bladeUtils.bladeNavigationService;

                blade.refresh = function (parentRefresh) {
                    blade.isLoading = true;
                    var searchCriteria = getSearchCriteria();

                    if (blade.searchCriteria) {
                        angular.extend(searchCriteria, blade.searchCriteria);
                    }

                    members.search(searchCriteria,
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

                            //Set navigation breadcrumbs
                            setBreadcrumbs();
                        });

                    if (parentRefresh && blade.parentRefresh) {
                        blade.parentRefresh();
                    }
                };

                //Breadcrumbs
                function setBreadcrumbs() {
                    if (blade.breadcrumbs) {
                        //Clone array (angular.copy leaves the same reference)
                        var breadcrumbs = blade.breadcrumbs.slice(0);

                        //prevent duplicate items
                        var all = _.all(breadcrumbs, function (x) {
                            return x.id !== blade.currentEntity.id;
                        });
                        if (all) {
                            var breadCrumb = generateBreadcrumb(blade.currentEntity.id, blade.currentEntity.name);
                            breadcrumbs.push(breadCrumb);
                        }
                        blade.breadcrumbs = breadcrumbs;
                    } else {
                        blade.breadcrumbs = [generateBreadcrumb(null, 'customer.blades.member-list.breadcrumb-all')];
                    }
                }

                function generateBreadcrumb(id, name) {
                    return {
                        id: id,
                        name: name,
                        blade: blade,
                        navigate: function (breadcrumb) {
                            breadcrumb.blade.disableOpenAnimation = true;
                            bladeNavigationService.showBlade(breadcrumb.blade);
                            breadcrumb.blade.refresh();
                        }
                    };
                }

                blade.setSelectedNode = function (listItem) {
                    $scope.selectedNodeId = listItem.id;
                };

                $scope.selectNode = function (listItem) {
                    blade.setSelectedNode(listItem);

                    var foundTemplate = memberTypesResolverService.resolve(listItem.memberType);
                    if (foundTemplate && foundTemplate.knownChildrenTypes && foundTemplate.knownChildrenTypes.length) {
                        var newBlade = {
                            id: blade.id,
                            breadcrumbs: blade.breadcrumbs,
                            subtitle: 'customer.blades.member-list.subtitle',
                            subtitleValues: { name: listItem.name },
                            currentEntity: listItem,
                            disableOpenAnimation: true,
                            controller: blade.controller,
                            template: blade.template,
                            isClosingDisabled: true,
                            options: $scope.options
                        };
                        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
                    }
                };

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.refresh",
                        icon: 'fa fa-refresh',
                        executeMethod: blade.refresh,
                        canExecuteMethod: function () { return true; }
                    },
                    {
                        name: "platform.commands.confirm",
                        icon: 'fa fa-check',
                        executeMethod: function (selectBlade) {
                            if ($scope.options.pickExecutedCallback) {
                                $scope.options.pickExecutedCallback(selectBlade);
                            }
                        },
                        canExecuteMethod: function () {
                            return _.any($scope.options.selectedItemIds);
                        }
                    },
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
                    gridOptions.isRowSelectable = function (row) {
                        return !_.contains(row.entity.groups, blade.contract.code);
                    };

                    gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);

                    uiGridHelper.initialize($scope, gridOptions, externalRegisterApiCallback);
                    bladeUtils.initializePagination($scope);
                };

                function externalRegisterApiCallback(gridApi) {
                    $scope.gridApi = gridApi;

                    gridApi.grid.registerDataChangeCallback(function (grid) {
                        //check already selected rows
                        $timeout(function () {
                            _.each($scope.listEntries, function (x) {
                                var checked = _.some($scope.options.selectedItemIds, function (y) {
                                        return y === x.id;
                                    });
                                if (checked) {
                                    gridApi.selection.selectRow(x);
                                }
                            });
                        });
                    }, [uiGridConstants.dataChange.ROW]);

                    gridApi.selection.on.rowSelectionChanged($scope, function (row) {
                        if ($scope.options.checkItemCallback) {
                            $scope.options.checkItemCallback(row.entity, row.isSelected);
                        }

                        if (row.isSelected) {
                            if (!_.contains($scope.options.selectedItemIds, row.entity.id)) {
                                $scope.options.selectedItemIds.push(row.entity.id);
                            }
                        }
                        else {
                            $scope.options.selectedItemIds = _.without($scope.options.selectedItemIds, row.entity.id);
                        }
                    });

                    uiGridHelper.bindRefreshOnSortChanged($scope);
                }

                function getSearchCriteria() {
                    return {
                        memberType: blade.memberType,
                        memberId: blade.currentEntity.id,
                        keyword: filter.keyword ? filter.keyword : undefined,
                        deepSearch: filter.keyword ? true : false,
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount,
                        objectType: 'Member'
                    };
                }
            }]);
