using SecuritySystem.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecuritySystem.Application.Interfaces;

public interface ISourceCodeProvider
{
	Task<IReadOnlyList<ProjectFile>> GetSourceFilesAsync(string repositoryUrl, string branch, CancellationToken ct);
}
