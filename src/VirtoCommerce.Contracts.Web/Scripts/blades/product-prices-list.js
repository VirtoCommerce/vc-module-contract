angular.module('Contracts')
    .controller('Contracts.pricesListController',
        ['$scope', 'Contracts.pricesApi', 'platformWebApp.objCompareService', 'platformWebApp.bladeNavigationService', 'platformWebApp.uiGridHelper', 'virtoCommerce.pricingModule.priceValidatorsService', 'platformWebApp.ui-grid.extension',
            function ($scope, contractPrices, objCompareService, bladeNavigationService, uiGridHelper, priceValidatorsService, gridOptionExtension) {
                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var blade = $scope.blade;
                blade.updatePermission = 'Contracts:update';

                blade.refresh = function () {
                    blade.isLoading = true;

                    var payload = {
                        contractId: blade.contract.id,
                        productId: blade.productId
                    }

                    contractPrices.getContractProductPrices(payload, function (data) {
                        blade.currentEntities = angular.copy(data);
                        blade.originalEntities = data;
                        priceValidatorsService.setAllPrices(blade.currentEntities);

                        // set currency by first element
                        var first = _.first(blade.currentEntities);
                        if (first) {
                            blade.currency = first.currency;
                        }

                        blade.isLoading = false;
                    });
                };

                blade.onClose = function (closeCallback) {
                    bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, $scope.saveChanges, closeCallback,
                        "Contract.dialogs.notification-prices-save.title", "Contract.dialogs.notification-prices-save.message");
                };

                function isDirty() {
                    return blade.currentEntities && !objCompareService.equal(blade.originalEntities, blade.currentEntities) && blade.hasUpdatePermission()
                }

                function canSave() {
                    return isDirty() && $scope.isValid();
                }

                $scope.cancelChanges = function () {
                    $scope.bladeClose();
                };

                $scope.isValid = function () {
                    return $scope.formScope && $scope.formScope.$valid &&
                        _.all(blade.currentEntities, $scope.isListPriceValid) &&
                        _.all(blade.currentEntities, $scope.isSalePriceValid) &&
                        _.all(blade.currentEntities, $scope.isUniqueQty) &&
                        (blade.currentEntities.length === 0 || _.some(blade.currentEntities, function (x) { return x.minQuantity === 1; }));
                }

                $scope.saveChanges = function () {
                    blade.isLoading = true;

                    var changedPrices = [];

                    // get changed
                    _.each(blade.currentEntities, function (price) {
                        price = angular.copy(price);

                        var found = _.find(blade.originalEntities, function (originalPrice) {
                            return originalPrice.id === price.id;
                        });

                        if (found) {
                            if (!objCompareService.equal(price, found)) {
                                changedPrices.push(price)

                                if (price.state === 'Base' && price.id) {
                                    price.id = null;
                                }
                            }
                        }
                        else {
                            changedPrices.push(price)
                        }
                    });

                    if (_.any(changedPrices)) {
                        var payload = {
                            contractId: blade.contract.id,
                            prices: changedPrices
                        };

                        contractPrices.saveContractPrices(payload, function (data) {
                            angular.copy(blade.currentEntities, blade.originalEntities);

                            blade.parentBlade.refresh();
                            $scope.bladeClose();
                        },
                            function (error) { bladeNavigationService.setError('Error ' + error.status, $scope.blade); });
                    }
                };

                $scope.setForm = function (form) {
                    $scope.formScope = form;
                };

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.save",
                        icon: 'fas fa-save',
                        executeMethod: $scope.saveChanges,
                        canExecuteMethod: canSave,
                        permission: blade.updatePermission
                    },
                    {
                        name: "platform.commands.reset",
                        icon: 'fa fa-undo',
                        executeMethod: function () {
                            angular.copy(blade.originalEntities, blade.currentEntities);
                        },
                        canExecuteMethod: isDirty,
                        permission: blade.updatePermission
                    },
                    {
                        name: "platform.commands.add",
                        icon: 'fas fa-plus',
                        executeMethod: function () { addNewPrice(blade.currentEntities); },
                        canExecuteMethod: function () { return true; },
                        permission: blade.updatePermission
                    }
                ];

                function addNewPrice(targetList) {
                    var newEntity = { productId: blade.productId, list: '', minQuantity: 1, currency: blade.currency };
                    targetList.push(newEntity);
                    $scope.validateGridData();
                }

                $scope.isListPriceValid = priceValidatorsService.isListPriceValid;
                $scope.isSalePriceValid = priceValidatorsService.isSalePriceValid;
                $scope.isUniqueQty = priceValidatorsService.isUniqueQty;

                // ui-grid
                $scope.setGridOptions = function (gridId, gridOptions) {
                    gridOptions.onRegisterApi = function (gridApi) {
                        $scope.gridApi = gridApi;

                        gridApi.edit.on.afterCellEdit($scope, function () {
                            //to process validation for all rows in grid.
                            //e.g. if we have two rows with the same count of min qty, both of this rows will be marked as error.
                            //when we change data to valid in one row, another one should became valid too.
                            //more info about ui-grid validation: https://github.com/angular-ui/ui-grid/issues/4152
                            $scope.validateGridData();
                        });

                        $scope.validateGridData();
                    };

                    $scope.gridOptions = gridOptions;
                    gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);
                    return gridOptions;
                };

                $scope.validateGridData = function () {
                    if ($scope.gridApi) {
                        angular.forEach(blade.currentEntities, function (rowEntity) {
                            angular.forEach($scope.gridOptions.columnDefs, function (colDef) {
                                $scope.gridApi.grid.validate.runValidators(rowEntity, colDef, rowEntity[colDef.name], undefined, $scope.gridApi.grid)
                            });
                        });
                    }
                };

                // actions on load
                blade.refresh();
            }])
