using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SecuritySystem.Application.Dtos;
using SecuritySystem.Application.Interfaces;
using SecuritySystem.Domain.ValueObjects;

namespace SecuritySystem.Infrastructure.Analysis;

public class RoslynSecurityAnalyzer : ISecurityAnalyzer
{
	public Task<IReadOnlyList<RawFinding>> AnalyzeSourceCodeAsync(string sourceCode, CancellationToken cancellationToken)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cancellationToken: cancellationToken);
		var root = syntaxTree.GetRoot(cancellationToken);
		var findings = new List<RawFinding>();

		var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

		foreach (var method in methods)
		{
			var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

			foreach (var invocation in invocations)
			{
				var expressionString = invocation.Expression.ToString();

				if (expressionString.Contains("SqlCommand") || expressionString.Contains("ExecuteQuery"))
				{
					var methodCode = method.ToFullString();
					var sinkCode = invocation.ToFullString();
					var graph = BuildBasicTaintGraph(method, invocation);

					findings.Add(new RawFinding(method.Identifier.Text, sinkCode, methodCode, graph));
				}
			}
		}

		return Task.FromResult<IReadOnlyList<RawFinding>>(findings);
	}

	private TaintGraph BuildBasicTaintGraph(MethodDeclarationSyntax method, InvocationExpressionSyntax sink)
	{
		var nodes = new List<TaintNode>
			{
				new(Guid.NewGuid().ToString(), "Input Parameters", "Source"),
				new(Guid.NewGuid().ToString(), sink.ToFullString().Trim(), "Sink")
			};
		var edges = new List<(string, string)> { (nodes[0].NodeId, nodes[1].NodeId) };

		return new TaintGraph(nodes, edges);
	}
}

