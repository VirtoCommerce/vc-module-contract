angular.module('Contracts')
    .controller('Contracts.contractAttachmentsAddController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'FileUploader', function ($scope, $translate, bladeNavigationService, FileUploader) {
        var blade = $scope.blade;
        blade.headIcon = 'fa fa-plus';
        blade.toolbarCommands = [];
        blade.isLoading = false;
        $scope.isValid = true;

        blade.refresh = function (contract) {
            initialize(contract);
        }

        $scope.saveChanges = function () {
            if (blade.onSelect) {
                blade.onSelect(blade.currentEntities);
            }

            $scope.bladeClose();
        };

        function initialize(contract) {
            blade.item = contract;
            if (!$scope.uploader && blade.hasUpdatePermission()) {
                var uploader = $scope.uploader = new FileUploader({
                    scope: $scope,
                    headers: { Accept: 'application/json' },
                    method: 'POST',
                    autoUpload: true,
                    removeAfterUpload: true
                });

                uploader.url = 'api/assets?folderUrl=contracts/' + contract.code;

                uploader.onSuccessItem = function (fileItem, attachments) {
                    angular.forEach(attachments, function (attachment) {
                        attachment.itemId = blade.item.id;
                        if (fileItem.file) {
                            attachment.size = fileItem.file.size;
                        }

                        blade.currentEntities.push(attachment);
                    });
                };

                uploader.onAfterAddingAll = function (addedItems) {
                    bladeNavigationService.setError(null, blade);
                };

                uploader.onErrorItem = function (item, response, status, headers) {
                    var message = response.message ? response.message : status;
                    bladeNavigationService.setError(`${item._file.name} failed: ${message}`, blade);
                };
            }
            blade.currentEntities = [];
        }

        $scope.removeAction = function (attachment) {
            var index = blade.currentEntities.indexOf(attachment);
            if (index >= 0) {
                blade.currentEntities.splice(index, 1);
            }
        };

        $scope.copyUrl = function (data) {
            $translate('Contract.blades.contract-attachments-list.labels.copy-url-prompt').then(function (promptMessage) {
                window.prompt(promptMessage, data.url);
            });
        }

        initialize(blade.contract);
    }]);
