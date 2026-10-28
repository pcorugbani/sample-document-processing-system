namespace DocumentProcessor.WebForms.Configuration
{
    /// <summary>
    /// Strongly-typed options for database connection resolution settings.
    /// Bound from the "Database" section of appsettings.json.
    /// </summary>
    public class DatabaseOptions
    {
        public bool UseSecretsManager { get; set; }
        public string SecretDescriptionPrefix { get; set; } = "Password for RDS MSSQL used for MAM417.";
    }
}
