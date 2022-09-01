angular.module('Contracts')
    .controller('Contracts.contractCustomersWidgetController',
        ['$scope', 'platformWebApp.bladeNavigationService', 'Contracts.membersApi', function ($scope, bladeNavigationService, contractMembers) {
            var blade = $scope.widget.blade;

            function refresh() {
                $scope.membersCount = '...';

                var searchCriteria = {
                    contractId: blade.currentEntity.id,
                    deepSearch: true,
                    take: 0
                };

                contractMembers.searchContractMembers(searchCriteria, function (data) {
                    $scope.membersCount = data.totalCount;
                });
            }

            $scope.show = function () {
                var newBlade = {
                    id: 'contractMembersList',
                    contract: blade.currentEntity,
                    controller: 'Contracts.contractMembersListController',
                    template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-members-list.html',
                    parentRefresh: function () {
                        refresh();
                    }
                };
                bladeNavigationService.showBlade(newBlade, blade);
            }

            refresh();
        }]);
