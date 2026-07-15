using Xunit;

namespace SharpOpenGl.Tests.Config;

/// <summary>
/// Serializes tests that mutate static <see cref="SharpOpenGl.Engine.Config.MovementBalance"/>
/// multipliers so parallel xUnit workers cannot race orbit/movement assertions.
/// </summary>
[CollectionDefinition("MovementBalance", DisableParallelization = true)]
public sealed class MovementBalanceCollection;