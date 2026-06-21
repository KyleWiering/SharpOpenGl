using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpOpenGl.Engine.Rendering;

namespace SharpOpenGl;

/// <summary>Desktop GPU upload wrappers for race-aware procedural ship meshes.</summary>
public static class ShipMeshExtensions
{
    public static (int vao, int vbo, int vertexCount) BuildRaceMesh(
        string raceId, string hullOrDefinitionId, Vector3 color, float sizeScale = 1f)
        => Upload(RaceShipMeshes.Build(raceId, hullOrDefinitionId, color, sizeScale));

    public static (int vao, int vbo, int vertexCount) BuildFighterMesh(Vector3 color, float size = 2f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "fighter", color, size / 1.75f);

    public static (int vao, int vbo, int vertexCount) BuildScoutMesh(Vector3 color, float size = 1.4f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "scout", color, size / 1.4f);

    public static (int vao, int vbo, int vertexCount) BuildDroneMesh(Vector3 color, float size = 0.9f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "drone", color, size / 0.9f);

    public static (int vao, int vbo, int vertexCount) BuildCorvetteMesh(Vector3 color, float size = 2.2f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "corvette", color, size / 2.2f);

    public static (int vao, int vbo, int vertexCount) BuildFrigateMesh(Vector3 color, float size = 3f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "frigate", color, size / 3f);

    public static (int vao, int vbo, int vertexCount) BuildGunshipMesh(Vector3 color, float size = 3.2f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "gunship", color, size / 3.2f);

    public static (int vao, int vbo, int vertexCount) BuildCruiserMesh(Vector3 color, float size = 4.2f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "cruiser", color, size / 4.2f);

    public static (int vao, int vbo, int vertexCount) BuildTransportMesh(Vector3 color, float size = 3.8f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "transport", color, size / 3.8f);

    public static (int vao, int vbo, int vertexCount) BuildMinerMesh(Vector3 color, float size = 2.2f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "miner", color, size / 2.2f);

    public static (int vao, int vbo, int vertexCount) BuildDreadnoughtMesh(Vector3 color, float size = 5.5f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "dreadnought", color, size / 5.5f);

    public static (int vao, int vbo, int vertexCount) BuildInterceptorMesh(Vector3 color, float size = 1.6f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "interceptor", color, size / 1.6f);

    public static (int vao, int vbo, int vertexCount) BuildFreighterMesh(Vector3 color, float size = 4.5f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "freighter", color, size / 4.5f);

    public static (int vao, int vbo, int vertexCount) BuildSupportMesh(Vector3 color, float size = 3.5f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "support", color, size / 3.5f);

    public static (int vao, int vbo, int vertexCount) BuildHeroMesh(Vector3 color, float size = 3f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "hero", color, size / 3f);

    public static (int vao, int vbo, int vertexCount) BuildBomberMesh(Vector3 color, float size = 2.5f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "bomber", color, size / 2.5f);

    public static (int vao, int vbo, int vertexCount) BuildDestroyerMesh(Vector3 color, float size = 3.5f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "destroyer", color, size / 3.5f);

    public static (int vao, int vbo, int vertexCount) BuildCarrierMesh(Vector3 color, float size = 4f)
        => BuildRaceMesh(RaceShipMeshes.DefaultRace, "carrier", color, size / 4f);

    public static (int vao, int vbo, int vertexCount) BuildShipyardSmall(float size = 6f)
        => BuildShipyardCore(size, 1, 2);

    public static (int vao, int vbo, int vertexCount) BuildShipyardMedium(float size = 9f)
        => BuildShipyardCore(size, 2, 4);

    public static (int vao, int vbo, int vertexCount) BuildShipyardLarge(float size = 12f)
        => BuildShipyardCore(size, 3, 6);

