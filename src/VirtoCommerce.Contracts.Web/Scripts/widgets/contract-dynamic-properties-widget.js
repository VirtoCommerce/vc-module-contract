angular.module('Contracts')
    .controller('Contracts.contractDynamicPropertiesWidgetController',
        ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
            var blade = $scope.widget.blade;

            blade.show = function () {
            }
        }]);
