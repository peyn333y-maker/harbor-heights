using Godot;

public partial class BeachLife : Node3D
{
    public readonly record struct BeachStop(Vector3 Position, string Prompt, string Reply);
    public List<BeachStop> Stations { get; } = new();
    private sealed class Visitor(Node3D model, AnimationPlayer anim, Vector3 start, float phase, bool stationary)
    {
        public readonly Node3D Model = model;
        public readonly AnimationPlayer Animator = anim;
        public readonly Vector3 Home = start;
        public readonly float Phase = phase;
        public readonly bool Stationary = stationary;
        public string Pose = "";
    }
    private readonly List<Visitor> _people = new();
    private readonly List<RigidBody3D> _floatingProps = new();
    private readonly RandomNumberGenerator _random = new() { Seed = 51911 };
    private readonly StandardMaterial3D _white = WorldArt.Paint("f8ebcd");
    private readonly StandardMaterial3D _wood = WorldArt.Paint("ac7b53");
    private readonly StandardMaterial3D _teal = WorldArt.Paint("27877f");
    private readonly StandardMaterial3D _coral = WorldArt.Paint("d7654c");
    private readonly StandardMaterial3D _steel = WorldArt.Paint("4c5952", .65f, .25f);
    private float _coast;
    private BeachDetail _detail = null!;
    private MeshInstance3D _flag = null!;
    private Node3D _flagPivot = null!;
    private int _flagState = -1;


    public Vector3 Spawn { get; private set; }
    public BeachMatch Match { get; private set; } = null!;
    public SwimCourse Swim { get; private set; } = null!;
    public Vector3 LemonadeStand { get; private set; }
    public Vector3 Lookout { get; private set; }

