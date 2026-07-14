using OpenTK.Mathematics;
using SharpOpenGl.Engine.Build;
using SharpOpenGl.Engine.Config;
using SharpOpenGl.Engine.Economy;
using SharpOpenGl.Engine.ECS;
using SharpOpenGl.Engine.Entities;
using SharpOpenGl.Engine.Grid;
using Xunit;

namespace SharpOpenGl.Tests.Build;

public class BuildingPlacementValidatorTests
{
    private static BuildMapCatalog CreateCatalog()
    {
        var config = new BuildMapConfig
        {
            Categories =
            [
                new BuildMapCategoryConfig
                {
                    Id = "production",
                    DisplayName = "Production",
                    Buildings =
                    [
                        new BuildMapEntryConfig { Id = "command_center", Prerequisites = [] },
                        new BuildMapEntryConfig { Id = "shipyard_small", Prerequisites = ["command_center"] },
                    ],
                },
            ],
        };

        var defs = new Dictionary<string, EntityDefinition>
        {
            ["command_center"] = new()
            {
                Id = "command_center",
                DisplayName = "Command Center",
                Cost = new CostDefinition(),
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "command_center", Footprint = [2, 2] },
                },
            },
            ["shipyard_small"] = new()
            {
                Id = "shipyard_small",
                DisplayName = "Small Shipyard",
                Cost = new CostDefinition { Energy = 50, Minerals = 50, Crew = 1 },
                Components = new ComponentsDefinition
                {
                    Building = new BuildingDefinition { BuildingType = "shipyard_small", Footprint = [2, 2] },
                },
            },
        };

        return new BuildMapCatalog(config, defs);
    }

    [Fact]
    public void Validate_rejects_locked_building_without_prerequisites()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var def = catalog.BuildViews(world, 1, resources, null)
            .SelectMany(c => c.Buildings)
            .First(b => b.Id == "shipyard_small");

        var shipyardDef = new EntityDefinition
        {
            Id = def.Id,
            Components = new ComponentsDefinition
            {
                Building = new BuildingDefinition { Footprint = [2, 2] },
            },
            Cost = new CostDefinition { Energy = 50, Minerals = 50, Crew = 1 },
        };

        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, shipyardDef, new Vector3(0f, 0f, 0f),
            catalog, resources, supply: null);

        Assert.False(result.IsValid);
        Assert.Equal(PlacementFailureReason.Locked, result.Reason);
    }

    [Fact]
    public void Validate_rejects_occupied_cells()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        var commandCenter = world.CreateEntity();
        world.AddComponent(commandCenter, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
            Footprint = [2, 2],
        });

        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var def = new EntityDefinition
        {
            Id = "shipyard_small",
            Components = new ComponentsDefinition
            {
                Building = new BuildingDefinition { Footprint = [2, 2] },
            },
            Cost = new CostDefinition { Energy = 50, Minerals = 50 },
        };

        BuildingFootprint.Occupy(grid, commandCenter, new Vector3(0f, 0f, 0f), [2, 2]);

        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, def, new Vector3(0f, 0f, 0f),
            catalog, resources, supply: null);

        Assert.False(result.IsValid);
        Assert.Equal(PlacementFailureReason.CellOccupied, result.Reason);
    }

    [Fact]
    public void Validate_rejects_impassable_terrain()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        grid.GetCell(10, 10)!.Terrain = TerrainType.Impassable;

        var world = new World();
        world.AddComponent(world.CreateEntity(), new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var def = new EntityDefinition
        {
            Id = "shipyard_small",
            Components = new ComponentsDefinition
            {
                Building = new BuildingDefinition { Footprint = [1, 1] },
            },
            Cost = new CostDefinition(),
        };

        Vector3 worldPos = grid.GridToWorld(10, 10);
        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, def, worldPos, catalog, resources, supply: null);

        Assert.False(result.IsValid);
        Assert.Equal(PlacementFailureReason.ImpassableTerrain, result.Reason);
    }

    [Fact]
    public void Validate_accepts_valid_open_location()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        world.AddComponent(world.CreateEntity(), new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
        });

        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var def = new EntityDefinition
        {
            Id = "shipyard_small",
            Components = new ComponentsDefinition
            {
                Building = new BuildingDefinition { Footprint = [2, 2] },
            },
            Cost = new CostDefinition { Energy = 50, Minerals = 50 },
        };

        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, def, new Vector3(40f, 0f, 40f),
            catalog, resources, supply: null);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_partial_overlap_on_multi_cell_footprint()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        var commandCenter = world.CreateEntity();
        world.AddComponent(commandCenter, new BuildingComponent
        {
            BuildingType = "command_center",
            PlayerId = 1,
            Footprint = [2, 2],
        });

        BuildingFootprint.Occupy(grid, commandCenter, new Vector3(0f, 0f, 0f), [2, 2]);

        var resources = CreateFundedResources();
        var catalog = CreateCatalog();
        var def = new EntityDefinition
        {
            Id = "repair_bay",
            Components = new ComponentsDefinition
            {
                Building = new BuildingDefinition { Footprint = [3, 3] },
            },
            Cost = new CostDefinition(),
        };

        var result = BuildingPlacementValidator.Validate(
            grid, world, 1, def, new Vector3(10f, 0f, 0f),
            catalog, resources, supply: null);

        Assert.False(result.IsValid);
        Assert.Equal(PlacementFailureReason.CellOccupied, result.Reason);
    }

    [Fact]
    public void Occupy_marks_all_footprint_cells()
    {
        var grid = new GridSystem(20, 20, 10f, new Vector2(-100f, -100f));
        var world = new World();
        var entity = world.CreateEntity();
        BuildingFootprint.Occupy(grid, entity, new Vector3(0f, 0f, 0f), [2, 2]);

        foreach (var (x, y) in BuildingFootprint.EnumerateCells(grid, new Vector3(0f, 0f, 0f), 2, 2))
            Assert.Equal(entity, grid.GetCell(x, y)!.Occupant);
    }

    private static ResourceManager CreateFundedResources()
    {
        var resources = new ResourceManager();
        var player = resources.AddPlayer(1);
        player.SetStartingAmount(ResourceType.Energy, 5000);
        player.SetStartingAmount(ResourceType.Minerals, 5000);
        player.SetStartingAmount(ResourceType.Data, 5000);
        player.SetStartingAmount(ResourceType.Crew, 5000);
        return resources;
    }
}