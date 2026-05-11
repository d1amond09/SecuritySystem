using SecuritySystem.Application.Interfaces;
using SecuritySystem.Domain.Entities;
using SecuritySystem.Domain.ValueObjects;

namespace SecuritySystem.Application.UseCases.AnalyzeProject;

public class AnalyzeProjectCommandHandler(
	ISourceCodeProvider sourceProvider,
	ILlmAnalysisService llmService,
	IPatchVerifier patchVerifier,
	IVulnerabilityRepository repository)
{
	public async Task<IReadOnlyList<Guid>> HandleAsync(AnalyzeProjectCommand command, CancellationToken ct)
	{
		var files = await sourceProvider.GetSourceFilesAsync(command.RepositoryUrl, command.Branch, ct);
		var resultIds = new List<Guid>();

		foreach (var file in files)
		{
			var aiResults = await llmService.AnalyzeAndPatchAsync(command.Language, file.Content, file.FilePath, ct);

			foreach (var result in aiResults)
			{
				var originalSnippet = new CodeSnippet(result.OriginalTaintedCode, true, file.FilePath);

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
					vulnerability.ApplyVerifiedPatch(new CodeSnippet(result.PatchedCode, true, file.FilePath));
				}

				await repository.SaveAsync(vulnerability, ct);
				resultIds.Add(vulnerability.Id);
			}
		}

		return resultIds;
	}
}
