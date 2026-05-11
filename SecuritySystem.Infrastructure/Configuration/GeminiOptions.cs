namespace SecuritySystem.Infrastructure.Configuration;

public class GeminiOptions
{
	public const string SectionName = "LlmSettings:Gemini";
	public string ApiKey { get; init; } = string.Empty;
	public string Model { get; init; } = string.Empty;
}
