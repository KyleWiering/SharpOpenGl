using Xunit;

namespace SharpOpenGl.Tests.Rendering;

/// <summary>Serializes tests that read/write GameData/Meshes to avoid parallel OBJ corruption.</summary>
[CollectionDefinition("ModelMigrationExport", DisableParallelization = true)]
public sealed class ModelMigrationExportCollection;