using SecuritySystem.Application.Dtos;

namespace SecuritySystem.Application.Interfaces;

public interface ISecurityAnalyzer
{
	Task<IReadOnlyList<RawFinding>> AnalyzeSourceCodeAsync(string sourceCode, CancellationToken cancellationToken);
}
