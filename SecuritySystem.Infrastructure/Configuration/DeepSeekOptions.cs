using System;
using System.Collections.Generic;
using System.Text;

namespace SecuritySystem.Infrastructure.Configuration;

public class DeepSeekOptions
{
	public const string SectionName = "LlmSettings:DeepSeek";
	public string ApiKey { get; init; } = string.Empty;
	public string Model { get; init; } = "deepseek-coder"; 
	public string BaseUrl { get; init; } = "https://api.deepseek.com/v1/";
}
