using SecuritySystem.Application.Interfaces;
using SecuritySystem.Domain.Entities;
using SecuritySystem.Domain.ValueObjects;

namespace SecuritySystem.Application.UseCases.AnalyzeCode;

public class AnalyzeCodeCommandHandler(
	ILlmAnalysisService llmService,
	IPatchVerifier patchVerifier,
	IVulnerabilityRepository repository)
{
	public async Task<IReadOnlyList<Guid>> HandleAsync(AnalyzeCodeCommand command, CancellationToken ct)
	{
		var aiResults = await llmService.AnalyzeAndPatchAsync(command.Language, command.SourceCode, "snippet", ct);
		var resultIds = new List<Guid>();

		foreach (var result in aiResults)
		{
			var originalSnippet = new CodeSnippet(result.OriginalTaintedCode, true, "snippet");

			var vulnerability = new Vulnerability(
				Guid.NewGuid(),
				result.CweId,
				result.OwaspCategory,
				new CvssScore(result.CvssScore),
				originalSnippet,
				result.FlowGraph
			);

			var isPatchValid = patchVerifier.VerifyCompilation(result.PatchedCode);
			if (isPatchValid)
			{
				vulnerability.ApplyVerifiedPatch(new CodeSnippet(result.PatchedCode, true, "snippet"));
			}

			await repository.SaveAsync(vulnerability, ct);
			resultIds.Add(vulnerability.Id);
		}

		return resultIds;
	}
}
