using SecuritySystem.Application.Interfaces;
using SecuritySystem.Domain.Entities;
using SecuritySystem.Domain.ValueObjects;

namespace SecuritySystem.Application.UseCases.AnalyzeFile;

public class AnalyzeFileCommandHandler(
	ILlmAnalysisService llmService,
	IPatchVerifier patchVerifier,
	IVulnerabilityRepository repository)
{
	public async Task<IReadOnlyList<Guid>> HandleAsync(AnalyzeFileCommand command, CancellationToken ct)
	{
		var language = DetectLanguage(command.FileName);

		var aiResults = await llmService.AnalyzeAndPatchAsync(language, command.Content, command.FileName, ct);
		var resultIds = new List<Guid>();

		foreach (var result in aiResults)
		{
			var originalSnippet = new CodeSnippet(result.OriginalTaintedCode, true, command.FileName);

			var vulnerability = new Vulnerability(
				Guid.NewGuid(),
				result.CweId,
				result.OwaspCategory,
				new CvssScore(result.CvssScore),
				originalSnippet,
				result.FlowGraph
			);

			if (patchVerifier.VerifyCompilation(result.PatchedCode))
			{
				vulnerability.ApplyVerifiedPatch(new CodeSnippet(result.PatchedCode, true, command.FileName));
			}

			await repository.SaveAsync(vulnerability, ct);
			resultIds.Add(vulnerability.Id);
		}

		return resultIds;
	}

	private string DetectLanguage(string fileName)
	{
		return Path.GetExtension(fileName).ToLowerInvariant() switch
		{
			".cs" => "csharp",
			".py" => "python",
			".js" => "javascript",
			".ts" => "typescript",
			".java" => "java",
			_ => "unknown" 
		};
	}
}
