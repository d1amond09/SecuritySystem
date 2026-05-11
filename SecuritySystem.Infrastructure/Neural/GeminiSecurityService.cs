using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
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

public class GeminiSecurityService(IOptions<GeminiOptions> options) : ILlmAnalysisService
{
	private readonly GeminiOptions _options = options.Value;

	public async Task<IReadOnlyList<AiVulnerabilityResult>> AnalyzeAndPatchAsync(string language, string sourceCode, string filePath, CancellationToken ct)
	{
		var prompt = BuildPrompt(language, sourceCode, filePath);

		var client = new Client(apiKey: _options.ApiKey);
		var response = await client.Models.GenerateContentAsync(
			model: _options.Model,
			contents: prompt,
			config: new() { ResponseMimeType = "application/json", Temperature = 0.1 }
		);

		var rawContent = response?.Candidates?[0].Content?.Parts?[0].Text ?? "[]";
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
		You are an expert security analyzer. Analyze the following {language} code for vulnerabilities.
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
        Code: {sourceCode}";
}