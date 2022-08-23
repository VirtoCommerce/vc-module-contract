angular.module('Contract')
    .factory('Contract.webApi', ['$resource', function ($resource) {
        return $resource('api/contracts/', {}, {
            getContract: { method: 'GET', url: 'api/contracts/:id' },
            searchContracts: { method: 'POST', url: 'api/contracts/search' },
            createContract: { method: 'POST' },
            updateContract: { method: 'PUT' },
            deleteContract: { method: 'DELETE' }
        })
    }]);
