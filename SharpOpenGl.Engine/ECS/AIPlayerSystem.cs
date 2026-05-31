using OpenTK.Mathematics;

namespace SharpOpenGl.Engine.ECS;

/// <summary>
/// Simple AI system that gives computer-controlled entities patrol and attack behavior.
/// Entities with <see cref="AIControlledComponent"/> will be given move orders periodically.
/// </summary>
public sealed class AIPlayerSystem : GameSystem
{
    private readonly Random _rng = new();
    private float _decisionTimer;
    private const float DecisionInterval = 3f;
    private readonly float _mapSize;

    public AIPlayerSystem(float mapSize = 1000f)
    {
        _mapSize = mapSize;
    }

    public override void Update(World world, float deltaTime)
    {
        _decisionTimer -= deltaTime;
        if (_decisionTimer > 0f) return;
        _decisionTimer = DecisionInterval;

        foreach (var (entity, ai) in world.Query<AIControlledComponent>())
        {
            var movement = world.GetComponent<MovementComponent>(entity);
            if (movement == null) continue;

            var transform = world.GetComponent<TransformComponent>(entity);
            if (transform == null) continue;

            // If already moving toward a target, skip
            if (movement.PathTarget != null)
            {
                float dist = (movement.PathTarget.Value - transform.Position).Length;
                if (dist > 5f) continue;
            }

            // Pick a new patrol point within map bounds
            float halfMap = _mapSize * 0.4f;
            float x = (_rng.NextSingle() - 0.5f) * 2f * halfMap;
            float z = (_rng.NextSingle() - 0.5f) * 2f * halfMap;
            movement.PathTarget = new Vector3(x, 0f, z);
        }
    }
}
