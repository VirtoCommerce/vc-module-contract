using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace VirtoCommerce.Contracts.Data.Models
{
    public class ContractEntity : AuditableEntity, IDataEntity<ContractEntity, Contract>
    {
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(256)]
        public string Code { get; set; }

        [StringLength(128)]
        public string Status { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        [StringLength(128)]
        public string VendorId { get; set; }

        [StringLength(128)]
        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(128)]
        public string BasePricelistAssignmentId { get; set; }

        [StringLength(128)]
        public string PriorityPricelistAssignmentId { get; set; }

        public virtual ObservableCollection<ContractDynamicPropertyObjectValueEntity> DynamicPropertyObjectValues { get; set; }
            = new NullCollection<ContractDynamicPropertyObjectValueEntity>();

        public ObservableCollection<ContractAttachmentEntity> Attachments { get; set; } =
            new NullCollection<ContractAttachmentEntity>();

        public ContractEntity FromModel(Contract model, PrimaryKeyResolvingMap pkMap)
        {
            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            Name = model.Name;
            Code = model.Code;
            Status = model.Status;
            Description = model.Description;
            VendorId = model.VendorId;
            StoreId = model.StoreId;
            StartDate = model.StartDate;
            EndDate = model.EndDate;
            BasePricelistAssignmentId = model.BasePricelistAssignmentId;
            PriorityPricelistAssignmentId = model.PriorityPricelistAssignmentId;

            if (model.DynamicProperties != null)
            {
                DynamicPropertyObjectValues = new ObservableCollection<ContractDynamicPropertyObjectValueEntity>(model
                    .DynamicProperties
                    .SelectMany(x => x.Values.Select(y => AbstractTypeFactory<ContractDynamicPropertyObjectValueEntity>.TryCreateInstance().FromModel(y, model, x)))
                    .OfType<ContractDynamicPropertyObjectValueEntity>());
            }

            if (model.Attachments != null)
            {
                Attachments = new ObservableCollection<ContractAttachmentEntity>(model.Attachments.Select(x => AbstractTypeFactory<ContractAttachmentEntity>.TryCreateInstance().FromModel(x, pkMap)));
            }

            return this;
        }

        public Contract ToModel(Contract model)
        {
            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.Name = Name;
            model.Code = Code;
            model.Status = Status;
            model.Description = Description;
            model.VendorId = VendorId;
            model.StoreId = StoreId;
            model.StartDate = StartDate;
            model.EndDate = EndDate;
            model.BasePricelistAssignmentId = BasePricelistAssignmentId;
            model.PriorityPricelistAssignmentId = PriorityPricelistAssignmentId;

            model.DynamicProperties = DynamicPropertyObjectValues.GroupBy(x => x.PropertyId ?? x.PropertyName)
                .Select(x =>
                {
                    var property = AbstractTypeFactory<DynamicObjectProperty>.TryCreateInstance();
                    property.Id = x.First()?.PropertyId;
                    property.Name = x.FirstOrDefault()?.PropertyName;
                    property.Values = x.Select(y => y.ToModel(AbstractTypeFactory<DynamicPropertyObjectValue>.TryCreateInstance())).ToArray();
                    return property;
                }).ToArray();

            model.Attachments = Attachments.Select(x => x.ToModel(AbstractTypeFactory<ContractAttachment>.TryCreateInstance())).ToList();

            return model;
        }

        public void Patch(ContractEntity target)
        {
            target.Name = Name;
            target.Code = Code;
            target.Status = Status;
            target.Description = Description;
            target.StoreId = StoreId;
            target.StartDate = StartDate;
            target.EndDate = EndDate;
            target.BasePricelistAssignmentId = BasePricelistAssignmentId;
            target.PriorityPricelistAssignmentId = PriorityPricelistAssignmentId;

            if (!DynamicPropertyObjectValues.IsNullCollection())
            {
                DynamicPropertyObjectValues.Patch(target.DynamicPropertyObjectValues, (sourceDynamicPropertyObjectValues, targetDynamicPropertyObjectValues) => sourceDynamicPropertyObjectValues.Patch(targetDynamicPropertyObjectValues));
            }

            if (!Attachments.IsNullCollection())
            {
                Attachments.Patch(target.Attachments, (sourceAttachment, targetAttachment) => { sourceAttachment.Patch(targetAttachment); });
            }
        }
    }
}
