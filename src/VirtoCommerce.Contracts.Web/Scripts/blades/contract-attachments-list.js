angular.module('Contracts')
    .controller('Contracts.contractAttachmentsListController',
        ['$scope', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper', '$translate',
            function ($scope, bladeUtils, dialogService, uiGridHelper, $translate) {
                var blade = $scope.blade;
                blade.headIcon = 'fas fa-file-contract';
                blade.title = 'Contract.blades.contract-attachments-list.title';
                blade.isLoading = false;
                $scope.isValid = true;

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var bladeNavigationService = bladeUtils.bladeNavigationService;

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.add",
                        icon: 'fas fa-plus',
                        executeMethod: function () {
                            var newBlade = {
                                title: "Contract.blades.contract-attachment-add.title",
                                contract: blade.contract,
                                onSelect: function (attachments) {
                                    _.each(attachments, function (attachment) {
                                        blade.currentEntities.push(angular.copy(attachment));
                                    });
                                },
                                controller: 'Contracts.contractAttachmentsAddController',
                                template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-attachment-add.html'
                            };
                            bladeNavigationService.showBlade(newBlade, blade);
                        },
                        canExecuteMethod: function () {
                            return true;
                        },
                        permission: 'Contract:create'
                    },
                    {
                        name: "platform.commands.delete",
                        icon: 'fas fa-trash-alt',
                        executeMethod: function () {
                            $scope.deleteAttachments($scope.gridApi.selection.getSelectedRows());
                        },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'Contract:delete'
                    }
                ];

                blade.refresh = function (item) {
                    initialize(item);
                }

                function initialize(contract) {
                    blade.subtitle = contract.name;

                    blade.currentEntities = contract.attachments ? angular.copy(contract.attachments) : [];
                }

                $scope.selectNode = function (contract) {
                    $scope.selectedNodeId = contract.id;
                    blade.selectedConctact = contract;
                    blade.editContract(contract);
                };

                $scope.saveChanges = function () {
                    blade.contract.attachments = blade.currentEntities;

                    if (blade.parentRefresh) {
                        blade.parentRefresh();
                    }

                    $scope.bladeClose();
                };

                $scope.deleteAttachments = function (selection) {
                    var dialog = {
                        id: "confirmDeleteContracts",
                        title: "Contract.dialogs.notification-contract-delete.title",
                        message: "Contract.dialogs.notification-contract-delete.message",
                        callback: function (remove) {
                            if (remove) {
                                angular.forEach(selection, function (attachment) {
                                    deleteAttachment(attachment);
                                });
                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                }

                $scope.deleteAttachment = function (attachment) {
                    var dialog = {
                        id: "confirmDeleteContracts",
                        title: "Contract.dialogs.notification-contract-delete.title",
                        message: "Contract.dialogs.notification-contract-delete.message",
                        callback: function (remove) {
                            if (remove) {
                                deleteAttachment(attachment);

                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                };

                function deleteAttachment(attachment) {
                    var index = blade.currentEntities.indexOf(attachment);
                    if (index >= 0) {
                        blade.currentEntities.splice(index, 1);
                    }
                }

                $scope.downloadUrl = function (attachment) {
                    window.open(attachment.url, '_blank');
                }

                $scope.copyUrl = function (attachment) {
                    $translate('Contract.blades.contract-attachments-list.labels.copy-url-prompt').then(function (promptMessage) {
                        window.prompt(promptMessage, attachment.url);
                    });
                }

                // ui-grid
                $scope.setGridOptions = function (gridOptions) {
                    uiGridHelper.initialize($scope, gridOptions);
                };

                initialize(blade.contract);
            }]);
