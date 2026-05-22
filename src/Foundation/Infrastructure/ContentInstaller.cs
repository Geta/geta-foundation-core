using EPiServer.Applications;
using EPiServer.Enterprise;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.Shell.Security;
using Foundation.Infrastructure.Cms.Settings;
using Mediachase.Commerce.Catalog.ImportExport;
using Mediachase.Search;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;

namespace Foundation.Infrastructure
{
    public class ContentInstaller : IBlockingFirstRequestInitializer
    {
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly ContentRootService _contentRootService;
        private readonly IContentRepository _contentRepository;
        private readonly IDataImporter _dataImporter;
        private readonly ISettingsService _settingsService;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<SearchOptions> _searchOptions;
        private readonly IndexBuilder _indexBuilder;
        private readonly IPrincipalAccessor _principalAccessor;

        public ContentInstaller(
            ISiteDefinitionRepository siteDefinitionRepository,
            IApplicationRepository applicationRepository,
            ContentRootService contentRootService,
            IContentRepository contentRepository,
            IDataImporter dataImporter,
            ISettingsService settingsService,
            ILanguageBranchRepository languageBranchRepository,
            IWebHostEnvironment webHostEnvironment,
            IServiceProvider serviceProvider,
            IOptions<SearchOptions> searchOptions,
            IndexBuilder indexBuilder,
            IPrincipalAccessor principalAccessor)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _applicationRepository = applicationRepository;
            _contentRootService = contentRootService;
            _contentRepository = contentRepository;
            _dataImporter = dataImporter;
            _settingsService = settingsService;
            _languageBranchRepository = languageBranchRepository;
            _webHostEnvironment = webHostEnvironment;
            _serviceProvider = serviceProvider;
            _searchOptions = searchOptions;
            _indexBuilder = indexBuilder;
            _principalAccessor = principalAccessor;
        }

