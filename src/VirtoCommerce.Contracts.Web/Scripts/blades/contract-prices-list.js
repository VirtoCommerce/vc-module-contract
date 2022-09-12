angular.module('Contracts')
    .controller('Contracts.contractPricesListController',
        ['$scope', 'Contracts.pricesApi', 'platformWebApp.bladeUtils', 'platformWebApp.ui-grid.extension', 'platformWebApp.uiGridHelper',
            function ($scope, contractPrices, bladeUtils, gridOptionExtension, uiGridHelper) {
                var blade = $scope.blade;
                blade.headIcon = 'fa fa-list';
                blade.title = 'Contract.blades.contract-prices.title';

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
                    icon: 'fa fa-check',
                    executeMethod: function () {
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

                                // create request
                                var addRequest = {
                                    contractId: blade.contract.id,
                                    pricelistId: _.first(selection)
                                };

                                contractPrices.linkPricelistMembers(addRequest, function () {
                                    bladeNavigationService.closeBlade(selectBlade);
                                    blade.pricelistLinked = true;
                                    blade.refresh(true);
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
                    name: "platform.commands.add",
                    icon: 'fas fa-plus',
                    executeMethod: function () {
                        $scope.selectedNodeId = null;
                        var selectedProducts = [];
                        var newBlade = {
                            id: "CatalogItemsSelect",
                            title: "'Contract.blades.contract-prices.select-products-title",
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

                blade.refresh = function (parentRefresh) {
                    if (blade.pricelistLinked) {
                        blade.toolbarCommands = [refreshCommand, addProductCommand];
                    }
                    else {
                        blade.toolbarCommands = [linkPricelistCommand];
                    }

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

                    if (parentRefresh && blade.parentRefresh) {
                        blade.parentRefresh();
                    }
                };

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
