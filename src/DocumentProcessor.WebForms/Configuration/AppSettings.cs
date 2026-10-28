using System;
using Microsoft.Extensions.Configuration;

namespace DocumentProcessor.WebForms.Configuration
{
    // PORT-TODO
    // {"was": "System.Configuration.ConfigurationManager", "note": "This entire static class reads settings via ConfigurationManager.AppSettings and ConfigurationManager.ConnectionStrings. Rewrite as strongly-typed options classes (IOptions<BedrockOptions>, IOptions<StorageOptions>, IOptions<DatabaseOptions>) bound from appsettings.json via IConfiguration. The package keeps this compiling, but ConfigurationManager.AppSettings reads the XML app.config, not appsettings.json — if config data moves to JSON these reads return null at runtime.", "files": ["Program.cs"]}
    /// <summary>
    /// Reads settings from IConfiguration (appsettings.json).
    /// Call <see cref="Initialize"/> from Program.cs before any property is accessed.
    /// </summary>
    public static class AppSettings
    {
        private static IConfiguration _configuration;

        /// <summary>
        /// Initializes the static settings accessor with the application's IConfiguration.
        /// Must be called once at startup (Program.cs) before any property is used.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static string ConnectionString
        {
            get
            {
                EnsureInitialized();
                var cs = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(cs))
                {
                    throw new InvalidOperationException("The 'DefaultConnection' connection string is missing from appsettings.json.");
                }
                return cs;
            }
        }

        public static bool DatabaseUseSecretsManager => GetBoolean("Database:UseSecretsManager", false);

        public static string DatabaseSecretDescriptionPrefix => GetString("Database:SecretDescriptionPrefix", "Password for RDS MSSQL used for MAM417.");

        public static string BedrockRegion => GetString("Bedrock:Region", "us-east-1");

        public static string BedrockSummarizationModelId => GetString("Bedrock:SummarizationModelId", "global.anthropic.claude-sonnet-5");

        public static int BedrockMaxTokens => GetInt32("Bedrock:MaxTokens", 2000);

        /// <summary>Characters of extracted text sent to the model.</summary>
        public static int BedrockMaxInputCharacters => GetInt32("Bedrock:MaxInputCharacters", 10000);

        public static int BedrockMaxPdfPages => GetInt32("Bedrock:MaxPdfPages", 5);

        public static string StorageRootPath => GetString("Storage:RootPath", "App_Data/uploads");

        public static int StorageMaxFileSizeMegabytes => GetInt32("Storage:MaxFileSizeMegabytes", 50);

        public static long StorageMaxFileSizeBytes => StorageMaxFileSizeMegabytes * 1024L * 1024L;

        public static int StorageMaxFilesPerUpload => GetInt32("Storage:MaxFilesPerUpload", 10);

        public static string[] StorageAllowedExtensions
        {
            get
            {
                var raw = GetString("Storage:AllowedExtensions", ".pdf,.txt,.log");
                return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static void EnsureInitialized()
        {
            if (_configuration == null)
                throw new InvalidOperationException("AppSettings has not been initialized. Call AppSettings.Initialize(configuration) in Program.cs.");
        }

        private static string GetString(string key, string fallback)
        {
            EnsureInitialized();
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : value.Trim();
        }

        private static int GetInt32(string key, int fallback)
        {
            EnsureInitialized();
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : int.Parse(value.Trim());
        }

        private static bool GetBoolean(string key, bool fallback)
        {
            EnsureInitialized();
            var value = _configuration[key];
            return string.IsNullOrEmpty(value) ? fallback : bool.Parse(value.Trim());
        }
    }
}