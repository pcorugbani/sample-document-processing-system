namespace DocumentProcessor.WebForms.Configuration
{
    /// <summary>
    /// Strongly-typed options for the Amazon Bedrock summarization settings.
    /// Bound from the "Bedrock" section of appsettings.json.
    /// </summary>
    public class BedrockOptions
    {
        public string Region { get; set; } = "us-east-1";
        public string SummarizationModelId { get; set; } = "global.anthropic.claude-sonnet-5";
        public int MaxTokens { get; set; } = 2000;
        public int MaxInputCharacters { get; set; } = 10000;
        public int MaxPdfPages { get; set; } = 5;
    }
}
