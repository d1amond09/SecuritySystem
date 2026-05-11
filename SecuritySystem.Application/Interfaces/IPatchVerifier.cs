namespace SecuritySystem.Application.Interfaces;

public interface IPatchVerifier
{
	bool VerifyCompilation(string sourceCode);
}
