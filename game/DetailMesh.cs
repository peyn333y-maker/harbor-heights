using Godot;

// A single surface for repeated architectural and prop details.
internal sealed class DetailMesh
{
    private readonly SurfaceTool _surface = new();
    private bool _hasFaces;
    public DetailMesh(Material material)
    {
        _surface.Begin(Godot.Mesh.PrimitiveType.Triangles);
        _surface.SetMaterial(material);
    }

    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color color)
    {
        if ((b - a).Cross(c - a).Dot(normal) > 0) (b, c) = (c, b);
        Vertex(a, normal, color);
        Vertex(b, normal, color);
        Vertex(c, normal, color);
        _hasFaces = true;
    }

    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
    {
        Triangle(a, b, c, normal, color);
        Triangle(a, c, d, normal, color);
    }

    public void Box(Vector3 at, Vector3 size, Color color)
    {
        var a = at - size * .5f;
        var b = at + size * .5f;
        Quad(new(a.X,a.Y,a.Z), new(b.X,a.Y,a.Z), new(b.X,b.Y,a.Z), new(a.X,b.Y,a.Z), Vector3.Forward, color);
        Quad(new(a.X,a.Y,b.Z), new(b.X,a.Y,b.Z), new(b.X,b.Y,b.Z), new(a.X,b.Y,b.Z), Vector3.Back, color);
        Quad(new(a.X,a.Y,a.Z), new(a.X,a.Y,b.Z), new(a.X,b.Y,b.Z), new(a.X,b.Y,a.Z), Vector3.Left, color);
        Quad(new(b.X,a.Y,a.Z), new(b.X,a.Y,b.Z), new(b.X,b.Y,b.Z), new(b.X,b.Y,a.Z), Vector3.Right, color);
        Quad(new(a.X,b.Y,a.Z), new(b.X,b.Y,a.Z), new(b.X,b.Y,b.Z), new(a.X,b.Y,b.Z), Vector3.Up, color);
        Quad(new(a.X,a.Y,a.Z), new(b.X,a.Y,a.Z), new(b.X,a.Y,b.Z), new(a.X,a.Y,b.Z), Vector3.Down, color);
    }

    public void Finish(Node3D root, string name, float range = 0)
    {
        if (!_hasFaces) return;
        var instance = WorldArt.Mesh(root, _surface.Commit(), Vector3.Zero, null);
        instance.Name = name;
        if (range > 0) instance.VisibilityRangeEnd = range;
    }

    private void Vertex(Vector3 p, Vector3 normal, Color color)
    {
        _surface.SetColor(color);
        _surface.SetNormal(normal);
        _surface.SetUV(new Vector2(p.X + p.Z, p.Y + p.Z * .17f));
        _surface.AddVertex(p);
    }
}
