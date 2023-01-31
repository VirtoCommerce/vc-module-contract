angular.module('Contracts')
    .controller('Contracts.contractAttachmentsWidgetController',
        ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
            var blade = $scope.widget.blade;

            function refresh() {
                $scope.attachmentsCount = blade.currentEntity.attachments
                    ? blade.currentEntity.attachments.length
                    : 0;
            }

            $scope.show = function () {
                var newBlade = {
                    id: 'contractAttachmentsList',
                    contract: blade.currentEntity,
                    controller: 'Contracts.contractAttachmentsListController',
                    template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-attachments-list.html',
                    parentRefresh: function () {
                        refresh();
                    }
                };
                bladeNavigationService.showBlade(newBlade, blade);
            }

            refresh();
        }]);
