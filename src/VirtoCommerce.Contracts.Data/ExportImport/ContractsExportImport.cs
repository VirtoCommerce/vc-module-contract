using System;

using System.Threading;
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

    public async Task DoExportAsync(Stream outStream, Action<ExportImportProgressInfo> progressCallback, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo { Description = "loading data..." };
        progressCallback(progressInfo);

        await using var sw = new StreamWriter(outStream);
        await using var writer = new JsonTextWriter(sw);
        await writer.WriteStartObjectAsync(cancellationToken);

        progressInfo.Description = "Contracts are started to export";
        progressCallback(progressInfo);

        await writer.WritePropertyNameAsync("Contracts", cancellationToken);
        await writer.SerializeArrayWithPagingAsync(jsonSerializer, BatchSize, async (skip, take) =>
        {
            var searchCriteria = AbstractTypeFactory<ContractSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = take;
            searchCriteria.Skip = skip;
            var searchResult = await contractSearchService.SearchAsync(searchCriteria);
            return (GenericSearchResult<Contract>)searchResult;
        }, (processedCount, totalCount) =>
        {
            progressInfo.Description = $"{processedCount} of {totalCount} contracts have been exported";
            progressCallback(progressInfo);
        }, cancellationToken);

        await writer.WriteEndObjectAsync(cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    public async Task DoImportAsync(Stream inputStream, Action<ExportImportProgressInfo> progressCallback, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progressInfo = new ExportImportProgressInfo();

        using var streamReader = new StreamReader(inputStream);
        await using var reader = new JsonTextReader(streamReader);
        while (await reader.ReadAsync(cancellationToken))
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
       ExportImportProgressInfo progressInfo, Func<IList<T>, Task> action, Action<int> progressCallback, CancellationToken cancellationToken)
    {
        await reader.ReadAsync(cancellationToken);
        if (reader.TokenType == JsonToken.StartArray)
        {
            await reader.ReadAsync(cancellationToken);

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
                await reader.ReadAsync(cancellationToken);
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


