using Godot;

public partial class RescueFlight : Node3D
{
    private Node3D _mainRotor = null!;
    private Node3D _tailRotor = null!;
    private Node3D _rotorBlur = null!;
    private Vector3 _pad;
    private float _clock;
    private float _rotorPhase;
    private enum Flight { Hidden, Approach, Landed, Depart }
    private Flight _state;
    public bool Landed => _state == Flight.Landed;
    public bool Finished => _state == Flight.Depart && _clock >= 12.5f;
    public Camera3D Seat { get; private set; } = null!;
    public Vector3 Door => ToGlobal(new Vector3(-1.46f, -.71f, .64f));

    public void Build(Vector3 pad)
    {
        _pad = pad + Vector3.Up * 1.14f;
        Visible = false;
        var red = WorldArt.Paint("b94533", .42f, .24f);
        var white = WorldArt.Paint("eee5cc", .48f, .18f);
        var glass = WorldArt.Paint("294e58", .21f, .37f);
        var frame = WorldArt.Paint("3c4543", .5f, .31f);
        var black = WorldArt.Paint("252e31", .68f, .17f);
        WorldArt.Ball(this, new Vector3(0, .05f, .04f), new Vector3(2.38f, 2.13f, 5.22f), red, 24);
        WorldArt.Ball(this, new Vector3(0, -.48f, -.12f), new Vector3(2.29f, 1.15f, 4.94f), white, 24);
        WorldArt.Ball(this, new Vector3(0, .42f, -1.43f), new Vector3(2.16f, 1.24f, 2.34f), glass, 24);
        WorldArt.Rod(this, new Vector3(0, -.14f, -2.5f), new Vector3(0, 1.0f, -.71f), .043f, .036f, frame, 6);
        WorldArt.Ball(this, new Vector3(0, 1.05f, .53f), new Vector3(1.76f, .80f, 2.37f), red, 16);
        WorldArt.Rod(this, new Vector3(0, .43f, 1.68f), new Vector3(0, .94f, 5.70f), .48f, .12f, red, 12);
        WorldArt.Box(this, new Vector3(0, 1.16f, 5.55f), new Vector3(.19f, 1.48f, .67f), white).Rotation = new Vector3(.43f, 0, 0);
        WorldArt.Box(this, new Vector3(0, .83f, 4.87f), new Vector3(1.87f, .085f, .62f), red);
        for (var side = -1; side <= 1; side += 2)
        {
            WorldArt.Box(this, new Vector3(side * 1.115f, .06f, .28f), new Vector3(.069f, 1.35f, 1.81f), frame);
            WorldArt.Box(this, new Vector3(side * 1.153f, -.16f, .28f), new Vector3(.051f, .86f, 1.69f), red);
            WorldArt.Ball(this, new Vector3(side * 1.17f, .44f, .28f), new Vector3(.10f, .46f, 1.30f), glass);
            WorldArt.Sign(this, "RESCUE  07", new Vector3(side * 1.188f, -.14f, .35f), Vector3.Right * side, .0023f);
            var x = side * 1.06f;
            WorldArt.Rod(this, new Vector3(x, -.995f, -1.83f), new Vector3(x, -.995f, 1.89f), .064f, .064f, frame, 8);
            WorldArt.Rod(this, new Vector3(x, -.995f, -1.83f), new Vector3(x, -.80f, -2.14f), .064f, .049f, frame, 8);
            for (var z = -1; z <= 1; z += 2)
                WorldArt.Rod(this, new Vector3(side * .76f, -.49f, z * 1.03f), new Vector3(x, -.995f, z * 1.11f), .058f, .068f, frame);
        }

        WorldArt.Rod(this, new Vector3(0, 1.21f, .31f), new Vector3(0, 1.81f, .31f), .076f, .094f, black);
        _mainRotor = new Node3D { Position = new Vector3(0, 1.82f, .31f) };
        AddChild(_mainRotor);
        for (var i = 0; i < 4; i++)
        {
            var blade = new Node3D { Rotation = new Vector3(0, Mathf.Pi * .5f * i, 0) };
            _mainRotor.AddChild(blade);
            WorldArt.Box(blade, new Vector3(2.31f, 0, 0), new Vector3(4.47f, .040f, .22f), black);
            WorldArt.Box(blade, new Vector3(4.45f, .005f, 0), new Vector3(.28f, .045f, .23f), white);
        }
        _tailRotor = new Node3D { Position = new Vector3(.19f, 1.03f, 5.53f) };
        AddChild(_tailRotor);
        WorldArt.Box(_tailRotor, Vector3.Zero, new Vector3(.042f, .118f, 1.52f), black);
        WorldArt.Box(_tailRotor, Vector3.Zero, new Vector3(.046f, 1.52f, .118f), black);
        var blur = new StandardMaterial3D { Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(.18f, .22f, .23f, .19f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        _rotorBlur = WorldArt.Mesh(this, new TorusMesh { InnerRadius = 3.57f, OuterRadius = 4.52f, Rings = 36, RingSegments = 8 },
            _mainRotor.Position, blur);
        _rotorBlur.Scale = new Vector3(1, .013f, 1);
        ((_rotorBlur) as MeshInstance3D)!.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        Seat = new Camera3D { Position = new Vector3(-1.49f, .78f, .64f), RotationDegrees = new Vector3(-12, 63, 0),
            Fov = 71, Near = .045f, Far = 12000 };
        AddChild(Seat);
    }

    public void Call()
    {
        _state = Flight.Approach;
        _clock = 0;
        Visible = true;
        Position = _pad + new Vector3(-260, 56, -100);
        Rotation = new Vector3(0, -1.34f, 0);
    }

    public void Board()
    {
        _state = Flight.Depart;
        _clock = 0;
        Seat.MakeCurrent();
    }

    public void Fly(float dt)
    {
        if (_state == Flight.Hidden) return;
        _clock += dt;
        _rotorPhase = Mathf.PosMod(_rotorPhase + dt * 33f, Mathf.Tau);
        _mainRotor.Rotation = new Vector3(0, _rotorPhase, 0);
        _tailRotor.Rotation = new Vector3(-_rotorPhase * 2.2f, 0, 0);
        _rotorBlur.Visible = _state != Flight.Landed;
        if (_state == Flight.Approach)
        {
            var t = Mathf.SmoothStep(0, 13, _clock);
            var height = 56 * (1 - t) + Mathf.Sin(Mathf.Pi * t) * 4;
            Position = _pad + new Vector3(-260 * (1 - t), height, -100 * (1 - t));
            Rotation = new Vector3(-.15f * Mathf.Sin(Mathf.Pi * t), Mathf.Lerp(-1.34f, -Mathf.Pi * .5f, t), .15f * Mathf.Sin(Mathf.Pi * t));
            if (_clock >= 13) _state = Flight.Landed;
        }
        else if (_state == Flight.Depart)
        {
            var t = Mathf.Clamp(_clock / 12.5f, 0, 1);
            Position = _pad + new Vector3(-160 * t * t, 94 * t, 84 * t * t);
            Rotation = new Vector3(.11f * t, -Mathf.Pi * .5f + Mathf.Pi * Mathf.SmoothStep(.02f, .63f, t),
                -.12f * Mathf.Sin(t * Mathf.Pi));
            var view = (Vector3.Right * .94f - GlobalBasis.X * .38f + Vector3.Down * (.26f + .18f * t)).Normalized();
            Seat.GlobalBasis = Basis.LookingAt(view, Vector3.Up);
        }
    }
}
