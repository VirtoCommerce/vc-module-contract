angular.module('Contract')
    .controller('Contract.contractAttachmentsWidgetController',
        ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
            var blade = $scope.widget.blade;

            blade.show = function () {
            }
        }]);
