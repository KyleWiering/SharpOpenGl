using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpOpenGl;

/// <summary>Extended procedural ship and shipyard meshes with improved silhouettes.</summary>
public static class ShipMeshExtensions
{
    public static (int vao, int vbo, int vertexCount) BuildFighterMesh(Vector3 color, float size = 2f)
        => Upload(Clamp([
             0f,     0.05f*size,  size*0.95f,  color.X*1.15f, color.Y*1.15f, color.Z*1.15f,
            -size*0.22f, 0f,      size*0.35f,  color.X*0.85f, color.Y*0.85f, color.Z*0.85f,
             size*0.22f, 0f,      size*0.35f,  color.X*0.85f, color.Y*0.85f, color.Z*0.85f,
            -size*0.22f, 0f,      size*0.35f,  color.X*0.75f, color.Y*0.75f, color.Z*0.75f,
            -size*0.55f, 0f,     -size*0.15f,  color.X*0.55f, color.Y*0.55f, color.Z*0.55f,
            -size*0.18f, 0f,     -size*0.45f,  color.X*0.65f, color.Y*0.65f, color.Z*0.65f,
             size*0.22f, 0f,      size*0.35f,  color.X*0.75f, color.Y*0.75f, color.Z*0.75f,
             size*0.18f, 0f,     -size*0.45f,  color.X*0.65f, color.Y*0.65f, color.Z*0.65f,
             size*0.55f, 0f,     -size*0.15f,  color.X*0.55f, color.Y*0.55f, color.Z*0.55f,
            -size*0.18f, 0f,     -size*0.45f,  color.X*0.5f, color.Y*0.5f, color.Z*0.65f,
            -size*0.28f, 0.08f*size,-size*0.65f, color.X*0.35f, color.Y*0.35f, color.Z*0.5f,
             0f,      0f,     -size*0.55f,  color.X*0.4f, color.Y*0.4f, color.Z*0.55f,
             size*0.18f, 0f,     -size*0.45f,  color.X*0.5f, color.Y*0.5f, color.Z*0.65f,
             0f,      0f,     -size*0.55f,  color.X*0.4f, color.Y*0.4f, color.Z*0.55f,
             size*0.28f, 0.08f*size,-size*0.65f, color.X*0.35f, color.Y*0.35f, color.Z*0.5f,
             0f,      0.22f*size, size*0.55f,  color.X*1.2f, color.Y*1.2f, color.Z*1.25f,
            -size*0.12f, 0.08f*size, size*0.1f, color.X*0.9f, color.Y*0.9f, color.Z*0.95f,
             size*0.12f, 0.08f*size, size*0.1f, color.X*0.9f, color.Y*0.9f, color.Z*0.95f,
        ]));

