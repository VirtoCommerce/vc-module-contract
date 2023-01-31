using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.Contracts.Data.Models
{
    public class ContractAttachmentEntity : AuditableEntity, IDataEntity<ContractAttachmentEntity, ContractAttachment>
    {
        [StringLength(1024)]
        public string Url { get; set; }

        [StringLength(1024)]
        public string Name { get; set; }

        #region Navigation Properties

        public virtual ContractEntity Contract { get; set; }
        public string ContractId { get; set; }

        #endregion

        public ContractAttachmentEntity FromModel(ContractAttachment model, PrimaryKeyResolvingMap pkMap)
        {
            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Name = model.Name;
            Url = model.Url;

            return this;
        }

        public ContractAttachment ToModel(ContractAttachment model)
        {
            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Url = Url;
            model.Name = Name;

            return model;
        }

        public void Patch(ContractAttachmentEntity target)
        {
            target.Url = Url;
            target.Name = Name;
        }
    }
}
