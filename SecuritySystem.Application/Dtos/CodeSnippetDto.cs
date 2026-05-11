using System;
using System.Collections.Generic;
using System.Text;

namespace SecuritySystem.Application.Dtos;

public record CodeSnippetDto(string Content, bool IsCompilable);
