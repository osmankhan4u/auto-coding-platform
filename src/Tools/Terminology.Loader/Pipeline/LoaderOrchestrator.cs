using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminology.Data;
using Terminology.Data.Services;

namespace Terminology.Loader.Pipeline;

public sealed class LoaderOrchestrator
{
    private const int ConceptBatchSize = 2000;
    private const int AliasBatchSize = 2000;
    private const int EmbeddingBatchSize = 128;

    private readonly TerminologyDbContext _dbContext;
    private readonly LoaderOptions _options;
    private readonly Icd10CmTabularParser _tabularParser;
    private readonly Icd10CmIndexParser _indexParser;
    private readonly IEmbeddingProvider _embeddingProvider;

    public LoaderOrchestrator(
        TerminologyDbContext dbContext,
        LoaderOptions options,
        Icd10CmTabularParser tabularParser,
        Icd10CmIndexParser indexParser,
        IEmbeddingProvider embeddingProvider)
    {
        _dbContext = dbContext;
        _options = options;
        _tabularParser = tabularParser;
        _indexParser = indexParser;
        _embeddingProvider = embeddingProvider;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.InputZip))
        {
            throw new FileNotFoundException("Input zip not found.", _options.InputZip);
        }

        var totalStopwatch = Stopwatch.StartNew();
        await _dbContext.Database.OpenConnectionAsync(cancellationToken);

        var codeVersionGuid = _options.ResolveCodeVersionGuid();

        var versionStopwatch = Stopwatch.StartNew();
        await EnsureCodeVersionAsync(codeVersionGuid, cancellationToken);
        versionStopwatch.Stop();

        var conceptsStopwatch = Stopwatch.StartNew();
        var conceptCounts = await LoadConceptsAsync(codeVersionGuid, cancellationToken);
        conceptsStopwatch.Stop();

        var searchStopwatch = Stopwatch.StartNew();
        await UpdateSearchFieldsAsync(codeVersionGuid, cancellationToken);
        searchStopwatch.Stop();

        StageCounts aliasCounts = new(0, 0);
        if (_options.Aliases)
        {
            var aliasStopwatch = Stopwatch.StartNew();
            aliasCounts = await LoadAliasesAsync(codeVersionGuid, cancellationToken);
            aliasStopwatch.Stop();
            Console.WriteLine($"Aliases: inserted={aliasCounts.Inserted}, updated={aliasCounts.Updated}, elapsed={aliasStopwatch.Elapsed}");
        }

        StageCounts embeddingCounts = new(0, 0);
        if (_options.Embed)
        {
            var embedStopwatch = Stopwatch.StartNew();
            embeddingCounts = await LoadEmbeddingsAsync(codeVersionGuid, cancellationToken);
            embedStopwatch.Stop();
            Console.WriteLine($"Embeddings: inserted={embeddingCounts.Inserted}, updated={embeddingCounts.Updated}, elapsed={embedStopwatch.Elapsed}");
        }

        Console.WriteLine($"CodeVersion: elapsed={versionStopwatch.Elapsed}");
        Console.WriteLine($"Concepts: inserted={conceptCounts.Inserted}, updated={conceptCounts.Updated}, elapsed={conceptsStopwatch.Elapsed}");
        Console.WriteLine($"SearchFields: elapsed={searchStopwatch.Elapsed}");
        Console.WriteLine($"Total: elapsed={totalStopwatch.Elapsed}");
    }

    private async Task EnsureCodeVersionAsync(Guid codeVersionId, CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO terminology_code_version (id, code_system, code_version_id, effective_date)
        VALUES (@id, @codeSystem, @codeVersionId, @effectiveDate)
        ON CONFLICT (id) DO UPDATE
        SET code_system = EXCLUDED.code_system,
            code_version_id = EXCLUDED.code_version_id,
            effective_date = EXCLUDED.effective_date;
        """;

        var parameters = new[]
        {
            new NpgsqlParameter("id", codeVersionId),
            new NpgsqlParameter("codeSystem", _options.CodeSystem),
            new NpgsqlParameter("codeVersionId", _options.CodeVersionId),
            new NpgsqlParameter("effectiveDate", _options.EffectiveFrom)
        };

        await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    private async Task<StageCounts> LoadConceptsAsync(Guid codeVersionId, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var batch = new List<ConceptRow>(ConceptBatchSize);

        foreach (var concept in _tabularParser.ParseConcepts(_options.InputZip))
        {
            batch.Add(concept);
            if (batch.Count >= ConceptBatchSize)
            {
                var counts = await UpsertConceptBatchAsync(codeVersionId, batch, cancellationToken);
                inserted += counts.Inserted;
                updated += counts.Updated;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            var counts = await UpsertConceptBatchAsync(codeVersionId, batch, cancellationToken);
            inserted += counts.Inserted;
            updated += counts.Updated;
        }

        return new StageCounts(inserted, updated);
    }

    private async Task<StageCounts> LoadAliasesAsync(Guid codeVersionId, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var batch = new List<AliasRow>(AliasBatchSize);

        foreach (var alias in _indexParser.ParseAliases(_options.InputZip))
        {
            batch.Add(alias);
            if (batch.Count >= AliasBatchSize)
            {
                var counts = await UpsertAliasBatchAsync(codeVersionId, batch, cancellationToken);
                inserted += counts.Inserted;
                updated += counts.Updated;
                batch.Clear();
            }
        }

        foreach (var alias in LoadCuratedAliases())
        {
            batch.Add(alias);
            if (batch.Count >= AliasBatchSize)
            {
                var counts = await UpsertAliasBatchAsync(codeVersionId, batch, cancellationToken);
                inserted += counts.Inserted;
                updated += counts.Updated;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            var counts = await UpsertAliasBatchAsync(codeVersionId, batch, cancellationToken);
            inserted += counts.Inserted;
            updated += counts.Updated;
        }

        return new StageCounts(inserted, updated);
    }

    private async Task<StageCounts> LoadEmbeddingsAsync(Guid codeVersionId, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var batch = new List<ConceptRow>(EmbeddingBatchSize);

        foreach (var concept in _tabularParser.ParseConcepts(_options.InputZip))
        {
            batch.Add(concept);
            if (batch.Count >= EmbeddingBatchSize)
            {
                var counts = await UpsertEmbeddingBatchAsync(codeVersionId, batch, cancellationToken);
                inserted += counts.Inserted;
                updated += counts.Updated;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            var counts = await UpsertEmbeddingBatchAsync(codeVersionId, batch, cancellationToken);
            inserted += counts.Inserted;
            updated += counts.Updated;
        }

        return new StageCounts(inserted, updated);
    }

    private async Task UpdateSearchFieldsAsync(Guid codeVersionId, CancellationToken cancellationToken)
    {
        const string sql = """
        UPDATE terminology_concept
        SET search_text = unaccent(lower(code || ' ' || short_description || ' ' || long_description)),
            search_tsv = to_tsvector('english', unaccent(lower(code || ' ' || short_description || ' ' || long_description)))
        WHERE code_version_id = @codeVersionId;
        """;

        var parameters = new[] { new NpgsqlParameter("codeVersionId", codeVersionId) };
        await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    private async Task<StageCounts> UpsertConceptBatchAsync(
        Guid codeVersionId,
        IReadOnlyList<ConceptRow> concepts,
        CancellationToken cancellationToken)
    {
        var values = new List<string>(concepts.Count);
        var parameters = new List<NpgsqlParameter>(concepts.Count * 8 + 2)
        {
            new NpgsqlParameter("codeVersionId", codeVersionId),
            new NpgsqlParameter("codeSystem", _options.CodeSystem),
            new NpgsqlParameter("status", "ACTIVE")
        };

        for (var i = 0; i < concepts.Count; i++)
        {
            var concept = concepts[i];
            values.Add($"(@id{i}, @codeVersionId, @code{i}, @short{i}, @long{i}, @billable{i}, @header{i}, '', to_tsvector('english',''), @codeSystem, @status)");
            parameters.Add(new NpgsqlParameter($"id{i}", CreateDeterministicGuid($"{codeVersionId:N}:{concept.Code}")));
            parameters.Add(new NpgsqlParameter($"code{i}", concept.Code));
            parameters.Add(new NpgsqlParameter($"short{i}", concept.ShortDesc));
            parameters.Add(new NpgsqlParameter($"long{i}", concept.LongDesc));
            parameters.Add(new NpgsqlParameter($"billable{i}", concept.IsBillable));
            parameters.Add(new NpgsqlParameter($"header{i}", concept.IsHeader));
        }

        var sql = $"""
        INSERT INTO terminology_concept
            (id, code_version_id, code, short_description, long_description, is_billable, is_header, search_text, search_tsv, code_system, status)
        VALUES
            {string.Join(",", values)}
        ON CONFLICT (code_version_id, code) DO UPDATE SET
            short_description = EXCLUDED.short_description,
            long_description = EXCLUDED.long_description,
            is_billable = EXCLUDED.is_billable,
            is_header = EXCLUDED.is_header,
            code_system = EXCLUDED.code_system,
            status = EXCLUDED.status
        RETURNING (xmax = 0) AS inserted;
        """;

        return await ExecuteBatchAsync(sql, parameters, cancellationToken);
    }

    private async Task<StageCounts> UpsertAliasBatchAsync(
        Guid codeVersionId,
        IReadOnlyList<AliasRow> aliases,
        CancellationToken cancellationToken)
    {
        var values = new List<string>(aliases.Count);
        var parameters = new List<NpgsqlParameter>(aliases.Count * 4 + 1)
        {
            new NpgsqlParameter("codeVersionId", codeVersionId)
        };

        for (var i = 0; i < aliases.Count; i++)
        {
            var alias = aliases[i];
            values.Add($"(@id{i}, @conceptCode{i}, @alias{i}, @weight{i})");
            parameters.Add(new NpgsqlParameter($"id{i}", CreateDeterministicGuid($"{codeVersionId:N}:{alias.ConceptCode}:{alias.AliasText}")));
            parameters.Add(new NpgsqlParameter($"conceptCode{i}", alias.ConceptCode));
            parameters.Add(new NpgsqlParameter($"alias{i}", alias.AliasText));
            parameters.Add(new NpgsqlParameter($"weight{i}", alias.Weight));
        }

        var sql = $"""
        WITH alias_rows (id, concept_code, alias_text, weight) AS (
            VALUES {string.Join(",", values)}
        )
        INSERT INTO terminology_alias (id, concept_id, alias, alias_norm, code_version_id, concept_code, weight)
        SELECT
            ar.id,
            c.id,
            ar.alias_text,
            unaccent(lower(ar.alias_text)),
            @codeVersionId,
            ar.concept_code,
            ar.weight
        FROM alias_rows ar
        JOIN terminology_concept c
            ON c.code_version_id = @codeVersionId
           AND c.code = ar.concept_code
        ON CONFLICT (code_version_id, concept_code, alias_norm) DO UPDATE SET
            alias = EXCLUDED.alias,
            concept_id = EXCLUDED.concept_id,
            weight = EXCLUDED.weight
        RETURNING (xmax = 0) AS inserted;
        """;

        return await ExecuteBatchAsync(sql, parameters, cancellationToken);
    }

    private async Task<StageCounts> UpsertEmbeddingBatchAsync(
        Guid codeVersionId,
        IReadOnlyList<ConceptRow> concepts,
        CancellationToken cancellationToken)
    {
        var texts = concepts
            .Select(concept => $"{concept.Code} {concept.LongDesc}".Trim())
            .ToArray();

        var embeddings = await Task.WhenAll(texts.Select(text =>
            _embeddingProvider.EmbedAsync(text, cancellationToken)));

        var values = new List<string>(concepts.Count);
        var parameters = new List<NpgsqlParameter>(concepts.Count * 3 + 2)
        {
            new NpgsqlParameter("codeVersionId", codeVersionId),
            new NpgsqlParameter("modelId", _options.ModelId)
        };

        for (var i = 0; i < concepts.Count; i++)
        {
            var concept = concepts[i];
            values.Add($"(@id{i}, @code{i}, @embedding{i})");
            parameters.Add(new NpgsqlParameter($"id{i}", CreateDeterministicGuid($"{codeVersionId:N}:{concept.Code}:{_options.ModelId}")));
            parameters.Add(new NpgsqlParameter($"code{i}", concept.Code));
            parameters.Add(new NpgsqlParameter($"embedding{i}", embeddings[i]));
        }

        var sql = $"""
        WITH embed_rows (id, code, embedding) AS (
            VALUES {string.Join(",", values)}
        )
        INSERT INTO terminology_embedding (id, concept_id, code_version_id, code, model, embedding)
        SELECT
            er.id,
            c.id,
            @codeVersionId,
            er.code,
            @modelId,
            er.embedding
        FROM embed_rows er
        JOIN terminology_concept c
            ON c.code_version_id = @codeVersionId
           AND c.code = er.code
        ON CONFLICT (code_version_id, code, model) DO UPDATE SET
            embedding = EXCLUDED.embedding,
            concept_id = EXCLUDED.concept_id
        RETURNING (xmax = 0) AS inserted;
        """;

        return await ExecuteBatchAsync(sql, parameters, cancellationToken);
    }

    private async Task<StageCounts> ExecuteBatchAsync(
        string sql,
        IReadOnlyList<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)_dbContext.Database.GetDbConnection());
        command.Parameters.AddRange(parameters.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var inserted = 0;
        var updated = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.GetBoolean(0))
            {
                inserted++;
            }
            else
            {
                updated++;
            }
        }

        return new StageCounts(inserted, updated);
    }

    private IEnumerable<AliasRow> LoadCuratedAliases()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "data", "aliases", "radiology_aliases_v1.csv");
        if (!File.Exists(path))
        {
            return Array.Empty<AliasRow>();
        }

        var aliases = new List<AliasRow>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("alias_text", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var aliasText = parts[0];
            var conceptCode = parts[1];
            var weight = 1.0m;
            if (parts.Length > 2 && decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedWeight))
            {
                weight = parsedWeight;
            }
            if (string.IsNullOrWhiteSpace(aliasText) || string.IsNullOrWhiteSpace(conceptCode))
            {
                continue;
            }

            aliases.Add(new AliasRow(conceptCode, aliasText, weight));
        }

        return aliases;
    }

    private readonly record struct StageCounts(int Inserted, int Updated);

    private static Guid CreateDeterministicGuid(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
