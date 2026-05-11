using Microsoft.Extensions.Options;
using SecuritySystem.Application.Dtos;
using SecuritySystem.Application.Interfaces;
using SecuritySystem.Domain.ValueObjects;
using SecuritySystem.Infrastructure.Configuration;
using SecuritySystem.Infrastructure.Neural.Dtos;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SecuritySystem.Infrastructure.Neural;

public class DeepSeekSecurityService(HttpClient httpClient, IOptions<DeepSeekOptions> options) : ILlmAnalysisService
{
	private readonly DeepSeekOptions _options = options.Value;

	public async Task<IReadOnlyList<AiVulnerabilityResult>> AnalyzeAndPatchAsync(string language, string sourceCode, string filePath, CancellationToken ct)
	{
		var prompt = BuildPrompt(language, sourceCode, filePath);

		var requestBody = new DeepSeekRequest(
			Model: _options.Model,
			Messages: [
				new Message("system", "You are an expert security analyzer. Respond ONLY with valid JSON."),
				new Message("user", prompt)
			],
			Temperature: 0.1 // Низкая температура для максимальной детерминированности
		);

		// Отправка запроса к DeepSeek API
		var response = await httpClient.PostAsJsonAsync("chat/completions", requestBody, ct);
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(cancellationToken: ct);
		var rawContent = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "[]";

		var cleanJson = ExtractJson(rawContent);

		var parsedResults = JsonSerializer.Deserialize<List<AiVulnerabilityResultDto>>(cleanJson,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		return parsedResults?.Select(MapToResult).ToList() ?? [];
	}

	private static string ExtractJson(string text)
	{
		var match = Regex.Match(text, @"```(?:json)?\s*(.*?)\s*```", RegexOptions.Singleline);
		return match.Success ? match.Groups[1].Value : text.Trim();
	}

	private static AiVulnerabilityResult MapToResult(AiVulnerabilityResultDto dto)
	{
		var nodes = dto.FlowGraph.Nodes.Select(n => new TaintNode(n.NodeId, n.CodeContext, n.NodeType)).ToList();
		var edges = dto.FlowGraph.Edges.Select(e => (e.FromId, e.ToId)).ToList();
		var graph = new TaintGraph(nodes, edges);

		return new AiVulnerabilityResult(dto.CweId, dto.OwaspCategory, dto.CvssScore, dto.Description, dto.OriginalTaintedCode, dto.PatchedCode, graph);
	}

	private static string BuildPrompt(string language, string sourceCode, string filePath) => $@"
        Analyze the following {language} code for vulnerabilities.
        File path: {filePath}
        Perform deep Taint-analysis from source to sink. 
        Return ONLY a valid JSON array. Schema:
        [{{
            ""cweId"": ""string"", ""owaspCategory"": ""string"", ""cvssScore"": 0.0, ""description"": ""string"",
            ""originalTaintedCode"": ""string"", ""patchedCode"": ""string"",
            ""flowGraph"": {{
                ""nodes"": [{{ ""nodeId"": ""string"", ""codeContext"": ""string"", ""nodeType"": ""Source|Step|Sink"" }}],
                ""edges"": [{{ ""fromId"": ""string"", ""toId"": ""string"" }}]
            }}
        }}]
        Code: 
        {sourceCode}";

	// Внутренние DTO для API DeepSeek (OpenAI format)
	private record DeepSeekRequest(string Model, List<Message> Messages, double Temperature);
	private record Message(string Role, string Content);
	private record DeepSeekResponse(List<Choice> Choices);
	private record Choice(Message Message);
}