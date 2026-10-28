using System;
using System.Linq;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace DocumentProcessor.WebForms.Data
{
    /// <summary>
    /// Works out the SQL Server connection string at application start, either from
    /// appsettings.json or from AWS Secrets Manager.
    /// </summary>
    public static class DatabaseConnectionResolver
    {
        /// <summary>
        /// Resolves the database connection using <see cref="IConfiguration"/> (appsettings.json).
        /// Called from Program.cs at startup.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="services">
        /// The service collection — currently unused but available for future registration needs.
        /// </param>
        public static DatabaseConnection Resolve(IConfiguration configuration, IServiceCollection services)
        {
            var configured = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "The 'DefaultConnection' connection string is missing from appsettings.json.");

            var fromConfiguration = new DatabaseConnection(
                configured,
                new DatabaseInfo(DetectProvider(configured), "appsettings.json", HostOf(configured)));

            var useSecretsManager = configuration.GetValue<bool>("Database:UseSecretsManager");
            if (!useSecretsManager)
            {
                return fromConfiguration;
            }

            var descriptionPrefix = configuration.GetValue<string>("Database:SecretDescriptionPrefix")
                ?? "Password for RDS MSSQL used for MAM417.";

            try
            {
                return FromSecretsManager(descriptionPrefix);
            }
            catch (Exception ex)
            {
                return new DatabaseConnection(
                    fromConfiguration.ConnectionString,
                    fromConfiguration.Info,
                    "Secrets Manager lookup failed (" + ex.Message + "); using the configured connection string.");
            }
        }

        private static DatabaseConnection FromSecretsManager(string descriptionPrefix)
        {
            using var client = new AmazonSecretsManagerClient();

            var secrets = client.ListSecretsAsync(new ListSecretsRequest()).GetAwaiter().GetResult();
            var match = secrets.SecretList.FirstOrDefault(
                s => s.Description != null
                  && s.Description.StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                throw new InvalidOperationException(
                    "No secret whose description starts with '" + descriptionPrefix + "'.");
            }

            var value = client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = match.ARN })
                .GetAwaiter().GetResult();
            var secret = JObject.Parse(value.SecretString);
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Field(secret, "host") + "," + Field(secret, "port"),
                InitialCatalog = Field(secret, "dbname"),
                UserID = Field(secret, "username"),
                Password = Field(secret, "password"),
                TrustServerCertificate = true,
                Encrypt = true
            };

            return new DatabaseConnection(
                builder.ConnectionString,
                new DatabaseInfo(DatabaseProvider.SqlServer, "AWS Secrets Manager", builder.DataSource));
        }

        /// <summary>
        /// Npgsql connection strings use Host=/Username=; SQL Server uses Server=/User Id=.
        /// Lets the UI report which engine is actually in use after a PostgreSQL migration.
        /// </summary>
        public static DatabaseProvider DetectProvider(string connectionString)
        {
            var looksPostgres = connectionString.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0
                && connectionString.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) < 0;
            return looksPostgres ? DatabaseProvider.PostgreSql : DatabaseProvider.SqlServer;
        }

        private static string HostOf(string connectionString)
        {
            if (DetectProvider(connectionString) == DatabaseProvider.SqlServer)
            {
                return new SqlConnectionStringBuilder(connectionString).DataSource;
            }

            var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var host = ValueOf(parts, "Host");
            var port = ValueOf(parts, "Port");
            if (host == null)
            {
                return "unknown";
            }

            return port == null ? host : host + ":" + port;
        }

        private static string? ValueOf(string[] parts, string key)
        {
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(key.Length + 1);
                }
            }

            return null;
        }

        private static string Field(JObject secret, string name)
        {
            var token = secret[name];
            if (token == null)
            {
                throw new InvalidOperationException("Secret is missing the '" + name + "' field.");
            }

            return token.ToString();
        }
    }
}
