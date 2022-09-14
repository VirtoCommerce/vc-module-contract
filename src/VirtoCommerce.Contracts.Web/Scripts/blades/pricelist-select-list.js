angular.module('Contracts')
    .controller('Contracts.pricelistSelectController',
        ['$scope', '$timeout', 'virtoCommerce.pricingModule.pricelists', 'platformWebApp.bladeUtils', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper', 'uiGridConstants', '$localStorage', 'platformWebApp.bladeNavigationService',
            function ($scope, $timeout, pricelists, bladeUtils, gridOptionExtension, uiGridHelper, uiGridConstants, $localStorage, bladeNavigationService) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-list';
                blade.title = 'Contract.blades.contract-pricrlist-select.title';

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                $scope.options = angular.extend({
                    selectedItemId: null
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
                            return $scope.options.selectedItemId;
                        }
                    }
                ];

                // filtering
                var filter = $scope.filter = blade.filter = {};
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

                // predefined filters for pricelists
                var defaultDataRequest = {
                    exportTypeName: 'VirtoCommerce.PricingModule.Data.ExportImport.ExportablePricelist',
                    dataQuery: {
                        exportTypeName: 'PricelistExportDataQuery'
                    }
                };
                var dataRequest = angular.copy(defaultDataRequest);

                if (!$localStorage.exportSearchFilters) {
                    $localStorage.exportSearchFilters = {};
                }

                if (!$localStorage.exportSearchFilters[dataRequest.exportTypeName]) {
                    $localStorage.exportSearchFilters[dataRequest.exportTypeName] = [{ name: 'export.blades.export-generic-viewer.labels.new-filter' }];
                }

                $scope.searchFilters = $localStorage.exportSearchFilters[dataRequest.exportTypeName];

                if (!$localStorage.exportSearchFilterIds) {
                    $localStorage.exportSearchFilterIds = {};
                }

                $scope.exportSearchFilterId = $localStorage.exportSearchFilterIds[dataRequest.exportTypeName];

                if ($scope.exportSearchFilterId) {
                    filter.current = _.findWhere($scope.searchFilters, { id: $scope.exportSearchFilterId });
                }

                filter.change = function () {
                    $localStorage.exportSearchFilterIds[dataRequest.exportTypeName] = filter.current ? filter.current.id : null;

                    var metafieldsId = dataRequest.exportTypeName + 'ExportFilter';
                    if (filter.current && !filter.current.id) {
                        filter.current = null;
                        showFilterDetailBlade({ isNew: true, metafieldsId: metafieldsId, exportTypeName: dataRequest.exportTypeName });
                    } else {
                        bladeNavigationService.closeBlade({ id: 'exportGenericViewerFilter' });

                        if (!filter.current) {
                            blade.resetRequestCustomFilter();
                        }

                        filter.criteriaChanged();
                    }
                };

                filter.edit = function () {
                    var metafieldsId = dataRequest.exportTypeName + 'ExportFilter';
                    var filterDetailsParams = {
                        data: filter.current,
                        metafieldsId: metafieldsId,
                        exportTypeName: dataRequest.exportTypeName
                    };

                    if (filter.current) {
                        angular.extend(filterDetailsParams, { data: filter.current });
                    }
                    else {
                        angular.extend(filterDetailsParams, { isNew: true });
                    }

                    showFilterDetailBlade(filterDetailsParams);
                };

                function showFilterDetailBlade(bladeData) {
                    var newBlade = {
                        id: 'exportGenericViewerFilter',
                        controller: 'virtoCommerce.exportModule.exportGenericViewerFilterController',
                        template: 'Modules/$(VirtoCommerce.Export)/Scripts/blades/export-generic-viewer-filter.tpl.html',
                        onBeforeApply: blade.resetRequestCustomFilter
                    };
                    angular.extend(newBlade, bladeData);
                    bladeNavigationService.showBlade(newBlade, blade);
                }

                blade.resetRequestCustomFilter = function () {
                    angular.copy(dataRequest, defaultDataRequest);
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
                        //check already selected row
                        $timeout(function () {
                            if ($scope.options.selectedItemId) {
                                var checkedRow = _.find($scope.listEntries, function (x) {
                                    return $scope.options.selectedItemId === x.id;
                                });
                                if (checkedRow) {
                                    gridApi.selection.selectRow(checkedRow);
                                }
                            }
                        });
                    }, [uiGridConstants.dataChange.ROW]);

                    gridApi.selection.on.rowSelectionChanged($scope, function (row) {
                        if (row.isSelected) {
                            if ($scope.options.selectedItemId && row.entity.id !== $scope.options.selectedItemId) {
                                var checkedRow = _.find($scope.listEntries, function (x) {
                                    return $scope.options.selectedItemId === x.id;
                                });
                                if (checkedRow) {
                                    gridApi.selection.unSelectRow(checkedRow);
                                }
                            }
                            $scope.options.selectedItemId = row.entity.id;
                        }
                        else {
                            $scope.options.selectedItemId = null;
                        }

                        if ($scope.options.checkItemCallback) {
                            $scope.options.checkItemCallback(row.entity, row.isSelected);
                        }
                    });

                    uiGridHelper.bindRefreshOnSortChanged($scope);
                }
            }]);
