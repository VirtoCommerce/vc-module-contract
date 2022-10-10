using System.Text.RegularExpressions;

namespace VirtoCommerce.Contracts.Data.Extensions
{
    public static class StringExtensions
    {
        public static string ToContractCode(this string name)
        {
            var contractCode = name.ToLowerInvariant();

            contractCode = Regex.Replace(contractCode, @"\W+", "-", RegexOptions.Compiled);

            contractCode = $"contract-{contractCode}";

            return contractCode;
        }
    }
}
