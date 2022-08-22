angular.module('Contract')
    .factory('Contract.webApi', ['$resource', function ($resource) {
        return $resource('api/contract');
    }]);
