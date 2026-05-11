namespace SecuritySystem.Application.Dtos;

public record AnalyzeRequestDto
{
	public string Language { get; init; } = "python";
	public string SourceCode { get; init; } = @"
import sqlite3
def get_user(username):
    conn = sqlite3.connect('users.db')
    cursor = conn.cursor()
    cursor.execute(f""SELECT * FROM users WHERE username = '{username}'"")
    return cursor.fetchall()
";
}
