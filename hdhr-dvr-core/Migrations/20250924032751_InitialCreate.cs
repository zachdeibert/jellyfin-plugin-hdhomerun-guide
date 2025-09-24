using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Com.ZachDeibert.MediaTools.Hdhr.Dvr.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata_SeriesId = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Title = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Category = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata_ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_PosterUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_IsNew = table.Column<bool>(type: "INTEGER", nullable: true),
                    Metadata_Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeriesStartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Metadata_Category = table.Column<int>(type: "INTEGER", nullable: true),
                    Metadata_ChannelImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_ChannelName = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_ChannelNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_EndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_EpisodeNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_EpisodeTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_FirstAiring = table.Column<bool>(type: "INTEGER", nullable: true),
                    Metadata_ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_MovieScore = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_OriginalAirdate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_PosterUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_ProgramId = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_RecordEndTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_RecordError = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_RecordStartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_RecordSuccess = table.Column<bool>(type: "INTEGER", nullable: true),
                    Metadata_SeriesId = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_StartTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Metadata_Synopsis = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Title = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Filename = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_PlayUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_CmdUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadInterrupted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadStarted = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DownloadReason = table.Column<int>(type: "INTEGER", nullable: false),
                    DeleteReason = table.Column<int>(type: "INTEGER", nullable: false),
                    ReRecordable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodes_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_SeriesId",
                table: "Episodes",
                column: "SeriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
