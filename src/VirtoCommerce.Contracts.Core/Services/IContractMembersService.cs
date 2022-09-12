using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;

namespace VirtoCommerce.Contracts.Core.Services
{
    public interface IContractMembersService
    {
        Task AddContractMembers(ContractMembers contractMembersRelation);

        Task DeleteContractMembers(ContractMembers contractMembersRelation);
    }
}
