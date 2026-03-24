namespace ClawMailCalCli.Tests;

/// <summary>
/// xUnit collection that disables parallel test execution for tests that mutate shared process state
/// such as <see cref="Console.Out"/> and <see cref="Console.Error"/>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelCollection
{
	/// <summary>The name of this collection.</summary>
	public const string Name = "NonParallel";
}
