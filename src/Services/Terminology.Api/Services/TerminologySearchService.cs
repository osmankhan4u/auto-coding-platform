using Microsoft.EntityFrameworkCore;
using Npgsql;
using Terminology.Data;
using Terminology.Data.Services;
using Terminology.Api.Models;

namespace Terminology.Api.Services;

public sealed class TerminologySearchService
{
    private readonly TerminologyDbContext _dbContext;
    private readonly IEmbeddingProvider _embeddingProvider;

    public TerminologySearchService(TerminologyDbContext dbContext, IEmbeddingProvider embeddingProvider)
    {
        _dbContext = dbContext;
        _embeddingProvider = embeddingProvider;
    }

    public async Task<IReadOnlyList<TerminologyHitDto>> SearchAsync(
        TerminologySearchRequest request,
        Guid codeVersionId,
        CancellationToken cancellationToken)
    {
        var isBillableOnly = ParseFlag(request.IsBillableOnly);
        var excludeHeaders = ParseFlag(request.ExcludeHeaders);
        var topN = Math.Clamp(request.TopN, 1, 50);
        var queryText = request.QueryText ?? string.Empty;
        var embedding = await _embeddingProvider.EmbedAsync(queryText, cancellationToken);

        var sql = """
        WITH query_params AS (
            SELECT
                @queryText::text AS query_text,
                websearch_to_tsquery('simple', unaccent(@queryText::text)) AS ts_query
        ),
        base AS (
            SELECT
                c.id,
                c.code,
                c.short_description,
                c.long_description,
                c.is_billable,
                c.is_header,
                ts_rank_cd(c.search_tsv, qp.ts_query) AS fts_rank,
                similarity(c.search_text, unaccent(qp.query_text)) AS trgm_sim
            FROM terminology_concept c
            CROSS JOIN query_params qp
            WHERE c.code_version_id = @codeVersionId
        ),
        alias_rank AS (
            SELECT
                a.concept_id,
                max(similarity(a.alias_norm, unaccent(qp.query_text))) AS alias_trgm,
                array_agg(a.alias) FILTER (WHERE similarity(a.alias_norm, unaccent(qp.query_text)) > 0.30) AS matched_terms
            FROM terminology_alias a
            CROSS JOIN query_params qp
            GROUP BY a.concept_id
        ),
        vec_rank AS (
            SELECT
                e.concept_id,
                (1 - (e.embedding <=> @qvec)) AS vec_sim
            FROM terminology_embedding e
            WHERE e.model = @modelId
        )
        SELECT
            b.code,
            b.short_description,
            b.long_description,
            (0.55 * COALESCE(b.fts_rank, 0)
             + 0.15 * COALESCE(b.trgm_sim, 0)
             + 0.30 * COALESCE(v.vec_sim, 0)
             + CASE WHEN COALESCE(a.alias_trgm, 0) > 0.50 THEN 0.05 ELSE 0 END) AS score,
            ARRAY_REMOVE(ARRAY[
                CASE WHEN b.fts_rank > 0 THEN 'FTS' END,
                CASE WHEN v.vec_sim > 0 THEN 'VECTOR' END,
                CASE WHEN COALESCE(b.trgm_sim, 0) > 0 OR COALESCE(a.alias_trgm, 0) > 0 THEN 'ALIAS' END
            ], NULL) AS match_modes,
            COALESCE(a.matched_terms, ARRAY[]::text[]) AS matched_terms
        FROM base b
        LEFT JOIN alias_rank a ON a.concept_id = b.id
        LEFT JOIN vec_rank v ON v.concept_id = b.id
        WHERE (@isBillableOnly = FALSE OR b.is_billable = TRUE)
          AND (@excludeHeaders = FALSE OR b.is_header = FALSE)
          AND (COALESCE(b.fts_rank, 0) > 0 OR COALESCE(b.trgm_sim, 0) > 0 OR COALESCE(v.vec_sim, 0) > 0 OR COALESCE(a.alias_trgm, 0) > 0)
        ORDER BY score DESC
        LIMIT @topN;
        """;

        var parameters = new[]
        {
            new NpgsqlParameter("queryText", queryText),
            new NpgsqlParameter("codeVersionId", codeVersionId),
            new NpgsqlParameter("qvec", embedding),
            new NpgsqlParameter("modelId", _embeddingProvider.ModelId),
            new NpgsqlParameter("isBillableOnly", isBillableOnly),
            new NpgsqlParameter("excludeHeaders", excludeHeaders),
            new NpgsqlParameter("topN", topN)
        };

        var results = await _dbContext.SearchRows
            .FromSqlRaw(sql, parameters)
            .ToListAsync(cancellationToken);

        return results.Select(row => new TerminologyHitDto
        {
            Code = row.Code,
            ShortDescription = row.ShortDescription,
            LongDescription = row.LongDescription,
            Score = Math.Clamp(row.Score, 0, 1),
            MatchModes = row.MatchModes,
            MatchedTerms = row.MatchedTerms
        }).ToList();
    }

    private static bool ParseFlag(string? value)
    {
        return bool.TryParse(value, out var result) && result;
    }

}