    public void Build(float coast, float centerZ)
    {
        _coast = coast;
        Spawn = new Vector3(coast - 14.5f, Sand(coast - 14.5f, centerZ) + .03f, centerZ);
        for (var row = 0; row < 2; row++)
        for (var slot = -6; slot <= 6; slot++)
        {
            var z = centerZ + slot * 9.2f + row * 4.4f;
            if (slot == 0 && row == 1 || slot == -2 && row == 0) continue;
            var x = coast - 14.5f + (row == 0 ? 0 : -9) + _random.RandfRange(-.78f, .78f);
            var at = new Vector3(x, Sand(x, z), z);
            var cloth = slot % 3 == 0 ? _coral : slot % 2 == 0 ? WorldArt.Paint("cf9e46") : _teal;
            Umbrella(at - Vector3.Right * .8f + Vector3.Back * 1.05f, cloth, _random.RandfRange(0, 3));
            if (slot != 0 || row != 0) Lounger(at, cloth);
            Lounger(at + Vector3.Back * 2.05f, cloth);
            if (slot % 2 == 0)
            {
                var towel = WorldArt.Box(this, at + new Vector3(2.1f, .022f, 2.04f), new Vector3(1.52f, .04f, .70f), cloth);
                towel.Rotation = new Vector3(0, _random.RandfRange(-.27f, .27f), 0);
                Cooler(at + new Vector3(-.65f, 0, 2.98f), cloth);
            }
        }

        var tower = GroundAt(coast - 24.1f, centerZ - 22.3f);
        Tower(tower);
        var kiosk = GroundAt(coast - 36.2f, centerZ + 8.2f);
        Kiosk(kiosk);
        LemonadeStand = kiosk + Vector3.Right * 1.9f;
        var lookout = GroundAt(coast - 6.1f, centerZ - 6.0f);
        WorldArt.Rod(this, lookout, lookout + Vector3.Up * 1.0f, .086f, .066f, _steel);
        WorldArt.Ball(this, lookout + new Vector3(.06f, 1.13f, 0), new Vector3(.37f, .23f, .32f), _steel);
        WorldArt.Rod(this, lookout + new Vector3(.16f, 1.13f, 0), lookout + new Vector3(.24f, 1.13f, 0), .09f, .12f, _steel);
        Lookout = lookout;
        Stations.Add(new BeachStop(tower + Vector3.Right * 2.3f, "Спросить спасателя о море", "Спасатель советует оставаться перед буйками."));

        var court = GroundAt(coast - 18.5f, centerZ + 77);
        Volleyball(court);
        Match = new BeachMatch();
        AddChild(Match);
        Match.Build(court);
        WorldArt.Sign(this, "ПЛЯЖНЫЙ ВОЛЕЙБОЛ", Match.Entry + new Vector3(-1.2f,1.41f,0), Vector3.Forward, .0025f);
        Swim = new SwimCourse();
        AddChild(Swim);
        Swim.Build(coast, centerZ + 28);
        var board = GroundAt(coast - 32.8f, centerZ - 7);
        WorldArt.Rod(this, board - Vector3.Back * .51f, board + new Vector3(0, 1.75f, -.51f), .045f, .045f, _wood);
        WorldArt.Rod(this, board + Vector3.Back * .51f, board + new Vector3(0, 1.75f, .51f), .045f, .045f, _wood);
        WorldArt.Box(this, board + Vector3.Up * 1.34f, new Vector3(.11f, .88f, 1.44f), _teal);
        WorldArt.Sign(this, "S A L T   B E A C H\nПЛЯЖ   /   ПАРКОВКА", board + new Vector3(.07f, 1.34f, 0), Vector3.Right, .00224f);
        Stations.Add(new BeachStop(board + Vector3.Right, "Прочитать схему пляжа", "Парковка за набережной. Западная дорога ведёт к площадке «Пайн-Ридж»."));
        for (var i = -11; i <= 11; i++)
        {
            var z = centerZ + i * 6.5f;
            var buoy = GroundAt(coast + 21 + 2 * Mathf.Sin(i * .37f), z);
            WorldArt.Ball(this, new Vector3(buoy.X, -.37f, z), new Vector3(.24f, .31f, .24f), _coral);
        }
        for (var i = 0; i < 9; i++)
        {
            var z = centerZ + (i - 4) * 9.6f + 5.2f;
            var x = coast - 19.6f + _random.RandfRange(-.9f, .9f);
            var boardAt = GroundAt(x, z);
            if (i % 3 == 0) FloatingBoard(boardAt, i % 2 == 0 ? _teal : _coral);
            else FloatingCooler(boardAt, i % 2 == 0 ? _teal : _coral);
        }
        MakeVisitors(centerZ, tower, kiosk);
        _detail = new BeachDetail();
        AddChild(_detail);
        _detail.Build(coast, centerZ, kiosk, tower);
    }

    private float Sand(float x, float z)
    {
        if (x < _coast - 32) return -.14f;
        var edge = HarborWater.Shore(_coast, z);
        return -.14f - .12f * Mathf.Clamp((x - (_coast - 32)) / (edge - (_coast - 32)), 0, 1);
    }
    private Vector3 GroundAt(float x, float z) => new(x, Sand(x, z), z);