    public static (int vao, int vbo, int vertexCount) BuildScoutMesh(Vector3 color, float size = 1.4f)
        => Upload(Clamp(ScoutVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildDroneMesh(Vector3 color, float size = 0.9f)
        => Upload(Clamp(DroneVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildCorvetteMesh(Vector3 color, float size = 2.2f)
        => Upload(Clamp(CorvetteVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildFrigateMesh(Vector3 color, float size = 3f)
        => Upload(Clamp(FrigateVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildGunshipMesh(Vector3 color, float size = 3.2f)
        => Upload(Clamp(GunshipVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildCruiserMesh(Vector3 color, float size = 4.2f)
        => Upload(Clamp(CruiserVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildTransportMesh(Vector3 color, float size = 3.8f)
        => Upload(Clamp(TransportVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildMinerMesh(Vector3 color, float size = 2.2f)
        => Upload(Clamp(MinerVertices(color, size)));

    public static (int vao, int vbo, int vertexCount) BuildDreadnoughtMesh(Vector3 color, float size = 5.5f)
        => Upload(Clamp(DreadnoughtVertices(color, size)));

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

    private static float[] ScoutVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.04f*s, s, r*1.1f, g*1.1f, b*1.1f,
            -s*0.12f, 0f, s*0.25f, r*0.8f, g*0.8f, b*0.8f,
             s*0.12f, 0f, s*0.25f, r*0.8f, g*0.8f, b*0.8f,
            -s*0.12f, 0f, s*0.25f, r*0.7f, g*0.7f, b*0.7f,
            -s*0.35f, 0f, -s*0.2f, r*0.5f, g*0.5f, b*0.5f,
             0f, 0.12f*s, 0f, r*0.95f, g*0.95f, b*0.95f,
             s*0.12f, 0f, s*0.25f, r*0.7f, g*0.7f, b*0.7f,
             0f, 0.12f*s, 0f, r*0.95f, g*0.95f, b*0.95f,
             s*0.35f, 0f, -s*0.2f, r*0.5f, g*0.5f, b*0.5f,
             0f, 0.18f*s, -s*0.35f, r*0.6f, g*0.75f, b*1.0f,
            -s*0.08f, 0.1f*s, -s*0.55f, r*0.45f, g*0.55f, b*0.8f,
             s*0.08f, 0.1f*s, -s*0.55f, r*0.45f, g*0.55f, b*0.8f,
        ];
    }

    private static float[] DroneVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.1f*s, s*0.5f, r, g, b,
            -s*0.35f, 0f, -s*0.1f, r*0.7f, g*0.7f, b*0.7f,
             s*0.35f, 0f, -s*0.1f, r*0.7f, g*0.7f, b*0.7f,
            -s*0.35f, 0f, -s*0.1f, r*0.6f, g*0.6f, b*0.6f,
            -s*0.45f, 0.05f*s, -s*0.35f, r*0.45f, g*0.45f, b*0.45f,
             0f, 0f, -s*0.25f, r*0.55f, g*0.55f, b*0.55f,
             s*0.35f, 0f, -s*0.1f, r*0.6f, g*0.6f, b*0.6f,
             0f, 0f, -s*0.25f, r*0.55f, g*0.55f, b*0.55f,
             s*0.45f, 0.05f*s, -s*0.35f, r*0.45f, g*0.45f, b*0.45f,
        ];
    }

    private static float[] CorvetteVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.08f*s, s*0.85f, r, g, b,
            -s*0.25f, 0f, s*0.2f, r*0.8f, g*0.8f, b*0.8f,
             s*0.25f, 0f, s*0.2f, r*0.8f, g*0.8f, b*0.8f,
            -s*0.25f, 0f, s*0.2f, r*0.7f, g*0.7f, b*0.7f,
            -s*0.55f, 0.06f*s, -s*0.25f, r*0.55f, g*0.55f, b*0.55f,
            -s*0.2f, 0f, -s*0.55f, r*0.6f, g*0.6f, b*0.6f,
             s*0.25f, 0f, s*0.2f, r*0.7f, g*0.7f, b*0.7f,
             s*0.2f, 0f, -s*0.55f, r*0.6f, g*0.6f, b*0.6f,
             s*0.55f, 0.06f*s, -s*0.25f, r*0.55f, g*0.55f, b*0.55f,
             0f, 0.2f*s, s*0.35f, r*1.05f, g*1.05f, b*1.05f,
            -s*0.15f, 0.1f*s, 0f, r*0.85f, g*0.85f, b*0.85f,
             s*0.15f, 0.1f*s, 0f, r*0.85f, g*0.85f, b*0.85f,
        ];
    }

    private static float[] FrigateVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.1f*s, s*0.75f, r, g, b,
            -s*0.3f, 0f, s*0.15f, r*0.75f, g*0.75f, b*0.75f,
             s*0.3f, 0f, s*0.15f, r*0.75f, g*0.75f, b*0.75f,
            -s*0.3f, 0f, s*0.15f, r*0.65f, g*0.65f, b*0.65f,
            -s*0.65f, 0.05f*s, -s*0.15f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.25f, 0.12f*s, -s*0.55f, r*0.55f, g*0.55f, b*0.55f,
             s*0.3f, 0f, s*0.15f, r*0.65f, g*0.65f, b*0.65f,
             s*0.25f, 0.12f*s, -s*0.55f, r*0.55f, g*0.55f, b*0.55f,
             s*0.65f, 0.05f*s, -s*0.15f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.25f, 0.12f*s, -s*0.55f, r*0.45f, g*0.45f, b*0.45f,
             s*0.25f, 0.12f*s, -s*0.55f, r*0.45f, g*0.45f, b*0.45f,
             0f, 0.18f*s, -s*0.75f, r*0.4f, g*0.4f, b*0.4f,
        ];
    }

    private static float[] GunshipVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.15f*s, s*0.65f, r, g, b,
            -s*0.45f, 0.05f*s, s*0.05f, r*0.75f, g*0.75f, b*0.75f,
             s*0.45f, 0.05f*s, s*0.05f, r*0.75f, g*0.75f, b*0.75f,
            -s*0.45f, 0.05f*s, s*0.05f, r*0.65f, g*0.65f, b*0.65f,
            -s*0.55f, 0.1f*s, -s*0.35f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.35f, 0.18f*s, -s*0.6f, r*0.55f, g*0.55f, b*0.55f,
             s*0.45f, 0.05f*s, s*0.05f, r*0.65f, g*0.65f, b*0.65f,
             s*0.35f, 0.18f*s, -s*0.6f, r*0.55f, g*0.55f, b*0.55f,
             s*0.55f, 0.1f*s, -s*0.35f, r*0.5f, g*0.5f, b*0.5f,
             0f, 0.28f*s, s*0.15f, r*1.1f, g*1.1f, b*1.1f,
            -s*0.18f, 0.22f*s, -s*0.1f, r*0.85f, g*0.85f, b*0.85f,
             s*0.18f, 0.22f*s, -s*0.1f, r*0.85f, g*0.85f, b*0.85f,
        ];
    }

    private static float[] CruiserVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.12f*s, s*0.8f, r, g, b,
            -s*0.45f, 0f, s*0.25f, r*0.75f, g*0.75f, b*0.75f,
             s*0.45f, 0f, s*0.25f, r*0.75f, g*0.75f, b*0.75f,
            -s*0.45f, 0f, s*0.25f, r*0.65f, g*0.65f, b*0.65f,
            -s*0.55f, 0.18f*s, -s*0.2f, r*0.55f, g*0.55f, b*0.55f,
            -s*0.4f, 0.22f*s, -s*0.65f, r*0.5f, g*0.5f, b*0.5f,
             s*0.45f, 0f, s*0.25f, r*0.65f, g*0.65f, b*0.65f,
             s*0.4f, 0.22f*s, -s*0.65f, r*0.5f, g*0.5f, b*0.5f,
             s*0.55f, 0.18f*s, -s*0.2f, r*0.55f, g*0.55f, b*0.55f,
             0f, 0.3f*s, s*0.05f, r*0.95f, g*0.95f, b*0.95f,
            -s*0.25f, 0.25f*s, -s*0.35f, r*0.8f, g*0.8f, b*0.8f,
             s*0.25f, 0.25f*s, -s*0.35f, r*0.8f, g*0.8f, b*0.8f,
        ];
    }

    private static float[] TransportVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.08f*s, s*0.55f, r, g, b,
            -s*0.55f, 0f, s*0.1f, r*0.75f, g*0.75f, b*0.75f,
             s*0.55f, 0f, s*0.1f, r*0.75f, g*0.75f, b*0.75f,
            -s*0.55f, 0f, s*0.1f, r*0.65f, g*0.65f, b*0.65f,
            -s*0.6f, 0.12f*s, -s*0.45f, r*0.55f, g*0.55f, b*0.55f,
            -s*0.35f, 0.15f*s, -s*0.65f, r*0.5f, g*0.5f, b*0.5f,
             s*0.55f, 0f, s*0.1f, r*0.65f, g*0.65f, b*0.65f,
             s*0.35f, 0.15f*s, -s*0.65f, r*0.5f, g*0.5f, b*0.5f,
             s*0.6f, 0.12f*s, -s*0.45f, r*0.55f, g*0.55f, b*0.55f,
             0f, 0.2f*s, 0f, r*0.85f, g*0.85f, b*0.85f,
            -s*0.25f, 0.18f*s, -s*0.2f, r*0.7f, g*0.7f, b*0.7f,
             s*0.25f, 0.18f*s, -s*0.2f, r*0.7f, g*0.7f, b*0.7f,
        ];
    }

