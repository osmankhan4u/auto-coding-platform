using Microsoft.EntityFrameworkCore;
using Terminology.Api.Data.Entities;

namespace Terminology.Api.Data;

public sealed class TerminologyDbContext : DbContext
{
    public TerminologyDbContext(DbContextOptions<TerminologyDbContext> options) : base(options)
    {
    }

    public DbSet<TerminologyCodeVersion> CodeVersions => Set<TerminologyCodeVersion>();
    public DbSet<TerminologyConcept> Concepts => Set<TerminologyConcept>();
    public DbSet<TerminologyAlias> Aliases => Set<TerminologyAlias>();
    public DbSet<TerminologyEmbedding> Embeddings => Set<TerminologyEmbedding>();
    public DbSet<TerminologySearchRow> SearchRows => Set<TerminologySearchRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TerminologyCodeVersion>(entity =>
        {
            entity.ToTable("terminology_code_version");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CodeSystem).HasColumnName("code_system").HasMaxLength(50);
            entity.Property(e => e.CodeVersionId).HasColumnName("code_version_id").HasMaxLength(50);
            entity.Property(e => e.EffectiveDate).HasColumnName("effective_date");
        });

        modelBuilder.Entity<TerminologyConcept>(entity =>
        {
            entity.ToTable("terminology_concept");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CodeVersionId).HasColumnName("code_version_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(20);
            entity.Property(e => e.ShortDescription).HasColumnName("short_description").HasMaxLength(300);
            entity.Property(e => e.LongDescription).HasColumnName("long_description").HasMaxLength(1000);
            entity.Property(e => e.IsBillable).HasColumnName("is_billable");
            entity.Property(e => e.IsHeader).HasColumnName("is_header");
            entity.Property(e => e.SearchText).HasColumnName("search_text");
            entity.Property(e => e.SearchTsv).HasColumnName("search_tsv").HasColumnType("tsvector");

            entity.HasOne(e => e.CodeVersion)
                .WithMany(c => c.Concepts)
                .HasForeignKey(e => e.CodeVersionId);
        });

        modelBuilder.Entity<TerminologyAlias>(entity =>
        {
            entity.ToTable("terminology_alias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConceptId).HasColumnName("concept_id");
            entity.Property(e => e.Alias).HasColumnName("alias").HasMaxLength(400);
            entity.Property(e => e.AliasNorm).HasColumnName("alias_norm").HasMaxLength(400);

            entity.HasOne(e => e.Concept)
                .WithMany(c => c.Aliases)
                .HasForeignKey(e => e.ConceptId);
        });

        modelBuilder.Entity<TerminologyEmbedding>(entity =>
        {
            entity.ToTable("terminology_embedding");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConceptId).HasColumnName("concept_id");
            entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(100);
            entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");

            entity.HasOne(e => e.Concept)
                .WithMany(c => c.Embeddings)
                .HasForeignKey(e => e.ConceptId);
        });

        modelBuilder.Entity<TerminologySearchRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.ShortDescription).HasColumnName("short_description");
            entity.Property(e => e.LongDescription).HasColumnName("long_description");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.MatchModes).HasColumnName("match_modes");
            entity.Property(e => e.MatchedTerms).HasColumnName("matched_terms");
        });
    }
}
