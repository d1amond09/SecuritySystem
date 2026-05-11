namespace SecuritySystem.Domain.ValueObjects;

public record CodeSnippet(string Content, bool IsCompilable, string FilePath = "Unknown");
