using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SecuritySystem.Application.Interfaces;

namespace SecuritySystem.Infrastructure.Analysis;

public class RoslynPatchVerifier : IPatchVerifier
{
	private static readonly List<MetadataReference> _references;

	static RoslynPatchVerifier()
	{
		var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
		_references = trustedAssembliesPaths
			.Where(p =>
				p.Contains("System.Private.CoreLib") ||
				p.Contains("System.Runtime") ||
				p.Contains("System.Console") ||
				p.Contains("System.Data.SqlClient") ||
				p.Contains("System.Data.Common"))
			.Select(p => MetadataReference.CreateFromFile(p))
			.ToList<MetadataReference>();
	}

	public bool VerifyCompilation(string sourceCode)
	{
		var fullCode = $"public class TempClass {{ public void TempMethod() {{ {sourceCode} }} }}";
		var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

		var compilation = CSharpCompilation.Create(
			"VerificationAssembly",
			syntaxTrees: new[] { syntaxTree },
			references: _references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		using var ms = new MemoryStream();
		var result = compilation.Emit(ms);

		return result.Success && !result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
	}
}

