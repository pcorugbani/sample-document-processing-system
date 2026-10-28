namespace DocumentProcessor.WebForms.Configuration
{
    /// <summary>
    /// Strongly-typed options for file storage settings.
    /// Bound from the "Storage" section of appsettings.json.
    /// </summary>
    public class StorageOptions
    {
        /// <summary>
        /// Relative path to the upload root directory, resolved against
        /// <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.ContentRootPath"/>.
        /// </summary>
        public string RootPath { get; set; } = "App_Data/uploads";

        public int MaxFileSizeMegabytes { get; set; } = 50;

        public long MaxFileSizeBytes => MaxFileSizeMegabytes * 1024L * 1024L;

        public int MaxFilesPerUpload { get; set; } = 10;

        public string AllowedExtensions { get; set; } = ".pdf,.txt,.log";

        public string[] AllowedExtensionsList =>
            AllowedExtensions.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
    }
}
