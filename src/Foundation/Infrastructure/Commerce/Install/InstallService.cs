using EPiServer.Logging;
using Mediachase.Commerce.Catalog.ImportExport;
using Mediachase.Data.Provider;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;

namespace Foundation.Infrastructure.Commerce.Install
{
    public class InstallService : IInstallService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(InstallService));
        private readonly IConnectionStringHandler _connectionStringHandler;
        private FoundationConfiguration _foundationConfiguration;
        private InstallProgressMessenger _progressMessenger;
        private IEnumerable<IInstallStep> _installSteps;

        public InstallService(IConnectionStringHandler connectionStringHandler) => _connectionStringHandler = connectionStringHandler;

        public IEnumerable<IInstallStep> InstallSteps
        {
            get => _installSteps ?? (_installSteps = ServiceLocator.Current.GetAllInstances<IInstallStep>());
            set => _installSteps = value;
        }

        public InstallProgressMessenger ProgressMessenger
        {
            get => _progressMessenger ?? (_progressMessenger = new InstallProgressMessenger());
            set => _progressMessenger = value;
        }

        public FoundationConfiguration FoundationConfiguration => _foundationConfiguration ?? (_foundationConfiguration = GetFoundationConfiguration());

        public Stream ExportCatalog(string name)
        {
            try
            {
                var stream = new MemoryStream();
                new CatalogImportExport
                {
                    IsModelsAvailable = true
                }.Export(name, stream, "");
                stream.Position = 0;
                return stream;
            }
            catch (Exception exception)
            {
                _logger.Error(exception.Message, exception);
                ProgressMessenger.AddProgressMessageText(exception.Message, true, 100);
            }

            return null;
        }

        public void RunInstallSteps()
        {
            foreach (var step in InstallSteps.OrderBy(x => x.Order))
            {
                var next = InstallStep(step);
                if (!next)
                {
                    return;
                }
            }
            UpdateFoundationConfiguration();
        }

        public bool ShouldInstall() => !FoundationConfiguration?.IsInstalled ?? false;

        private void UpdateFoundationConfiguration()
        {
            using (var connection = new SqlConnection(_connectionStringHandler.Commerce.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand
                {
                    Connection = connection,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "FoundationConfiguration_SetInstalled",
                };
                command.ExecuteNonQuery();
            }
        }

        private bool InstallStep(IInstallStep installStep)
        {
            ProgressMessenger.AddProgressMessageText("Starting migration step: " + installStep.Name, false, 0);
            var success = installStep.Execute(ProgressMessenger);
            ProgressMessenger.AddProgressMessageText("Completed migration step: " + installStep.Name, false, 0);
            return success;
        }

        private FoundationConfiguration GetFoundationConfiguration()
        {
            try
            {
                return QueryFoundationConfiguration();
            }
            catch (SqlException ex) when (ex.Number == 2812 || ex.Number == 208)
            {
                _logger.Information("FoundationConfiguration schema not found, creating it...");
                EnsureFoundationConfigurationSchema();
                return QueryFoundationConfiguration();
            }
        }

        private FoundationConfiguration QueryFoundationConfiguration()
        {
            using (var connection = new SqlConnection(_connectionStringHandler.Commerce.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand
                {
                    Connection = connection,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "FoundationConfiguration_List",
                };
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return new FoundationConfiguration
                        {
                            ApplicationName = reader["AppName"].ToString(),
                            IsInstalled = Convert.ToBoolean(reader["IsInstalled"]),
                        };
                    }
                }
            }

            return null;
        }

        private void EnsureFoundationConfigurationSchema()
        {
            using var connection = new SqlConnection(_connectionStringHandler.Commerce.ConnectionString);
            connection.Open();

            var schemaSql = @"
IF OBJECT_ID('dbo.FoundationConfiguration', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FoundationConfiguration](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [AppName] NVARCHAR(250) NOT NULL,
        [IsInstalled] BIT NOT NULL DEFAULT(0),
        CONSTRAINT [PK_FoundationConfiguration] PRIMARY KEY CLUSTERED ([Id] ASC));
    INSERT INTO FoundationConfiguration (AppName) VALUES ('Foundation');
END;

IF OBJECT_ID('dbo.FoundationConfiguration_List', 'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[FoundationConfiguration_List] AS BEGIN SELECT * FROM FoundationConfiguration END');

IF OBJECT_ID('dbo.FoundationConfiguration_SetInstalled', 'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[FoundationConfiguration_SetInstalled] AS BEGIN UPDATE FoundationConfiguration SET IsInstalled = 1 END');

IF OBJECT_ID('dbo.FoundationConfiguration_Save', 'P') IS NULL
    EXEC('CREATE PROCEDURE [dbo].[FoundationConfiguration_Save]
        @Id INT = 0, @AppName NVARCHAR(250), @IsInstalled BIT = 0
    AS BEGIN
        IF @Id > 0
            UPDATE FoundationConfiguration SET AppName = @AppName, IsInstalled = @IsInstalled WHERE Id = @Id
        ELSE
            INSERT INTO FoundationConfiguration (AppName, IsInstalled) VALUES (@AppName, @IsInstalled)
    END');
";
            using var command = new SqlCommand(schemaSql, connection);
            command.ExecuteNonQuery();
        }
    }
}
