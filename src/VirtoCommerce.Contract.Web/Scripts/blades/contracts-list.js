angular.module('Contract')
    .controller('Contract.contractsListController',
        ['$scope', 'Contract.webApi', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper',
            function ($scope, contracts, bladeUtils, dialogService, uiGridConstants, uiGridHelper) {
                var blade = $scope.blade;
            }]);
