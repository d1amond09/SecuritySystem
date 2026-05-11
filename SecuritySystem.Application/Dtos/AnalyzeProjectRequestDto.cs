using System;
using System.Collections.Generic;
using System.Text;

namespace SecuritySystem.Application.Dtos;

public record AnalyzeProjectRequestDto(
	string RepositoryUrl,
	string Branch = "main",
	string Language = "csharp"
);
