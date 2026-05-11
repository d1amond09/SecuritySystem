using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SecuritySystem.Application.Interfaces;

namespace SecuritySystem.Infrastructure.Analysis;

public class RoslynPatchVerifier : IPatchVerifier
{
	public bool VerifyCompilation(string sourceCode)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		var diagnostics = syntaxTree.GetDiagnostics();
		return !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
	}
}

