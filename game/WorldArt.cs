using Godot;

internal static class WorldArt
{
    private static readonly Dictionary<(string, float, float), StandardMaterial3D> Materials = new();
    public static StandardMaterial3D Paint(string hex, float roughness = .83f, float metal = 0)
    {
        var key = (hex, roughness, metal);
        if (!Materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = new Color(hex), Roughness = roughness, Metallic = metal };
            Materials[key] = material;
        }
        return material;
    }

    public static MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 at, Material? material)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = at, MaterialOverride = material };
        parent.AddChild(part);
        return part;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 at, Vector3 size, Material material) =>
        Mesh(parent, new BoxMesh { Size = size }, at, material);

    public static MeshInstance3D Ball(Node3D parent, Vector3 at, Vector3 size, Material material, int sides = 12)
    {
        var shape = Mesh(parent, new SphereMesh { Radius = .5f, Height = 1, RadialSegments = sides, Rings = 6 }, at, material);
        shape.Scale = size;
        return shape;
    }

    public static MeshInstance3D Rod(Node3D parent, Vector3 from, Vector3 to, float bottom, float top, Material material, int sides = 10)
    {
        var direction = to - from;
        var piece = Mesh(parent, new CylinderMesh { BottomRadius = bottom, TopRadius = top,
            Height = direction.Length(), RadialSegments = sides, Rings = 0 }, (from + to) * .5f, material);
        piece.Quaternion = new Quaternion(Vector3.Up, direction.Normalized());
        return piece;
    }

    public static StaticBody3D Solid(Node3D parent, Vector3 at, Vector3 size)
    {
        var body = new StaticBody3D { Position = at, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        parent.AddChild(body);
        return body;
    }

    public static Label3D Sign(Node3D parent, string words, Vector3 at, Vector3 face, float scale, string color = "fff4d2")
    {
        var sign = new Label3D { Text = words, Position = at, FontSize = 84, PixelSize = scale,
            OutlineSize = 8, Modulate = new Color(color), OutlineModulate = new Color("173236"),
            Rotation = new Vector3(0, Mathf.Atan2(face.X, face.Z), 0), DoubleSided = false };
        parent.AddChild(sign);
        return sign;
    }
}
