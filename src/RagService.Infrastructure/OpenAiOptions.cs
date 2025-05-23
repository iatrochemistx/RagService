namespace RagService.Infrastructure
{
    /// <summary>
    /// Settings for real OpenAI calls, bound from "OpenAI" in IConfiguration.
    /// </summary>
    public class OpenAiOptions
    {
        public string ApiKey         { get; set; } = "";
        public string BaseUrl        { get; set; } = "https://api.openai.com/";
        public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
        public string ChatModel      { get; set; } = "gpt-3.5-turbo";
    }
}
