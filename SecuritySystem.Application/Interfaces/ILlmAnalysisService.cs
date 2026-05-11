using SecuritySystem.Application.Dtos;

namespace SecuritySystem.Application.Interfaces;

public interface ILlmAnalysisService
{
	Task<IReadOnlyList<AiVulnerabilityResult>> AnalyzeAndPatchAsync(string language, string sourceCode, string filePath, CancellationToken ct);
}
