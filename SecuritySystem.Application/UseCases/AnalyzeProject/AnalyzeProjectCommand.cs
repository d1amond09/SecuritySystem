using System;
using System.Collections.Generic;
using System.Text;

namespace SecuritySystem.Application.UseCases.AnalyzeProject;

public record AnalyzeProjectCommand(string RepositoryUrl, string Branch, string Language);

