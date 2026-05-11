namespace SecuritySystem.Domain.ValueObjects;

public readonly record struct CvssScore
{
	public double Value { get; }
	public CvssScore(double value)
	{
		if (value is < 0 or > 10)
			throw new ArgumentOutOfRangeException(nameof(value), "CVSS score must be between 0 and 10.");
		Value = value;
	}
}
