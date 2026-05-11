namespace SecuritySystem.Domain.ValueObjects;

public record TaintGraph(IReadOnlyList<TaintNode> Nodes, IReadOnlyList<(string FromId, string ToId)> Edges);
