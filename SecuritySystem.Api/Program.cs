using Microsoft.AspNetCore.Mvc;
using SecuritySystem.Api.Mappers;
using SecuritySystem.Application.Dtos;
using SecuritySystem.Application.Interfaces;
using SecuritySystem.Application.UseCases.AnalyzeCode;
using SecuritySystem.Application.UseCases.AnalyzeFile;
using SecuritySystem.Application.UseCases.AnalyzeProject;
using SecuritySystem.Infrastructure.Analysis;
using SecuritySystem.Infrastructure.Configuration;
using SecuritySystem.Infrastructure.Neural;
using SecuritySystem.Infrastructure.Persistence;
using SecuritySystem.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => {
	options.AddDefaultPolicy(policy => {
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

// Core Services
builder.Services.AddSingleton<IVulnerabilityRepository, InMemoryVulnerabilityRepository>();
builder.Services.AddTransient<IPatchVerifier, RoslynPatchVerifier>();

// Use Cases
builder.Services.AddTransient<AnalyzeCodeCommandHandler>();
builder.Services.AddTransient<AnalyzeProjectCommandHandler>();
builder.Services.AddTransient<AnalyzeFileCommandHandler>();

// Providers
builder.Services.AddHttpClient<ISourceCodeProvider, GitHubSourceCodeProvider>(); 

// LLM
var activeProvider = builder.Configuration["LlmSettings:ActiveProvider"];
if (activeProvider?.Equals("Gemini", StringComparison.OrdinalIgnoreCase) == true)
{
	builder.Services.AddTransient<ILlmAnalysisService, GeminiSecurityService>();
}
else
{
	throw new InvalidOperationException("Unknown LLM Provider");
}

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

var api = app.MapGroup("/api/v1").WithOpenApi();

api.MapPost("/analyze/snippet", async (
	[FromBody] AnalyzeRequestDto request,
	[FromServices] AnalyzeCodeCommandHandler handler,
	CancellationToken ct) =>
{
	var command = new AnalyzeCodeCommand(request.Language, request.SourceCode);
	var ids = await handler.HandleAsync(command, ct);
	return Results.Ok(new { AnalyzedVulnerabilityIds = ids });
}).WithName("AnalyzeSourceCodeSnippet");

api.MapPost("/analyze/project", async (
	[FromBody] AnalyzeProjectRequestDto request,
	[FromServices] AnalyzeProjectCommandHandler handler,
	CancellationToken ct) =>
{
	var command = new AnalyzeProjectCommand(request.RepositoryUrl, request.Branch, request.Language);
	var ids = await handler.HandleAsync(command, ct);
	return Results.Ok(new { AnalyzedVulnerabilityIds = ids, TotalFound = ids.Count });
}).WithName("AnalyzeGitHubProject");

api.MapPost("/analyze/file", async (
	IFormFile file,
	[FromServices] AnalyzeFileCommandHandler handler,
	CancellationToken ct) =>
{
	if (file == null || file.Length == 0)
	{
		return Results.BadRequest("File is empty.");
	}

	using var reader = new StreamReader(file.OpenReadStream());
	var content = await reader.ReadToEndAsync(ct);

	var command = new AnalyzeFileCommand(file.FileName, content);
	var ids = await handler.HandleAsync(command, ct);
	return Results.Ok(new { AnalyzedVulnerabilityIds = ids });
}).WithName("AnalyzeFile").DisableAntiforgery();

api.MapGet("/vulnerabilities", async (
	[FromServices] IVulnerabilityRepository repo,
	CancellationToken ct) =>
{
	var data = await repo.GetAllAsync(ct);
	var response = data.Select(v => v.ToDto());
	return Results.Ok(response);
}).WithName("GetVulnerabilities");

app.Run();