using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Terminology.Data.Migrations;

[DbContext(typeof(TerminologyDbContext))]
partial class TerminologyDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyAlias", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<string>("Alias")
                .IsRequired()
                .HasMaxLength(400)
                .HasColumnType("character varying(400)")
                .HasColumnName("alias");

            b.Property<string>("AliasNorm")
                .IsRequired()
                .HasMaxLength(400)
                .HasColumnType("character varying(400)")
                .HasColumnName("alias_norm");

            b.Property<Guid>("ConceptId")
                .HasColumnType("uuid")
                .HasColumnName("concept_id");

            b.Property<string>("ConceptCode")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("concept_code");

            b.Property<Guid>("CodeVersionId")
                .HasColumnType("uuid")
                .HasColumnName("code_version_id");

            b.Property<decimal>("Weight")
                .HasColumnType("numeric(4,2)")
                .HasColumnName("weight");

            b.HasKey("Id");

            b.HasIndex("ConceptId")
                .HasDatabaseName("ix_terminology_alias_concept_id");

            b.HasIndex("CodeVersionId", "ConceptCode", "AliasNorm")
                .IsUnique()
                .HasDatabaseName("ux_terminology_alias_code_version_code_alias_norm");

            b.ToTable("terminology_alias", (string)null);
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyCodeVersion", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<string>("CodeSystem")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)")
                .HasColumnName("code_system");

            b.Property<string>("CodeVersionId")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)")
                .HasColumnName("code_version_id");

            b.Property<DateOnly>("EffectiveDate")
                .HasColumnType("date")
                .HasColumnName("effective_date");

            b.HasKey("Id");

            b.ToTable("terminology_code_version", (string)null);
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyConcept", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<Guid>("CodeVersionId")
                .HasColumnType("uuid")
                .HasColumnName("code_version_id");

            b.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("code");

            b.Property<string>("CodeSystem")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)")
                .HasColumnName("code_system");

            b.Property<bool>("IsBillable")
                .HasColumnType("boolean")
                .HasColumnName("is_billable");

            b.Property<bool>("IsHeader")
                .HasColumnType("boolean")
                .HasColumnName("is_header");

            b.Property<string>("LongDescription")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)")
                .HasColumnName("long_description");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("status");

            b.Property<string>("SearchText")
                .IsRequired()
                .HasColumnType("text")
                .HasColumnName("search_text");

            b.Property<string>("SearchTsv")
                .IsRequired()
                .HasColumnType("tsvector")
                .HasColumnName("search_tsv");

            b.Property<string>("ShortDescription")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("character varying(300)")
                .HasColumnName("short_description");

            b.HasKey("Id");

            b.HasIndex("CodeVersionId")
                .HasDatabaseName("ix_terminology_concept_code_version_id");

            b.HasIndex("CodeVersionId", "Code")
                .IsUnique()
                .HasDatabaseName("ux_terminology_concept_code_version_code");

            b.ToTable("terminology_concept", (string)null);
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyEmbedding", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<Guid>("ConceptId")
                .HasColumnType("uuid")
                .HasColumnName("concept_id");

            b.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("code");

            b.Property<Guid>("CodeVersionId")
                .HasColumnType("uuid")
                .HasColumnName("code_version_id");

            b.Property<float[]>("Embedding")
                .IsRequired()
                .HasColumnType("vector(1536)")
                .HasColumnName("embedding");

            b.Property<string>("Model")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("model");

            b.HasKey("Id");

            b.HasIndex("ConceptId")
                .HasDatabaseName("ix_terminology_embedding_concept_id");

            b.HasIndex("CodeVersionId", "Code", "Model")
                .IsUnique()
                .HasDatabaseName("ux_terminology_embedding_code_version_code_model");

            b.ToTable("terminology_embedding", (string)null);
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyAlias", b =>
        {
            b.HasOne("Terminology.Data.Entities.TerminologyConcept", "Concept")
                .WithMany("Aliases")
                .HasForeignKey("ConceptId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Concept");
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyConcept", b =>
        {
            b.HasOne("Terminology.Data.Entities.TerminologyCodeVersion", "CodeVersion")
                .WithMany("Concepts")
                .HasForeignKey("CodeVersionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("CodeVersion");
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyEmbedding", b =>
        {
            b.HasOne("Terminology.Data.Entities.TerminologyConcept", "Concept")
                .WithMany("Embeddings")
                .HasForeignKey("ConceptId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Concept");
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyCodeVersion", b =>
        {
            b.Navigation("Concepts");
        });

        modelBuilder.Entity("Terminology.Data.Entities.TerminologyConcept", b =>
        {
            b.Navigation("Aliases");
            b.Navigation("Embeddings");
        });
    }
}