        public bool CanRunInParallel => false;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            await InstallDefaultContent(httpContext);
            await ProvisionAdminUser(httpContext);
            _settingsService.InitializeSettings();
        }

        private async Task InstallDefaultContent(HttpContext context)
        {
            if (_siteDefinitionRepository.List().Any())
                return;

            var appDataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data");
            var episerverdataPath = Path.Combine(appDataPath, "foundation-cms13.episerverdata");
            if (!File.Exists(episerverdataPath))
                episerverdataPath = Path.Combine(appDataPath, "foundation.episerverdata");

            if (!File.Exists(episerverdataPath))
                return;

            var registeredRoots = _contentRepository.GetItems(_contentRootService.List(), new LoaderOptions());
            var settingsRootRegistered = registeredRoots.Any(x =>
                x.ContentGuid == SettingsFolder.SettingsRootGuid &&
                x.Name.Equals(SettingsFolder.SettingsRootName));

            if (!settingsRootRegistered)
            {
                _contentRootService.Register<SettingsFolder>(
                    SettingsFolder.SettingsRootName + "IMPORT",
                    SettingsFolder.SettingsRootGuid,
                    ContentReference.RootPage);
            }

            var startPageRef = ImportContent(episerverdataPath);
            if (ContentReference.IsNullOrEmpty(startPageRef))
                return;

            await SetupApplication(context, startPageRef);

            ServiceLocator.Current.GetInstance<ISettingsService>().UpdateSettings();
            _principalAccessor.Principal = new GenericPrincipal(new GenericIdentity("Importer"), null);

            var catalogZip = Path.Combine(appDataPath, "foundation_fashion.zip");
            if (!File.Exists(catalogZip))
                catalogZip = Path.Combine(appDataPath, "Foundation_Fashion.zip");

            if (File.Exists(catalogZip))
                ImportCatalog(catalogZip);

            try
            {
                var searchManager = new SearchManager(
                    Mediachase.Commerce.Core.AppContext.Current.ApplicationName,
                    _searchOptions, _serviceProvider, _indexBuilder);
                searchManager.BuildIndex(true);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error("Search index build failed", ex);
            }

            ProvisionContentAcls(context);
        }

        private ContentReference ImportContent(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _dataImporter.Import(stream, ContentReference.RootPage, new ImportOptions
                {
                    KeepIdentity = true,
                    EnsureContentNameUniqueness = false,
                });

                var status = _dataImporter.Status;
                if (status == null)
                    return ContentReference.EmptyReference;

                UpdateLanguageBranches(status);
                return status.ImportedRoot;
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error("Content import failed", ex);
                return ContentReference.EmptyReference;
            }
        }

        private async Task SetupApplication(HttpContext context, ContentReference startPageRef)
        {
            try
            {
                var existing = _applicationRepository.List().OfType<IRoutableApplication>().FirstOrDefault();
                if (existing != null)
                    return;

                var requestHost = context.Request.Host.ToString();
                var website = new InProcessWebsite("foundation", startPageRef);
                website.Hosts.Add(new ApplicationHost(requestHost) { Type = ApplicationHostType.Primary });
                website.Hosts.Add(new ApplicationHost("*") { Type = ApplicationHostType.Default });
                await _applicationRepository.SaveAsync(website);

                try { await _applicationRepository.MakeDefaultAsync(website, true); }
                catch { }

                FixApplicationAssetsRoot(context);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error("Application setup failed", ex);
            }
        }

        private void FixApplicationAssetsRoot(HttpContext context)
        {
            try
            {
                var config = context.RequestServices.GetRequiredService<IConfiguration>();
                var connStr = config.GetConnectionString("EPiServerDB");
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE tblApplication
                    SET fkAssetsRootID = (SELECT TOP 1 pkID FROM tblContent WHERE ContentGUID = 'E56F85D0-E833-4E02-976A-2D11FE4D598C'),
                        IsDefault = 1
                    WHERE IsDefault = 0 OR fkAssetsRootID IS NULL";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private void ProvisionContentAcls(HttpContext context)
        {
            try
            {
                var config = context.RequestServices.GetRequiredService<IConfiguration>();
                var connStr = config.GetConnectionString("EPiServerDB");
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
                conn.Open();

                using var check = conn.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM tblContentAccess WHERE fkContentID = 1 AND [Name] = 'Administrators' AND IsRole = 1";
                if ((int)check.ExecuteScalar()! > 0)
                    return;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO tblContentAccess (fkContentID, [Name], IsRole, AccessMask) VALUES
                    (1, 'Administrators', 1, 63),
                    (1, 'WebAdmins', 1, 63),
                    (1, 'WebEditors', 1, 31),
                    (1, 'CmsAdmins', 1, 63),
                    (1, 'CmsEditors', 1, 31)";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private async Task ProvisionAdminUser(HttpContext context)
        {
            try
            {
                var userProvider = context.RequestServices.GetRequiredService<UIUserProvider>();
                var roleProvider = context.RequestServices.GetRequiredService<UIRoleProvider>();

                string[] roles = ["Administrators", "WebAdmins", "WebEditors", "CmsAdmins", "CmsEditors"];
                foreach (var role in roles)
                {
                    if (!await roleProvider.RoleExistsAsync(role))
                        await roleProvider.CreateRoleAsync(role);
                }

                const string adminEmail = "admin@example.com";
                var existing = await userProvider.GetUserAsync(adminEmail);
                if (existing == null)
                {
                    var result = await userProvider.CreateUserAsync(adminEmail, "Episerver123!", adminEmail, null, null, true);
                    if (result.Status != UIUserCreateStatus.Success)
                    {
                        LogManager.GetLogger().Error($"Failed to create admin user: {result.Status}");
                        return;
                    }
                }

                await roleProvider.AddUserToRolesAsync(adminEmail, roles);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error("Admin user provisioning failed", ex);
            }
        }

        public bool ImportEpiserverContent(Stream stream, ContentReference destinationRoot)
        {
            try
            {
                _dataImporter.Import(stream, destinationRoot, new ImportOptions
                {
                    KeepIdentity = true,
                    EnsureContentNameUniqueness = false,
                });

                var status = _dataImporter.Status;
                if (status == null)
                    return false;

                UpdateLanguageBranches(status);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateLanguageBranches(IImportStatus status)
        {
            if (status.ContentLanguages == null)
                return;

            foreach (var languageId in status.ContentLanguages)
            {
                try
                {
                    var branch = _languageBranchRepository.Load(languageId);
                    if (branch == null)
                    {
                        _languageBranchRepository.Save(new LanguageBranch(languageId));
                    }
                    else if (!branch.Enabled)
                    {
                        branch = branch.CreateWritableClone();
                        branch.Enabled = true;
                        _languageBranchRepository.Save(branch);
                    }
                }
                catch { }
            }
        }

        private void ImportCatalog(string zipPath)
        {
            var catalogDir = Path.Combine(
                _webHostEnvironment.ContentRootPath, "App_Data", "Catalog",
                Path.GetFileNameWithoutExtension(zipPath));

            if (Directory.Exists(catalogDir))
                Directory.Delete(catalogDir, true);
            Directory.CreateDirectory(catalogDir);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory())
                        continue;
                    entry.ExtractToFile(Path.Combine(catalogDir, entry.Name), true);
                }
            }

            var assetsFile = Directory.GetFiles(catalogDir, "ProductAssets*").FirstOrDefault();
            var catalogXml = Directory.GetFiles(catalogDir, "*.xml").FirstOrDefault();
            if (assetsFile == null || catalogXml == null)
                return;

            var catalogFolder = _contentRepository.GetChildren<ContentFolder>(ContentReference.GlobalBlockFolder)
                .FirstOrDefault(f => f.Name.Equals("Catalogs"));
            if (catalogFolder == null)
            {
                catalogFolder = _contentRepository.GetDefault<ContentFolder>(ContentReference.GlobalBlockFolder);
                catalogFolder.Name = "Catalogs";
                _contentRepository.Save(catalogFolder, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
            }

            ImportEpiserverContent(File.OpenRead(assetsFile), catalogFolder.ContentLink);

            try
            {
                var catalogImport = new CatalogImportExport { IsModelsAvailable = true };
                catalogImport.Import(File.OpenRead(catalogXml), true);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error("Catalog import failed", ex);
            }
        }
    }

    public static class ZipExtensions
    {
        public static bool IsDirectory(this ZipArchiveEntry entry)
            => entry.FullName.Length > 0
            && (entry.FullName[entry.FullName.Length - 1] == '/' || entry.FullName[entry.FullName.Length - 1] == '\\');

        public static bool IsFile(this ZipArchiveEntry entry) => !entry.IsDirectory();
    }
}
