using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminology.Data.Migrations;

public partial class InitialTerminologySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\";");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"unaccent\";");
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"vector\";");

        migrationBuilder.CreateTable(
            name: "terminology_code_version",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                code_version_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                effective_date = table.Column<DateOnly>(type: "date", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_terminology_code_version", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "terminology_concept",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                short_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                long_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                is_billable = table.Column<bool>(type: "boolean", nullable: false),
                is_header = table.Column<bool>(type: "boolean", nullable: false),
                search_text = table.Column<string>(type: "text", nullable: false),
                search_tsv = table.Column<string>(type: "tsvector", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_terminology_concept", x => x.id);
                table.ForeignKey(
                    name: "fk_terminology_concept_code_version",
                    column: x => x.code_version_id,
                    principalTable: "terminology_code_version",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "terminology_alias",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                alias = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                alias_norm = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_terminology_alias", x => x.id);
                table.ForeignKey(
                    name: "fk_terminology_alias_concept",
                    column: x => x.concept_id,
                    principalTable: "terminology_concept",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "terminology_embedding",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                concept_id = table.Column<Guid>(type: "uuid", nullable: false),
                model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                embedding = table.Column<float[]>(type: "vector(1536)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_terminology_embedding", x => x.id);
                table.ForeignKey(
                    name: "fk_terminology_embedding_concept",
                    column: x => x.concept_id,
                    principalTable: "terminology_concept",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_terminology_concept_code_version_id",
            table: "terminology_concept",
            column: "code_version_id");

        migrationBuilder.CreateIndex(
            name: "ix_terminology_alias_concept_id",
            table: "terminology_alias",
            column: "concept_id");

        migrationBuilder.CreateIndex(
            name: "ix_terminology_embedding_concept_id",
            table: "terminology_embedding",
            column: "concept_id");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_terminology_concept_search_tsv ON terminology_concept USING GIN (search_tsv);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_terminology_concept_search_text_trgm ON terminology_concept USING GIN (search_text gin_trgm_ops);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_terminology_alias_alias_norm_trgm ON terminology_alias USING GIN (alias_norm gin_trgm_ops);");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_terminology_embedding_vector ON terminology_embedding USING ivfflat (embedding vector_cosine_ops);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminology_embedding_vector;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminology_alias_alias_norm_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminology_concept_search_text_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_terminology_concept_search_tsv;");

        migrationBuilder.DropTable(name: "terminology_embedding");
        migrationBuilder.DropTable(name: "terminology_alias");
        migrationBuilder.DropTable(name: "terminology_concept");
        migrationBuilder.DropTable(name: "terminology_code_version");
    }
}
