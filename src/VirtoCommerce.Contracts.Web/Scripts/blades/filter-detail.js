angular.module('Contracts')
    .controller('Contracts.filterDetailController', ['$scope', '$localStorage', 'platformWebApp.settings', 'virtoCommerce.customerModule.members', '$translate', 'platformWebApp.metaFormsService',
        function ($scope, $localStorage, settings, members, $translate, metaFormsService) {
        var blade = $scope.blade;

        blade.metaFields = blade.metaFields ? blade.metaFields : metaFormsService.getMetaFields('contractFilterDetail');
        
        function translateBladeStatuses(data) {
            blade.statuses = _.map(data, function (x) {
                return { value: x, key: x }
            });
        }
        settings.getValues({ id: 'Contract.Status' }, translateBladeStatuses);

        $scope.applyCriteria = function () {
            angular.copy(blade.currentEntity, blade.origEntity);
            if (blade.isNew) {
                $localStorage.contractSearchFilters.push(blade.origEntity);
                $localStorage.contractSearchFilterId = blade.origEntity.id;
                blade.parentBlade.filter.current = blade.origEntity;
                blade.isNew = false;
            }

            initializeBlade(blade.origEntity);
            blade.parentBlade.filter.criteriaChanged();
        };

        $scope.saveChanges = function () {
            $scope.applyCriteria();
        };

        function initializeBlade(data) {
            blade.currentEntity = angular.copy(data);
            blade.origEntity = data;
            blade.isLoading = false;

            blade.title = blade.isNew ? 'Contract.blades.filter-detail.new-title' : data.name;
            blade.subtitle = blade.isNew ? 'Contract.blades.filter-detail.new-subtitle' : 'Contract.blades.filter-detail.subtitle';
        }

        var formScope;
        $scope.setForm = function (form) {
            formScope = form;
        };

        function isDirty() {
            return !angular.equals(blade.currentEntity, blade.origEntity);
        }

        blade.headIcon = 'fa fa-filter';

        blade.toolbarCommands = [
            {
                name: "core.commands.apply-filter", icon: 'fa fa-filter',
                executeMethod: function () {
                    $scope.saveChanges();
                },
                canExecuteMethod: function () {
                    return formScope && formScope.$valid;
                }
            },
            {
                name: "platform.commands.reset", icon: 'fa fa-undo',
                executeMethod: function () {
                    angular.copy(blade.origEntity, blade.currentEntity);
                },
                canExecuteMethod: isDirty
            },
            {
                name: "platform.commands.delete", icon: 'fas fa-trash-alt',
                executeMethod: deleteEntry,
                canExecuteMethod: function () {
                    return !blade.isNew;
                }
        }];

        blade.fetchVendors = function (criteria) {
            return members.search(criteria);
        }

        function deleteEntry() {
            blade.parentBlade.filter.current = null;
            $localStorage.contractSearchFilters.splice($localStorage.contractSearchFilters.indexOf(blade.origEntity), 1);
            delete $localStorage.contractSearchFilterId;
            blade.parentBlade.refresh();
            $scope.bladeClose();
        }

        // actions on load
        if (blade.isNew) {
            $translate('Contract.blades.contracts-list.labels.unnamed-filter').then(function (result) {
                initializeBlade({ id: new Date().getTime(), name: result });
            });
        } else {
            initializeBlade(blade.data);
        }
    }]);
