using Godot;

public partial class HarborWater : Node3D
{
    public readonly record struct WaterSample(float Surface, Vector3 Flow)
    {
        public float Depth(float y) => Mathf.Max(0, Surface - y);
        public float Pressure(float y) => 500 * Depth(y) * Flow.LengthSquared();
    }

    public const float SurgeSpeed = 16.8f;
    public const float FloodLimit = -3130;
    private float _coast;
    private float _age;
    private float _storm;
    private float _surge = -1;
    private float _front;
    private ShaderMaterial _material = null!;

    public float Front => _front;
    public bool SurgeActive => _surge >= 0;
    public bool Offshore(Vector3 p) => p.X >= Shore(_coast, p.Z) + 2.3f;

    public void Build(float coast)
    {
        _coast = coast;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/harbor_surge.gdshader") };
        _material.SetShaderParameter("coast", coast);
        var columns = new List<float>();
        for (float x = -4000; x < coast + 740; x += 4) columns.Add(x);
        columns.AddRange(new[] { coast + 800, coast + 1100, coast + 1800, coast + 3500, coast + 7600 });
        var rows = new List<float> { -7600, -4000, -2400 };
        for (var z = -1800; z <= 1800; z += Math.Abs(z) < 510 ? 7 : 23) rows.Add(z);
        rows.AddRange(new[] { 2400f, 4000f, 7600f });
        var vertices = new Vector3[rows.Count * columns.Count];
        var indices = new int[(rows.Count - 1) * (columns.Count - 1) * 6];
        var i = 0;
        for (var z = 0; z < rows.Count; z++)
        for (var x = 0; x < columns.Count; x++)
        {
            var v = z * columns.Count + x;
            vertices[v] = new Vector3(columns[x], 0, rows[z]);
            if (z == 0 || x == 0) continue;
            var a = v - columns.Count - 1;
            indices[i++] = a;
            indices[i++] = a + 1;
            indices[i++] = v;
            indices[i++] = a;
            indices[i++] = v;
            indices[i++] = v - 1;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = Enumerable.Repeat(Vector3.Up, vertices.Length).ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var grid = new ArrayMesh();
        grid.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var surface = WorldArt.Mesh(this, grid, Vector3.Zero, _material);
        surface.CustomAabb = new Aabb(new Vector3(-4000, -10, -7600), new Vector3(coast + 11600, 72, 15200));
        surface.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        Advance(0, 0, -1);
    }

    public void Advance(float age, float storm, float surge)
    {
        _age = age;
        _storm = storm;
        _surge = surge;
        _front = Mathf.Max(FloodLimit, _coast + 265 - Mathf.Max(surge, 0) * SurgeSpeed);
        _material.SetShaderParameter("age", age);
        _material.SetShaderParameter("storm", storm);
        _material.SetShaderParameter("surge", surge);
        _material.SetShaderParameter("front", _front);
    }

    public static float Shore(float coast, float z) =>
        coast + Mathf.Sin(z * .0043f + .7f) * 6.3f + Mathf.Sin(z * .0092f) * 1.8f;

    // Keep the height field identical to harbor_surge.gdshader so the crest carries physical bodies.
    public WaterSample Sample(Vector3 p)
    {
        var shore = Shore(_coast, p.Z);
        var shelf = Mathf.SmoothStep(shore - 3, shore + 13, p.X);
        var phase = (p.X - _coast) * .51f + p.Z * .038f + _age * 2.25f;
        var ripples = shelf * (.065f + _storm * .28f);
        var surface = -.39f + ripples * (Mathf.Sin(phase) + .36f * Mathf.Sin(phase * 1.82f - p.Z * .12f));
        if (_surge < 0) return new WaterSample(surface, Vector3.Left * shelf * (.33f + _storm * .6f));
        var d = p.X - _front - Bend(p.Z);
        var wet = Mathf.SmoothStep(-9, 10, d);
        var reach = Mathf.SmoothStep(FloodLimit - 4, FloodLimit + 28, p.X);
        var strength = Strength();
        var crest = Mathf.Exp(-Mathf.Pow((d - 15) / 24, 2));
        var trailing = Mathf.Exp(-Mathf.Max(d, 0) / 430);
        var swell = wet * reach * strength;
        surface += swell * (1.25f + 3.4f * trailing + 29 * crest + .45f * Mathf.Sin(d * .19f + p.Z * .09f - _age * 2.8f));
        var flow = swell * (2.4f + 7.2f * trailing + 10 * crest);
        return new WaterSample(surface, new Vector3(-flow, 0, Mathf.Sin(p.Z * .034f + _age) * flow * .12f));
    }

    public void Push(RigidBody3D body, float draft, float buoyancy, float drag, Vector3 offset = default)
    {
        var position = body.GlobalPosition + body.GlobalBasis * offset;
        var sample = Sample(position);
        var immersion = Mathf.Clamp(sample.Depth(position.Y - draft * .5f) / draft, 0, 1);
        if (immersion <= 0) return;
        var pointOffset = position - body.GlobalPosition;
        var pointVelocity = body.LinearVelocity + body.AngularVelocity.Cross(pointOffset);
        var force = (sample.Flow - pointVelocity) * body.Mass * drag * immersion;
        force.Y += body.Mass * 9.81f * buoyancy * immersion;
        body.ApplyForce(force, pointOffset);
    }

    private float Strength() => Mathf.SmoothStep(0, 4, _surge) * (1 - .62f * Mathf.SmoothStep(0, 36,
        Mathf.Max(0, _surge - (_coast + 265 - FloodLimit) / SurgeSpeed)));
    private static float Bend(float z) => Mathf.Sin(z * .009f) * 14 + Mathf.Sin(z * .027f) * 4;
}
