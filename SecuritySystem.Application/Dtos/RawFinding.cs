using SecuritySystem.Domain.ValueObjects;

namespace SecuritySystem.Application.Dtos;

public record RawFinding(string MethodName, string SinkCode, string FullMethodCode, TaintGraph Graph);

