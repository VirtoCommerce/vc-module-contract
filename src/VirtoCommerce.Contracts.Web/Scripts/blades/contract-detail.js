angular.module('Contracts')
    .controller('Contracts.contractController',
        ['$scope', 'Contracts.api', 'virtoCommerce.storeModule.stores', 'virtoCommerce.customerModule.members', 'platformWebApp.dialogService', 'platformWebApp.bladeUtils', 'platformWebApp.metaFormsService', 'platformWebApp.settings',
            function ($scope, contracts, stores, members, dialogService, bladeUtils, metaFormsService, settings) {
                var blade = $scope.blade;
                blade.headIcon = 'fas fa-file-contract';
                blade.updatePermission = 'Contract:update';

                blade.metaFields = metaFormsService.getMetaFields("contractDetail");

                var bladeNavigationService = bladeUtils.bladeNavigationService;

                if (blade.isNew) {
                    blade.title = 'Contract.blades.contract-details.title-new';
                    blade.currentEntity = {};
                } else {
                    blade.subtitle = 'Contract.blades.contract-details.subtitle';
                }

                blade.refresh = function (parentRefresh) {
                    if (blade.isNew) {
                        blade.currentEntity = {};
                        blade.isLoading = false;
                    } else {
                        blade.isLoading = true;

                        contracts.getContract({ id: blade.currentEntityId }, initializeBlade);

                        if (parentRefresh) {
                            blade.parentBlade.refresh(true);
                        }
                    }
                };

                settings.getValues({ id: 'Contract.Status' }, translateBladeStatuses);
                blade.openStatusSettingManagement = function () {
                    var newBlade = {
                        id: 'settingDetailChild',
                        isApiSave: true,
                        currentEntityId: 'Contract.Status',
                        parentRefresh: translateBladeStatuses,
                        controller: 'platformWebApp.settingDictionaryController',
                        template: '$(Platform)/Scripts/app/settings/blades/setting-dictionary.tpl.html'
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                };

                function translateBladeStatuses(data) {
                    blade.statuses = _.map(data, function (x)
                    {
                        return { value: x, key: x }
                    });
                }

                function initializeBlade(data) {
                    blade.currentEntity = angular.copy(data);
                    blade.originalEntity = data;

                    if (!blade.isNew) {
                        blade.title = blade.currentEntity.name;
                    }

                    blade.isLoading = false;
                }

                blade.searchStores = function (criteria) {
                    return stores.search(criteria);
                }

                $scope.setForm = function (form) {
                    $scope.formScope = blade.formScope = form;
                }

                $scope.saveChanges = function () {
                    blade.isLoading = true;

                    if (blade.isNew) {
                        contracts.createContract(blade.currentEntity,
                            function () {
                                blade.parentBlade.refresh(true);
                                $scope.bladeClose();
                            },
                            function (error) {
                                bladeNavigationService.setError(`${error.status}: ${error.statusText}`, blade);

                                var errorDialog = {
                                    id: "errorDetails",
                                    title: 'platform.dialogs.error-details.title',
                                    message: error.data.message
                                }
                                dialogService.showErrorDialog(errorDialog);
                            });
                    } else {
                        contracts.updateContract(blade.currentEntity,
                            function () {
                                blade.refresh(true);
                            });
                    }
                };

                // datepickers
                blade.datepickers = {
                    start: false,
                    end: false
                };

                blade.open = function ($event, which) {
                    $event.preventDefault();
                    $event.stopPropagation();

                    blade.datepickers[which] = true;
                };

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.save",
                        icon: 'fas fa-save',
                        executeMethod: $scope.saveChanges,
                        canExecuteMethod: canSave,
                        permission: blade.updatePermission
                    }];

                if (!blade.isNew) {
                    blade.toolbarCommands.push(
                        {
                            name: "platform.commands.reset",
                            icon: 'fa fa-undo',
                            executeMethod: function () {
                                angular.copy(blade.originalEntity, blade.currentEntity);
                            },
                            canExecuteMethod: isDirty,
                            permission: blade.updatePermission
                        }
                    );
                }

                function canSave() {
                    return isDirty() && $scope.formScope && $scope.formScope.$valid;
                }

                function isDirty() {
                    return !angular.equals(blade.currentEntity, blade.originalEntity) && blade.hasUpdatePermission();
                }

                // handle contract code modifications via debounce
                var createAndSetContractCode = function () {
                    if (!blade.currentEntity.name) {
                        return;
                    }

                    var contractCode = blade.currentEntity.name.trim();
                    if (contractCode.length === 0) {
                        return;
                    }

                    contractCode = contractCode.toLowerCase();
                    contractCode = contractCode.replace(/\W+/g, '-');
                    contractCode = "contract-" + contractCode;

                    blade.currentEntity.code = contractCode;
                }

                var debounceCreateAndSetContractCode = _.debounce(function () {
                    // need to $apply model modifications so that it would happen inside $digest cycle
                    $scope.$apply(function () {
                        createAndSetContractCode();
                    });
                }, 800);

                $scope.$watch('blade.currentEntity.name', function () {
                    // disable automatic code handling if Code field was modified by user
                    if (isCodeModifiedByUser() || !blade.isNew) {
                        return;
                    }

                    debounceCreateAndSetContractCode($scope);
                });

                function isCodeModifiedByUser() {
                    return $scope.formScope &&
                        $scope.formScope.code &&
                        $scope.formScope.code.$dirty;
                }

                blade.codeValidator = function (value) {
                    var pattern = /[^\w_-]/;
                    return !pattern.test(value);
                };

                blade.fetchVendors = function (criteria) {
                    return members.search(criteria);
                }

                blade.openVendorsManagement = function () {
                    var newBlade = {
                        id: 'vendorList',
                        currentEntity: { id: null },
                        controller: 'virtoCommerce.customerModule.memberListController',
                        template: 'Modules/$(VirtoCommerce.Customer)/Scripts/blades/member-list.tpl.html'
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                };

                blade.refresh(false);
            }]);