    private static float[] MinerVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.06f*s, s*0.6f, r, g, b,
            -s*0.4f, 0f, s*0.05f, r*0.8f, g*0.8f, b*0.8f,
             s*0.4f, 0f, s*0.05f, r*0.8f, g*0.8f, b*0.8f,
            -s*0.4f, 0f, s*0.05f, r*0.7f, g*0.7f, b*0.7f,
            -s*0.5f, 0.1f*s, -s*0.35f, r*0.55f, g*0.55f, b*0.55f,
            -s*0.2f, 0.14f*s, -s*0.55f, r*0.6f, g*0.6f, b*0.6f,
             s*0.4f, 0f, s*0.05f, r*0.7f, g*0.7f, b*0.7f,
             s*0.2f, 0.14f*s, -s*0.55f, r*0.6f, g*0.6f, b*0.6f,
             s*0.5f, 0.1f*s, -s*0.35f, r*0.55f, g*0.55f, b*0.55f,
             0f, 0.22f*s, s*0.35f, r*1.05f, g*0.95f, b*0.35f,
            -s*0.12f, 0.16f*s, s*0.05f, r*0.85f, g*0.8f, b*0.3f,
             s*0.12f, 0.16f*s, s*0.05f, r*0.85f, g*0.8f, b*0.3f,
        ];
    }

    private static float[] DreadnoughtVertices(Vector3 color, float s)
    {
        float r = color.X, g = color.Y, b = color.Z;
        return [
             0f, 0.18f*s, s*0.85f, r, g, b,
            -s*0.5f, 0.05f*s, s*0.2f, r*0.75f, g*0.75f, b*0.75f,
             s*0.5f, 0.05f*s, s*0.2f, r*0.75f, g*0.75f, b*0.75f,
            -s*0.5f, 0.05f*s, s*0.2f, r*0.65f, g*0.65f, b*0.65f,
            -s*0.7f, 0.2f*s, -s*0.15f, r*0.5f, g*0.5f, b*0.5f,
            -s*0.45f, 0.28f*s, -s*0.65f, r*0.55f, g*0.55f, b*0.55f,
             s*0.5f, 0.05f*s, s*0.2f, r*0.65f, g*0.65f, b*0.65f,
             s*0.45f, 0.28f*s, -s*0.65f, r*0.55f, g*0.55f, b*0.55f,
             s*0.7f, 0.2f*s, -s*0.15f, r*0.5f, g*0.5f, b*0.5f,
             0f, 0.38f*s, s*0.05f, r*1.05f, g*1.05f, b*1.05f,
            -s*0.3f, 0.32f*s, -s*0.35f, r*0.85f, g*0.85f, b*0.85f,
             s*0.3f, 0.32f*s, -s*0.35f, r*0.85f, g*0.85f, b*0.85f,
            -s*0.45f, 0.28f*s, -s*0.65f, r*0.45f, g*0.45f, b*0.45f,
             s*0.45f, 0.28f*s, -s*0.65f, r*0.45f, g*0.45f, b*0.45f,
             0f, 0.35f*s, -s*0.85f, r*0.4f, g*0.4f, b*0.4f,
        ];
    }

    private static float[] Clamp(float[] vertices)
    {
        for (int i = 0; i < vertices.Length; i += 6)
        {
            vertices[i + 3] = MathHelper.Clamp(vertices[i + 3], 0f, 1f);
            vertices[i + 4] = MathHelper.Clamp(vertices[i + 4], 0f, 1f);
            vertices[i + 5] = MathHelper.Clamp(vertices[i + 5], 0f, 1f);
        }
        return vertices;
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