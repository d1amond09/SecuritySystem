using SecuritySystem.Application.Dtos;
using SecuritySystem.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace SecuritySystem.Infrastructure.Providers;

public class GitHubSourceCodeProvider(HttpClient httpClient) : ISourceCodeProvider
{
	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".cs", ".py", ".js", ".ts", ".java", ".cpp", ".c", ".go", ".rb", ".php"
	};

	public async Task<IReadOnlyList<ProjectFile>> GetSourceFilesAsync(string repositoryUrl, string branch, CancellationToken ct)
	{
		var zipUrl = BuildZipUrl(repositoryUrl, branch);

		// GitHub требует User-Agent
		httpClient.DefaultRequestHeaders.Add("User-Agent", "SecurityAnalyzer-App");

		using var response = await httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Failed to download repository from {zipUrl}. Status: {response.StatusCode}");
		}

		using var stream = await response.Content.ReadAsStreamAsync(ct);
		using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

		var projectFiles = new List<ProjectFile>();

		foreach (var entry in archive.Entries)
		{
			if (string.IsNullOrEmpty(entry.Name)) continue;

			var extension = Path.GetExtension(entry.FullName);
			if (!AllowedExtensions.Contains(extension)) continue;

			if (entry.FullName.Contains("/test/", StringComparison.OrdinalIgnoreCase) ||
				entry.FullName.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase) ||
				entry.FullName.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
				entry.FullName.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			using var entryStream = entry.Open();
			using var reader = new StreamReader(entryStream);
			var content = await reader.ReadToEndAsync(ct);

			var cleanPath = Regex.Replace(entry.FullName, @"^[^/]+/", "");

			projectFiles.Add(new ProjectFile(cleanPath, content));
		}

		return projectFiles;
	}

	private static string BuildZipUrl(string repositoryUrl, string branch)
	{
		var cleanUrl = repositoryUrl.TrimEnd('/');
		return $"{cleanUrl}/archive/refs/heads/{branch}.zip";
	}
}