    private static (int vao, int vbo, int vertexCount) BuildShipyardCore(float size, int gantryCount, int stripeCount)
    {
        float s = size;
        var verts = new List<float>();
        Vector3 padDark = new(0.28f, 0.3f, 0.34f);
        Vector3 padLight = new(0.45f, 0.48f, 0.52f);
        Vector3 gantry = new(0.85f, 0.45f, 0.15f);
        Vector3 gantryLit = new(1.0f, 0.6f, 0.2f);
        Vector3 hangar = new(0.08f, 0.1f, 0.14f);
        Vector3 stripe = new(0.95f, 0.55f, 0.12f);

        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 col)
        {
            verts.AddRange(new[] { a.X, a.Y, a.Z, col.X, col.Y, col.Z });
            verts.AddRange(new[] { b.X, b.Y, b.Z, col.X, col.Y, col.Z });
            verts.AddRange(new[] { c.X, c.Y, c.Z, col.X, col.Y, col.Z });
        }

        Tri(new Vector3(-s, 0f, -s * 0.65f), new Vector3(s, 0f, -s * 0.65f), new Vector3(s, 0f, s * 0.65f), padDark);
        Tri(new Vector3(-s, 0f, -s * 0.65f), new Vector3(s, 0f, s * 0.65f), new Vector3(-s, 0f, s * 0.65f), padLight * 0.9f);

        int half = stripeCount / 2;
        for (int i = -half; i <= half; i++)
        {
            float x = i * s * 0.18f;
            Tri(new Vector3(x - s * 0.04f, 0.02f * s, -s * 0.6f), new Vector3(x + s * 0.04f, 0.02f * s, -s * 0.6f), new Vector3(x + s * 0.04f, 0.02f * s, s * 0.6f), stripe);
            Tri(new Vector3(x - s * 0.04f, 0.02f * s, -s * 0.6f), new Vector3(x + s * 0.04f, 0.02f * s, s * 0.6f), new Vector3(x - s * 0.04f, 0.02f * s, s * 0.6f), stripe * 0.85f);
        }

        float hangarW = 0.45f + gantryCount * 0.05f;
        Tri(new Vector3(-s * hangarW, 0.03f * s, -s * 0.15f), new Vector3(s * hangarW, 0.03f * s, -s * 0.15f), new Vector3(s * hangarW, 0.03f * s, s * 0.45f), hangar);
        Tri(new Vector3(-s * hangarW, 0.03f * s, -s * 0.15f), new Vector3(s * hangarW, 0.03f * s, s * 0.45f), new Vector3(-s * hangarW, 0.03f * s, s * 0.45f), hangar * 1.2f);

        float frameH = s * (0.75f + gantryCount * 0.08f);
        for (int g = 0; g < gantryCount; g++)
        {
            float side = g % 2 == 0 ? -1f : 1f;
            float inset = 0.75f + g * 0.12f;
            Tri(new Vector3(side * s * 0.92f, 0f, -s * 0.55f), new Vector3(side * s * inset, 0f, -s * 0.55f), new Vector3(side * s * inset, frameH, -s * 0.55f), gantry);
            Tri(new Vector3(side * s * 0.92f, 0f, -s * 0.55f), new Vector3(side * s * inset, frameH, -s * 0.55f), new Vector3(side * s * 0.92f, frameH, -s * 0.55f), gantryLit);
            Tri(new Vector3(side * s * 0.92f, frameH, s * 0.55f), new Vector3(side * s * inset, frameH, s * 0.55f), new Vector3(side * s * inset, 0f, s * 0.55f), gantry);
        }

        Tri(new Vector3(-s * 0.7f, frameH, 0f), new Vector3(s * 0.7f, frameH, 0f), new Vector3(s * 0.7f, frameH * 0.92f, s * 0.12f), gantryLit);
        Tri(new Vector3(-s * 0.7f, frameH, 0f), new Vector3(s * 0.7f, frameH * 0.92f, s * 0.12f), new Vector3(-s * 0.7f, frameH * 0.92f, s * 0.12f), gantry);

        return Upload(verts.ToArray());
    }

    private static (int vao, int vbo, int vertexCount) Upload(float[] data)
    {
        const int stride = 6;
        int vertexCount = data.Length / stride;
        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
        return (vao, vbo, vertexCount);
    }
}