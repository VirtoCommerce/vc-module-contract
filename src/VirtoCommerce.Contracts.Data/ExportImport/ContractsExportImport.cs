using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;

namespace VirtoCommerce.Contracts.Data.ExportImport;

public class ContractsExportImport(
    IContractSearchService contractSearchService,
    IContractService contractService,
    JsonSerializer jsonSerializer)
{
    private const int BatchSize = 50;

    public async Task DoExportAsync(Stream outStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo { Description = "loading data..." };
        progressCallback(progressInfo);

        await using var sw = new StreamWriter(outStream);
        await using var writer = new JsonTextWriter(sw);
        await writer.WriteStartObjectAsync();

        progressInfo.Description = "Contracts are started to export";
        progressCallback(progressInfo);

        await writer.WritePropertyNameAsync("Contracts");
        await writer.SerializeArrayWithPagingAsync(jsonSerializer, BatchSize, async (skip, take) =>
        {
            var searchCriteria = AbstractTypeFactory<ContractSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = take;
            searchCriteria.Skip = skip;
            // searchCriteria.ResponseGroup = ContractResponseGroup.Full.ToString();
            var searchResult = await contractSearchService.SearchAsync(searchCriteria);
            return (GenericSearchResult<Contract>)searchResult;
        }, (processedCount, totalCount) =>
        {
            progressInfo.Description = $"{processedCount} of {totalCount} contracts have been exported";
            progressCallback(progressInfo);
        }, cancellationToken);

        await writer.WriteEndObjectAsync();
        await writer.FlushAsync();
    }

    public async Task DoImportAsync(Stream inputStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo();

        using var streamReader = new StreamReader(inputStream);
        await using var reader = new JsonTextReader(streamReader);
        while (await reader.ReadAsync())
        {
            if (reader.TokenType == JsonToken.PropertyName &&
                reader.Value?.ToString() == "Contracts")
            {
                await SafeDeserializeArrayWithPagingAsync<Contract>(reader, jsonSerializer, BatchSize, progressInfo,
                    items => contractService.SaveChangesAsync(items.ToArray()),
                    processedCount =>
                    {
                        progressInfo.Description = $"{processedCount} contracts have been imported";
                        progressCallback(progressInfo);
                    }, cancellationToken);
            }
        }
    }

    private static async Task SafeDeserializeArrayWithPagingAsync<T>(JsonTextReader reader, JsonSerializer serializer, int pageSize,
       ExportImportProgressInfo progressInfo, Func<IList<T>, Task> action, Action<int> progressCallback, ICancellationToken cancellationToken)
    {
        await reader.ReadAsync();
        if (reader.TokenType == JsonToken.StartArray)
        {
            await reader.ReadAsync();

            var items = new List<T>();
            var processedCount = 0;
            while (reader.TokenType != JsonToken.EndArray)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = serializer.Deserialize<T>(reader);
                    items.Add(item);
                }
                catch (Exception ex)
                {
                    progressInfo.Errors.Add($"Warning. Skip import for the template. Could not deserialize it. More details: {ex}");
                }

                processedCount++;
                await reader.ReadAsync();
                if (processedCount % pageSize == 0 || reader.TokenType == JsonToken.EndArray)
                {
                    await action(items);
                    items.Clear();
                    progressCallback(processedCount);
                }
            }
        }
    }
}


