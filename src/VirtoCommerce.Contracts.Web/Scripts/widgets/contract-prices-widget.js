angular.module('Contracts')
    .controller('Contracts.contractPricesWidgetController',
        ['$scope', 'platformWebApp.bladeNavigationService', 'Contracts.pricesApi', function ($scope, bladeNavigationService, contractPrices) {
            var blade = $scope.widget.blade;

            function refresh() {
                $scope.pricesCount = '...';

                var searchCriteria = {
                    contractId: blade.currentEntity.id,
                    take: 0
                };

                contractPrices.searchContractPrices(searchCriteria, function (data) {
                    $scope.pricesCount = data.totalCount;
                });
            }

            $scope.show = function () {
                var newBlade = {
                    id: 'contractPricesList',
                    contract: blade.currentEntity,
                    pricelistLinked: !!blade.currentEntity.basePricelistAssignmentId || blade.pricelistLinked, //show "link pricelist" button only if no assigment was created
                    controller: 'Contracts.contractPricesListController',
                    template: 'Modules/$(VirtoCommerce.Contracts)/Scripts/blades/contract-prices-list.html',
                    parentRefresh: function (parentRefresh) {
                        refresh();

                        if (parentRefresh) {
                            blade.parentBlade.refresh();
                        }
                    }
                };
                bladeNavigationService.showBlade(newBlade, blade);
            }

            refresh();
        }]);
