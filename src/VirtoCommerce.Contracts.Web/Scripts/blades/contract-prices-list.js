angular.module('Contracts')
    .controller('Contracts.contractPricesListController',
        ['$scope', 'Contracts.pricesApi', 'platformWebApp.bladeUtils', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper', 'platformWebApp.dialogService',
            function ($scope, contractPrices, bladeUtils, gridOptionExtension, uiGridHelper, dialogService) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-list';
                blade.title = 'Contract.blades.contract-prices.title';
                blade.noDataImage = 'Modules/$(VirtoCommerce.Contracts)/Content/images/pricelist.svg';
                blade.isLoading = false;

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var bladeNavigationService = bladeUtils.bladeNavigationService;

                var refreshCommand = {
                    name: "platform.commands.refresh",
                    icon: 'fa fa-refresh',
                    executeMethod: function () {
                        blade.refresh();
                    },
                    canExecuteMethod: function () { return true; }
                };

                var linkPricelistCommand = {
                    name: 'Contract.blades.contract-prices.commands.link-pricelist',
                    icon: 'fa fa-file-text',
                    executeMethod: function () {
                        var selection = null;

                        var options = {
                            selectedItemId: selection,
                            checkItemCallback: function (listItem, isSelected) {
                                if (isSelected) {
                                    selection = listItem.id;
                                }
                                else {
                                    selection = null;
                                }
                            },
                            pickExecutedCallback: function (selectBlade) {
                                blade.isLoading = true;

                                // create request
                                var addRequest = {
                                    contractId: blade.contract.id,
                                    pricelistId: selection
                                };

                                contractPrices.linkPricelistMembers(addRequest, function () {
                                    bladeNavigationService.closeBlade(selectBlade);
                                    blade.pricelistLinked = true;

                                    blade.refresh();

                                    // call parent refresh separately because paging directive always passes calls "blade.refresh" with parentRefresh = 1
                                    if (blade.parentRefresh) {
                                        blade.parentBlade.pricelistLinked = true;
                                        blade.parentRefresh();
                                    }
                                }, function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
                            }
                        };

                        var newBlade = {
                            id: 'selectContractPricelist',
                            contract: blade.currentEntity,
                            controller: 'Contracts.pricelistSelectController',
                            template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/pricelist-select-list.html',
                            options: options
                        };
                        bladeNavigationService.showBlade(newBlade, blade);
                    },
                    canExecuteMethod: function () { return true; },
                    permission: 'Contract:update'
                };

                var addProductCommand = {
                    name: 'platform.commands.add',
                    icon: 'fas fa-plus',
                    executeMethod: function () {
                        $scope.selectedNodeId = null;
                        var selectedProducts = [];
                        var newBlade = {
                            id: "CatalogItemsSelect",
                            title: "Contract.blades.contract-prices.select-products-title",
                            controller: 'virtoCommerce.catalogModule.catalogItemSelectController',
                            template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/common/catalog-items-select.tpl.html',
                            breadcrumbs: [],
                            toolbarCommands: [
                                {
                                    name: "pricing.commands.add-selected",
                                    icon: 'fas fa-plus',
                                    executeMethod: function (contractBlade) {
                                        contractBlade.isLoading = true;

                                        var payload = {
                                            contractId: blade.contract.id,
                                            prices: _.map(selectedProducts, function (product) {
                                                return {
                                                    productId: product.id,
                                                    minQuantity: 1
                                                }
                                            })
                                        };

                                        contractPrices.saveContractPrices(payload, function () {
                                            bladeNavigationService.closeBlade(contractBlade);
                                            blade.refresh();
                                            // call parent refresh separately because paging directive always passes calls "blade.refresh" with parentRefresh = 1
                                            if (blade.parentRefresh) {
                                                blade.parentRefresh();
                                            }
                                        }, function (error) {
                                            bladeNavigationService.setError('Error ' + error.status, blade);
                                        });
                                    },
                                    canExecuteMethod: function () {
                                        return selectedProducts.length > 0;
                                    }
                                }]
                        };

                        newBlade.options = {
                            allowCheckingCategory: false,
                            checkItemFn: function (listItem, isSelected) {
                                if (isSelected) {
                                    if (_.all(selectedProducts, function (x) {
                                        return x.id !== listItem.id;
                                    })) {
                                        selectedProducts.push(listItem);
                                    }
                                }
                                else {
                                    selectedProducts = _.reject(selectedProducts,
                                        function (x) { return x.id === listItem.id; });
                                }
                            }
                        };

                        bladeNavigationService.showBlade(newBlade, blade);
                    },
                    canExecuteMethod: function () { return true; },
                    permission: blade.updatePermission
                };

                var resetProductCommand = {
                    name: 'Contract.blades.contract-prices.commands.restore',
                    icon: 'fa fa-undo',
                    executeMethod: function () {
                        var selection = $scope.gridApi.selection.getSelectedRows();

                        var dialog = {
                            id: "confirmRestoreContractPrices",
                            title: "Contract.dialogs.notification-contract-prices-restore.title",
                            message: "Contract.dialogs.notification-contract-prices-restore.message",
                            callback: function (remove) {
                                if (remove) {
                                    bladeNavigationService.closeChildrenBlades(blade, function () {
                                        var payload = {
                                            contractId: blade.contract.id,
                                            productIds: _.pluck(selection, 'productId')
                                        };

                                        contractPrices.restoreContractPrices(payload,
                                            function () {
                                                blade.refresh();
                                                // call parent refresh separately because paging directive always passes calls "blade.refresh" with parentRefresh = 1
                                                if (blade.parentRefresh) {
                                                    blade.parentRefresh();
                                                }
                                            },
                                            function (error) {
                                                bladeNavigationService.setError('Error ' + error.status, blade);
                                            });
                                    });
                                }
                            }
                        };

                        dialogService.showWarningDialog(dialog);
                    },
                    canExecuteMethod: function () {
                        return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                    },
                    permission: blade.updatePermission
                }

                blade.toolbarCommands = [];

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
                        contractId: blade.contract.id,
                        keyword: filter.keyword,
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };
                }

                blade.updateToolbarCommadns = function() {
                    if (blade.pricelistLinked) {
                        blade.toolbarCommands = [refreshCommand, addProductCommand, resetProductCommand];
                    }
                    else {
                        blade.toolbarCommands = [linkPricelistCommand];
                    }
                }

                blade.refresh = function () {
                    blade.updateToolbarCommadns();

                    blade.isLoading = true;
                    var searchCriteria = getSearchCriteria();

                    if (blade.searchCriteria) {
                        angular.extend(searchCriteria, blade.searchCriteria);
                    }

                    contractPrices.searchContractPrices(searchCriteria,
                        function (data) {
                            blade.isLoading = false;
                            $scope.pageSettings.totalItems = data.totalCount;

                            $scope.listEntries = data.results ? data.results : [];
                        });
                };

                $scope.linkPricelist = function () {
                    linkPricelistCommand.executeMethod();
                }

                $scope.selectNode = function (price) {
                    $scope.selectedNodeId = price.id;
                    blade.selectedConctact = price;
                    blade.openPrice(price);
                };

                blade.openPrice = function (price) {
                    var newBlade = {
                        id: 'contractProductPrices',
                        productId: price.productId,
                        contract: blade.contract,
                        title: 'pricing.blades.prices-list.title',
                        titleValues: { name: price.productName },
                        subtitle: 'pricing.blades.prices-list.subtitle',
                        controller: 'Contracts.pricesListController',
                        template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/product-prices-list.html',
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                }

                $scope.getPriceRange = function (priceGroup) {
                    var min = priceGroup.minListPrice;
                    if (priceGroup.minSalePrice) {
                        min = Math.min(min, priceGroup.minSalePrice);
                    }

                    var max = Math.max(priceGroup.maxListPrice, priceGroup.maxSalePrice);

                    if (max === min) {
                        return `${max}`;
                    }

                    return `${min}-${max}`;
                }

                // ui-grid
                $scope.setGridOptions = function (gridId, gridOptions) {
                    $scope.gridOptions = gridOptions;

                    gridOptions.isRowSelectable = function (row) {
                        return row.entity.groupState !== 'Base';
                    };

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

                blade.updateToolbarCommadns();
            }]);
