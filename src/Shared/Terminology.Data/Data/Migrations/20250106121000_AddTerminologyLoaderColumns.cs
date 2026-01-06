using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminology.Data.Migrations;

public partial class AddTerminologyLoaderColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "code_system",
            table: "terminology_concept",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "status",
            table: "terminology_concept",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<Guid>(
            name: "code_version_id",
            table: "terminology_alias",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<string>(
            name: "concept_code",
            table: "terminology_alias",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "weight",
            table: "terminology_alias",
            type: "numeric(4,2)",
            nullable: false,
            defaultValue: 1.0m);

        migrationBuilder.AddColumn<Guid>(
            name: "code_version_id",
            table: "terminology_embedding",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<string>(
            name: "code",
            table: "terminology_embedding",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "ux_terminology_concept_code_version_code",
            table: "terminology_concept",
            columns: new[] { "code_version_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_terminology_alias_code_version_code_alias_norm",
            table: "terminology_alias",
            columns: new[] { "code_version_id", "concept_code", "alias_norm" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_terminology_embedding_code_version_code_model",
            table: "terminology_embedding",
            columns: new[] { "code_version_id", "code", "model" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_terminology_concept_code_version_code",
            table: "terminology_concept");

        migrationBuilder.DropIndex(
            name: "ux_terminology_alias_code_version_code_alias_norm",
            table: "terminology_alias");

        migrationBuilder.DropIndex(
            name: "ux_terminology_embedding_code_version_code_model",
            table: "terminology_embedding");

        migrationBuilder.DropColumn(
            name: "code_system",
            table: "terminology_concept");

        migrationBuilder.DropColumn(
            name: "status",
            table: "terminology_concept");

        migrationBuilder.DropColumn(
            name: "code_version_id",
            table: "terminology_alias");

        migrationBuilder.DropColumn(
            name: "concept_code",
            table: "terminology_alias");

        migrationBuilder.DropColumn(
            name: "weight",
            table: "terminology_alias");

        migrationBuilder.DropColumn(
            name: "code_version_id",
            table: "terminology_embedding");

        migrationBuilder.DropColumn(
            name: "code",
            table: "terminology_embedding");
    }
}