    private void Umbrella(Vector3 center, Material color, float angle)
    {
        var umbrella = new Node3D { Position = center, Rotation = new Vector3(0, angle, 0) };
        AddChild(umbrella);
        WorldArt.Rod(umbrella, Vector3.Zero, Vector3.Up * 2.39f, .029f, .027f, _steel);
        var mesh = new SurfaceTool();
        mesh.Begin(Mesh.PrimitiveType.Triangles);
        mesh.SetMaterial(new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = .96f });
        var stripe = ((StandardMaterial3D)color).AlbedoColor;
        const int segments = 18;
        for (var i = 0; i < segments; i++)
        {
            var a = Mathf.Tau * i / segments;
            var b = Mathf.Tau * (i + 1) / segments;
            var v1 = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var v2 = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            var top = Vector3.Up * 2.39f;
            var shoulderA = v1 * .86f + Vector3.Up * 2.23f;
            var shoulderB = v2 * .86f + Vector3.Up * 2.23f;
            var rimA = v1 * 1.41f + Vector3.Up * 1.99f;
            var rimB = v2 * 1.41f + Vector3.Up * 1.99f;
            mesh.SetColor(i % 2 == 0 ? stripe : _white.AlbedoColor);
            foreach (var v in new[] { top, shoulderA, shoulderB, shoulderA, rimA, rimB, shoulderA, rimB, shoulderB,
                rimA, (rimA + rimB) / 2 - Vector3.Up * .105f, rimB }) mesh.AddVertex(v);
            if (i % 2 == 0) WorldArt.Rod(umbrella, top - Vector3.Up * .02f, rimA, .0085f, .006f, _steel, 4);
        }
        mesh.GenerateNormals();
        WorldArt.Mesh(umbrella, mesh.Commit(), Vector3.Zero, null);
        WorldArt.Ball(umbrella, Vector3.Up * 2.41f, new Vector3(.089f, .082f, .089f), _white);
    }

    private void Lounger(Vector3 at, Material cloth)
    {
        var chair = new Node3D { Position = at, Rotation = new Vector3(0, _random.RandfRange(-.15f, .15f), 0) };
        AddChild(chair);
        WorldArt.Box(chair, new Vector3(.33f, .32f, 0), new Vector3(1.16f, .11f, .60f), cloth);
        var back = WorldArt.Box(chair, new Vector3(-.55f, .53f, 0), new Vector3(.83f, .11f, .60f), cloth);
        back.Rotation = new Vector3(0, 0, -.53f);
        WorldArt.Box(chair, new Vector3(-.81f, .70f, 0), new Vector3(.23f, .095f, .37f), _white).Rotation = back.Rotation;
        for (var side = -1; side <= 1; side += 2)
        {
            var z = side * .34f;
            WorldArt.Rod(chair, new Vector3(-.23f, .28f, z), new Vector3(.97f, .26f, z), .026f, .026f, _white);
            WorldArt.Rod(chair, new Vector3(-.24f, .3f, z), new Vector3(-.94f, .75f, z), .026f, .026f, _white);
            WorldArt.Rod(chair, new Vector3(-.57f, .015f, z), new Vector3(-.35f, .29f, z), .024f, .024f, _white);
            WorldArt.Rod(chair, new Vector3(.66f, .01f, z), new Vector3(.5f, .29f, z), .024f, .024f, _white);
        }
    }

    private void Cooler(Vector3 at, Material paint)
    {
        WorldArt.Box(this, at + Vector3.Up * .15f, new Vector3(.42f, .28f, .33f), paint);
        WorldArt.Box(this, at + Vector3.Up * .31f, new Vector3(.45f, .065f, .35f), _white);
        WorldArt.Box(this, at + Vector3.Up * .36f, new Vector3(.16f, .036f, .057f), _steel);
    }

    private RigidBody3D FloatingProp(Vector3 position, Vector3 hull, float mass)
    {
        var prop = new RigidBody3D { Position = position, Mass = mass, Freeze = true,
            CollisionLayer = 4, CollisionMask = 7, LinearDamp = .18f, AngularDamp = .65f,
            Rotation = new Vector3(0, _random.RandfRange(-.86f, .86f), 0) };
        prop.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = hull } });
        AddChild(prop);
        _floatingProps.Add(prop);
        return prop;
    }

    private void FloatingBoard(Vector3 at, Material cloth)
    {
        var board = FloatingProp(at + Vector3.Up * .055f, new Vector3(1.55f, .07f, .36f), 3.1f);
        WorldArt.Ball(board, Vector3.Zero, new Vector3(1.93f, .095f, .47f), cloth, 20);
        WorldArt.Ball(board, Vector3.Up * .031f, new Vector3(1.75f, .038f, .255f), _white, 16);
        WorldArt.Box(board, new Vector3(-.40f, .06f, 0), new Vector3(.34f, .018f, .239f), _steel);
    }

    private void FloatingCooler(Vector3 at, Material fabric)
    {
        var cooler = FloatingProp(at + Vector3.Up * .167f, new Vector3(.49f, .32f, .38f), 4.7f);
        WorldArt.Box(cooler, new Vector3(0, -.039f, 0), new Vector3(.49f, .26f, .38f), fabric);
        WorldArt.Box(cooler, new Vector3(0, .116f, 0), new Vector3(.52f, .08f, .41f), _white);
        WorldArt.Box(cooler, new Vector3(0, .164f, 0), new Vector3(.16f, .03f, .064f), _steel);
    }

    public void FloodProps(HarborWater water)
    {
        water.Push(Match.Ball, .26f, 1.25f, .95f);
        foreach (var prop in _floatingProps)
        {
            if (prop.Freeze)
            {
                if (water.Sample(prop.GlobalPosition).Depth(prop.GlobalPosition.Y) < .35f) continue;
                prop.Freeze = false;
            }
            water.Push(prop, .49f, 1.17f, 1.53f);
        }
    }

    private void Tower(Vector3 at)
    {
        var tower = new Node3D { Position = at };
        AddChild(tower);
        for (var x = -1; x <= 1; x += 2)
        for (var z = -1; z <= 1; z += 2)
            WorldArt.Rod(tower, new Vector3(x * 1.89f, -.04f, z * 1.35f),
                new Vector3(x * 1.49f, 1.58f, z * 1.09f), .095f, .095f, _white, 4);
        WorldArt.Box(tower, new Vector3(.31f, 1.52f, 0), new Vector3(4.4f, .24f, 3.12f), _wood);
        WorldArt.Box(tower, new Vector3(-.45f, 2.46f, 0), new Vector3(2.65f, 1.8f, 2.63f), _teal);
        WorldArt.Box(tower, new Vector3(-.4f, 3.44f, 0), new Vector3(3.45f, .19f, 3.14f), _white);
        WorldArt.Box(tower, new Vector3(-.47f, 3.58f, 0), new Vector3(3.22f, .11f, 2.89f), _coral);
        var glass = WorldArt.Paint("25454a", .25f, .36f);
        WorldArt.Box(tower, new Vector3(.89f, 2.67f, 0), new Vector3(.04f, .71f, 2.01f), glass);
        for (var side = -1; side <= 1; side += 2)
        {
            WorldArt.Box(tower, new Vector3(-.24f, 2.67f, side * 1.325f), new Vector3(1.6f, .71f, .044f), glass);
            WorldArt.Rod(tower, new Vector3(.9f, 1.59f, side * 1.44f), new Vector3(2.48f, 1.59f, side * 1.44f), .031f, .031f, _white);
            for (var i = 0; i < 3; i++)
                WorldArt.Rod(tower, new Vector3(1.04f + i * .69f, 1.58f, side * 1.44f),
                    new Vector3(1.04f + i * .69f, 2.22f, side * 1.44f), .026f, .026f, _white);
            WorldArt.Rod(tower, new Vector3(.92f, 2.22f, side * 1.44f), new Vector3(2.48f, 2.22f, side * 1.44f), .031f, .031f, _white);
        }
        WorldArt.Sign(tower, "OCEAN RESCUE   /   04", new Vector3(.93f, 3.18f, 0), Vector3.Right, .00196f);
        WorldArt.Rod(tower, new Vector3(-.68f, 3.59f, -1.17f), new Vector3(-.68f, 5.22f, -1.17f), .026f, .021f, _steel);
        _flagPivot = new Node3D { Position = new Vector3(-.68f,4.91f,-1.17f) };
        tower.AddChild(_flagPivot);
        _flag = WorldArt.Box(_flagPivot,new Vector3(0,0,.38f),new Vector3(.026f,.49f,.74f),WorldArt.Paint("779957"));
        _flag.Rotation = new Vector3(.08f,0,0);
        for (var i = 0; i < 7; i++)
        {
            var step = new Vector3(2.58f + i * .235f, 1.48f - i * .212f, 0);
            WorldArt.Box(tower, step, new Vector3(.26f, .15f, .78f), _wood);
            WorldArt.Solid(tower, step, new Vector3(.26f, .15f, .78f));
        }
        WorldArt.Solid(tower, new Vector3(.31f, 1.52f, 0), new Vector3(4.4f, .24f, 3.12f));
        WorldArt.Solid(tower, new Vector3(-.45f, 2.46f, 0), new Vector3(2.65f, 1.8f, 2.63f));
    }

    private void Kiosk(Vector3 at)
    {
        var root = new Node3D { Position = at };
        AddChild(root);
        WorldArt.Box(root, new Vector3(0, 1.21f, 0), new Vector3(2.74f, 2.48f, 3.44f), _wood);
        WorldArt.Solid(root, new Vector3(0, 1.21f, 0), new Vector3(2.74f, 2.48f, 3.44f));
        WorldArt.Box(root, new Vector3(1.38f, 1.57f, 0), new Vector3(.04f, .97f, 2.2f), _steel);
        WorldArt.Box(root, new Vector3(1.42f, 1.08f, 0), new Vector3(.41f, .105f, 2.45f), _white);
        WorldArt.Box(root, new Vector3(0, 2.50f, 0), new Vector3(3.26f, .17f, 3.77f), _teal);
        for (var stripe = 0; stripe < 11; stripe++)
            WorldArt.Box(root, new Vector3(1.59f, 2.24f, (stripe - 5) * .24f), new Vector3(1.16f, .054f, .24f),
                stripe % 2 == 0 ? _coral : _white).Rotation = new Vector3(0, 0, -.12f);
        WorldArt.Sign(root, "C I T R U S   B A R", new Vector3(1.66f, 2.74f, 0), Vector3.Right, .0026f);
    }

    private void Volleyball(Vector3 at)
    {
        var net = WorldArt.Paint("ddd3b8");
        for (var side = -1; side <= 1; side += 2)
            WorldArt.Rod(this, at + Vector3.Back * side * 3.84f, at + new Vector3(0, 2.52f, side * 3.84f), .047f, .044f, _steel);
        for (var row = 0; row < 10; row++)
            WorldArt.Box(this, at + Vector3.Up * (1.62f + row * .096f), new Vector3(.012f, .013f, 7.66f), net);
        for (var col = -35; col <= 35; col++)
            WorldArt.Box(this, at + new Vector3(0, 2.05f, col * .108f), new Vector3(.012f, .88f, .012f), net);
        WorldArt.Box(this, at + Vector3.Up * 2.51f, new Vector3(.030f, .070f, 7.68f), _white);
        for (var side = -1; side <= 1; side += 2)
        {
            WorldArt.Box(this, at + new Vector3(side * 7, .039f, 0), new Vector3(.055f, .015f, 7.45f), _teal);
            WorldArt.Box(this, at + new Vector3(0, .039f, side * 3.72f), new Vector3(14f, .015f, .055f), _teal);
        }
    }

    private void MakeVisitors(float z, Vector3 tower, Vector3 kiosk)
    {
        for (var i = 0; i < 17; i++)
        {
            var file = i == 0 ? "beach-worker" : i % 2 == 0 ? "beach-woman" : "beach-man";
            var position = i switch
            {
                0 => tower + new Vector3(2.4f, .02f, 2.05f),
                1 => kiosk + new Vector3(2.05f, 0, -.25f),
                2 => GroundAt(_coast - 10.2f, z + 2.4f),
                3 => GroundAt(_coast - 8.5f, z - 4.6f),
                _ => GroundAt(_coast - (i % 3 == 0 ? 30.3f : 6.2f), z + (i - 10) * 7.2f)
            };
            var model = GD.Load<PackedScene>($"res://assets/models/{file}.glb").Instantiate<Node3D>();
            model.Position = position;
            model.Rotation = new Vector3(0, _random.RandfRange(0, Mathf.Tau), 0);
            model.Scale = Vector3.One * (_random.RandfRange(.94f, 1.06f) * (i == 0 ? .94f : .435f));
            AddChild(model);
            var animator = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().FirstOrDefault();
            if (animator == null) continue;
            Recolor(model, i);
            var visitor = new Visitor(model, animator, position, i * 1.27f, i < 5 || i % 5 == 0);
            _people.Add(visitor);
            Animate(visitor, "Idle");
        }
    }

    private static void Recolor(Node3D model, int index)
    {
        string[] shirts = { "eb6d53", "ece5d2", "dfac51", "3a8290", "c49baf", "53875c", "cc6e52", "778eb2" };
        string[] skin = { "aa6f4d", "cd9974", "eac0a0", "8f5e48", "d9a882", "b88266" };
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
        {
            var original = mesh.Mesh.SurfaceGetMaterial(surface);
            var color = original?.ResourceName switch
            {
                "Shirt" => shirts[index % shirts.Length],
                "Pants" => index % 3 == 0 ? "c3aa8b" : index % 3 == 1 ? "34465a" : "ece0c8",
                "Skin" => skin[index % skin.Length],
                "Hair" or "HairBase" when index % 3 == 0 => "54392d",
                "Worker_Vest" => "dd684d",
                _ => ""
            };
            if (color.Length > 0) mesh.SetSurfaceOverrideMaterial(surface, WorldArt.Paint(color));
        }
    }

    public void UpdatePeople(float time, bool evacuate, float dt)
    {
        _detail.WaterLife(time, evacuate);
        var flag = evacuate || time >= 217 ? 2 : time >= 72 ? 1 : 0;
        if (flag != _flagState)
        {
            _flagState = flag;
            _flag.MaterialOverride = WorldArt.Paint(flag == 2 ? "be4e43" : flag == 1 ? "d6b248" : "769354");
        }
        _flagPivot.Rotation = new Vector3(Mathf.Sin(time*4.1f)*.09f, Mathf.Sin(time*(3.1f+flag))*(.29f+flag*.12f), 0);
        foreach (var person in _people)
        {
            if (!person.Model.Visible) continue;
            var here = person.Model.Position;
            if (evacuate)
            {
                var run = new Vector3(-1, 0, Mathf.Sin(person.Phase) * .26f).Normalized();
                Animate(person, "Run");
                var next = here + run * dt * (3.6f + .5f * Mathf.Sin(person.Phase * 2));
                person.Model.Position = next;
                FaceVisitor(person, run, dt);
                if (here.X < _coast - 78) person.Model.Hide();
            }
            else if (!person.Stationary)
            {
                var swing = (time * .113f + person.Phase);
                var offset = Mathf.Sin(swing) * 5.8f;
                var direction = new Vector3(0, 0, Mathf.Cos(swing) > 0 ? 1 : -1);
                var next = person.Home + Vector3.Back * offset;
                person.Model.Position = next;
                FaceVisitor(person, direction, dt);
                Animate(person, Mathf.Abs(Mathf.Cos(swing)) > .22f ? "Walk" : "Idle");
            }
        }
    }

    private static void FaceVisitor(Visitor visitor, Vector3 facing, float dt)
    {
        var angle = Mathf.Atan2(facing.X, facing.Z);
        visitor.Model.Rotation = new Vector3(0, Mathf.LerpAngle(visitor.Model.Rotation.Y, angle, Mathf.Min(1, 4 * dt)), 0);
    }

    private static void Animate(Visitor visitor, string pose)
    {
        if (visitor.Pose == pose) return;
        var key = visitor.Animator.GetAnimationList().FirstOrDefault(clip => clip.EndsWith(pose, StringComparison.Ordinal));
        if (key == null) return;
        visitor.Pose = pose;
        visitor.Animator.GetAnimation(key).LoopMode = Animation.LoopModeEnum.Linear;
        visitor.Animator.Play(key, .21);
    }
}
