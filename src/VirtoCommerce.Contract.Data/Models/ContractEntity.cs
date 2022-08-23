using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.Contract.Data.Models
{
    public class ContractEntity : AuditableEntity, IDataEntity<ContractEntity, Core.Models.Contract>
    {
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        [StringLength(128)]
        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ContractEntity FromModel(Core.Models.Contract model, PrimaryKeyResolvingMap pkMap)
        {
            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Name = model.Name;
            Description = model.Description;
            StoreId = model.StoreId;
            StartDate = model.StartDate;
            EndDate = model.EndDate;

            return this;
        }

        public Core.Models.Contract ToModel(Core.Models.Contract model)
        {
            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Name = Name;
            model.Description = Description;
            model.StoreId = StoreId;
            model.StartDate = StartDate;
            model.EndDate = EndDate;

            return model;
        }

        public void Patch(ContractEntity target)
        {
            target.Name = Name;
            target.Description = Description;
            target.StoreId = StoreId;
            target.StartDate = StartDate;
            target.EndDate = EndDate;
        }
    }
}
