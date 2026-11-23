using Godot;

public partial class FloodDamage : Node3D
{
    private sealed class Structure(Node3D node, Vector3 size, Basis orientation, Color tint, float strength)
    {
        public readonly Node3D Node = node;
        public readonly Vector3 Origin = node.GlobalPosition;
        public readonly Vector3 Size = size;
        public readonly Basis Orientation = orientation;
        public readonly float VisualYaw = node.Rotation.Y;
        public readonly Color Tint = tint;
        public readonly float Strength = strength;
        public float Load;
        public float Failure = -1;
    }

    private readonly record struct Fragment(RigidBody3D Body, float Draft, float CreatedAt);
    private readonly List<Structure> _buildings = new();
    private readonly List<Fragment> _fragments = new();
    private readonly RandomNumberGenerator _random = new() { Seed = 17107 };
    private float _fluidTick;
    private float _elapsed;
    public int Collapsed { get; private set; }
    public Action<Vector3, float>? CollapseSound { get; set; }

    public void RegisterCity(Node3D city)
    {
        foreach (var lot in GetTree().GetNodesInGroup("flood_building").OfType<Node3D>())
        {
            if (!city.IsAncestorOf(lot)) continue;
            var body = lot.GetChildren().OfType<StaticBody3D>().First();
            var volumes = body.GetChildren().OfType<CollisionShape3D>().ToArray();
            if (volumes.Length == 0) continue;
            var first = volumes[0];
            var size = first.Shape is BoxShape3D box ? box.Size : new Vector3(18, 22, 18);
            size.Y = volumes.Max(volume => volume.Shape is BoxShape3D section
                ? volume.Position.Y + section.Size.Y * .5f : 23);
            var facade = lot.GetChildren().OfType<MeshInstance3D>().First().Mesh;
            var colors = facade.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Color].AsColorArray();
            Register(lot, size, first.Basis, colors.Length == 0 ? new Color("bba695") : colors[0].Lightened(.2f));
        }
    }

    public void Register(Node3D lot, Vector3 size, Basis orientation, Color tint)
    {
        var strength = _random.RandfRange(.8f, 1.35f) * Mathf.Clamp(.8f + size.X * size.Z * .00055f, .8f, 1.55f);
        _buildings.Add(new Structure(lot, size, orientation, tint, strength));
    }

    public void Step(HarborWater water, Vector3 eye, float dt)
    {
        _elapsed += dt;
        _fluidTick += dt;
        for (var i = _fragments.Count - 1; i >= 0; i--)
        {
            var part = _fragments[i];
            var age = _elapsed - part.CreatedAt;
            if (age > 11 && (part.Body.GlobalPosition.DistanceSquaredTo(eye) > 340 * 340 || age > 28))
            {
                part.Body.QueueFree();
                _fragments.RemoveAt(i);
                continue;
            }
            water.Push(part.Body, part.Draft, .31f, .55f);
        }

        if (_fluidTick < .10f) return;
        var step = _fluidTick;
        _fluidTick = 0;
        for (var i = _buildings.Count - 1; i >= 0; i--)
        {
            var building = _buildings[i];
            if (building.Origin.X < water.Front - 25) continue;
            if (building.Failure >= 0)
            {
                var delay = _elapsed - building.Failure;
                if (delay > .78f)
                {
                    Break(building, water, eye);
                    _buildings.RemoveAt(i);
                }
                else
                {
                    var visibleTilt = .12f * Mathf.Pow(delay / .78f, 2);
                    building.Node.Rotation = new Vector3(.016f * Mathf.Sin(delay * 60), building.VisualYaw, visibleTilt);
                }
                continue;
            }
            var sample = water.Sample(building.Origin);
            var pressure = sample.Pressure(building.Origin.Y);
            building.Load = Mathf.Max(0, building.Load + (pressure / 800_000f - .12f) * step);
            if (building.Load >= building.Strength) building.Failure = _elapsed;
        }
    }

    private void Break(Structure building, HarborWater water, Vector3 eye)
    {
        Collapsed++;
        var distance = building.Origin.DistanceSquaredTo(eye);
        if (distance < 285 * 285) CollapseSound?.Invoke(building.Origin, building.Size.Y);

        building.Node.Hide();
        foreach (var solid in building.Node.GetChildren().OfType<StaticBody3D>())
            solid.SetDeferred(CollisionObject3D.PropertyName.CollisionLayer, 0);

        var tiers = Mathf.Clamp(Mathf.CeilToInt(building.Size.Y / 11), 2, 7);
        var sectionH = building.Size.Y / tiers;
        var shell = WorldArt.Paint(building.Tint.ToHtml(false));
        var dark = WorldArt.Paint(building.Tint.Darkened(.43f).ToHtml(false));
        var glazing = WorldArt.Paint("3d5357", .36f, .2f);
        if (distance < 840 * 840 && _fragments.Count < 165)
        {
            for (var j = 0; j < tiers; j++)
            for (var side = -1; side <= 1; side += 2)
            {
                var offset = building.Orientation * new Vector3(side * building.Size.X * .25f, (j + .5f) * sectionH, 0);
                var size = new Vector3(building.Size.X * .48f, sectionH * .95f, building.Size.Z * .96f);
                var position = building.Origin + offset + Vector3.Left * j * .3f;
                var part = new RigidBody3D
                {
                    Name = "FloodFracture", CollisionLayer = 4, CollisionMask = 7, Mass = 250 + size.X * size.Y * size.Z * 1.2f,
                    Position = position, Basis = building.Orientation, LinearDamp = .16f, AngularDamp = .36f,
                    PhysicsMaterialOverride = new PhysicsMaterial { Friction = .72f, Bounce = .05f }
                };
                part.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
                WorldArt.Box(part, Vector3.Zero, size, shell);
                WorldArt.Box(part, new Vector3(0, size.Y * .48f, 0), new Vector3(size.X * 1.01f, .28f, size.Z * 1.01f), dark);
                for (var band = -1; band <= 1; band++)
                    WorldArt.Box(part, new Vector3(0, band * size.Y * .29f, MathF.Sign(side) * size.Z * .501f),
                        new Vector3(size.X * .85f, size.Y * .15f, .12f), glazing);
                AddChild(part);
                var current = water.Sample(position).Flow;
                part.LinearVelocity = current * .72f + new Vector3(-1.5f - j * .48f, .8f, side * 2.1f);
                part.AngularVelocity = new Vector3(_random.RandfRange(-.4f, .4f), side * .22f, .37f + .041f * j);
                _fragments.Add(new Fragment(part, sectionH, _elapsed));
            }
        }

        var rubble = new Node3D { Position = building.Origin };
        AddChild(rubble);
        for (var i = 0; i < 5; i++)
        {
            var size = new Vector3(building.Size.X * _random.RandfRange(.15f, .36f), _random.RandfRange(.8f, 2.9f),
                building.Size.Z * _random.RandfRange(.17f, .42f));
            var pos = new Vector3(_random.RandfRange(-.27f, .27f) * building.Size.X, .57f,
                _random.RandfRange(-.26f, .26f) * building.Size.Z);
            var slab = WorldArt.Box(rubble, pos, size, i % 2 == 0 ? dark : shell);
            slab.Rotation = new Vector3(_random.RandfRange(-.27f, .27f), _random.RandfRange(-1, 1), _random.RandfRange(-.36f, .36f));
            slab.CreateConvexCollision();
        }
    }
}
