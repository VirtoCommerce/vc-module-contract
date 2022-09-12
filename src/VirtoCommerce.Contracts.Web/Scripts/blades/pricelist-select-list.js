angular.module('Contracts')
    .controller('Contracts.pricelistSelectController',
        ['$scope', '$timeout', 'virtoCommerce.pricingModule.pricelists', 'platformWebApp.bladeUtils', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper', 'uiGridConstants',
            function ($scope, $timeout, pricelists, bladeUtils, gridOptionExtension, uiGridHelper, uiGridConstants) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-list';
                blade.title = 'Contract.blades.contract-pricrlist-select.title';

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                $scope.options = angular.extend({
                    selectedItemIds: []
                }, blade.options);

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
                    var result = {
                        keyword: filter.keyword,
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount,
                        responseGroup: "NoDetails"
                    };

                    if (filter.current) {
                        result.currencies = filter.current.currencies;
                    }
                    return result;
                }

                blade.refresh = function () {
                    blade.isLoading = true;

                    var searchCriteria = getSearchCriteria();
                    pricelists.search(searchCriteria, function (data) {
                        blade.isLoading = false;

                        $scope.pageSettings.totalItems = data.totalCount;
                        $scope.listEntries = data.results ? data.results : [];
                    });
                };

                $scope.selectNode = function (node) {
                    $scope.selectedNodeId = node.id;
                };

                // ui-grid
                $scope.setGridOptions = function (gridId, gridOptions) {
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
            }]);
