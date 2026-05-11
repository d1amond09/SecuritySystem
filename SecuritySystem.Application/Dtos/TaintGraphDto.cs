namespace SecuritySystem.Application.Dtos;

public record TaintGraphDto(IReadOnlyList<TaintNodeDto> Nodes, IReadOnlyList<(string FromId, string ToId)> Edges);
