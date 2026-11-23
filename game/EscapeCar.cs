using Godot;

public partial class EscapeCar : RigidBody3D
{
    public const float TopSpeed = 25.8f;
    private const float WheelRadius = .43f;
    private const float SuspensionRange = .64f;
    private const float Spring = 23500;
    private readonly Vector3[] _mounts =
    {
        new(-.88f, .02f, -1.22f), new(.88f, .02f, -1.22f),
        new(-.88f, .02f, 1.05f), new(.88f, .02f, 1.05f)
    };
    private readonly Node3D[] _wheelHubs = new Node3D[4];
    private readonly Node3D[] _tires = new Node3D[4];
    private readonly float[] _spin = new float[4];
    private PhysicsRayQueryParameters3D _ray = null!;
    private float _steering;
    private SuvModel _model = null!;

    public float Throttle { get; set; }
    public float Turn { get; set; }
    public bool Handbrake { get; set; } = true;
    public float Speed => new Vector2(LinearVelocity.X, LinearVelocity.Z).Length();
    public Camera3D DashboardCamera { get; private set; } = null!;

    public void Build(Vector3 position, Vector3 direction)
    {
        Name = "RescueSUV";
        Position = position;
        Basis = Basis.LookingAt(direction, Vector3.Up);
        Mass = 1470;
        CollisionLayer = 2;
        CollisionMask = 5;
        LinearDamp = .055f;
        AngularDamp = .34f;
        CenterOfMassMode = CenterOfMassModeEnum.Custom;
        CenterOfMass = new Vector3(0, -.31f, .12f);
        ContinuousCd = true;
        PhysicsMaterialOverride = new PhysicsMaterial { Friction = .68f, Bounce = .03f };
        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, .19f, .08f),
            Shape = new BoxShape3D { Size = new Vector3(1.94f, .99f, 3.95f) }
        });

        _model = new SuvModel { Name = "Tideline R4" };
        AddChild(_model);
        _model.Build();
        DashboardCamera = _model.Cockpit;
        for (var i = 0; i < 4; i++)
        {
            var hub = new Node3D();
            AddChild(hub);
            _wheelHubs[i] = hub;
            _tires[i] = SuvModel.MakeTire(i % 2 == 0 ? -1 : 1);
            hub.AddChild(_tires[i]);
            hub.Position = _mounts[i] - Vector3.Up * .4f;
        }
        _ray = PhysicsRayQueryParameters3D.Create(Vector3.Zero, Vector3.Down, 5,
            new Godot.Collections.Array<Rid> { GetRid() });
    }

    public void Cabin(bool occupied) => _model.Driver(occupied);

    public void Drive(float dt)
    {
        var forward = -GlobalBasis.Z;
        var up = GlobalBasis.Y;
        var speed = LinearVelocity.Dot(forward);
        var maxSteer = Mathf.Lerp(.51f, .19f, Mathf.Clamp(Mathf.Abs(speed) / TopSpeed, 0, 1));
        _steering = Mathf.MoveToward(_steering, Turn * maxSteer, dt * 1.7f);
        _model.Update(_steering, Speed);

        var reversing = Mathf.Abs(speed) > .9f && Mathf.Abs(Throttle) > .1f && speed * Throttle < 0;
        for (var i = 0; i < _mounts.Length; i++)
        {
            var offset = GlobalBasis * _mounts[i];
            var origin = GlobalPosition + offset;
            _ray.From = origin;
            _ray.To = origin - up * (SuspensionRange + WheelRadius);
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(_ray);
            var extension = SuspensionRange;
            var wheelDirection = i < 2 ? _steering : 0;
            if (hit.Count != 0)
            {
                var contact = hit["position"].AsVector3();
                var normal = hit["normal"].AsVector3();
                extension = Mathf.Clamp(origin.DistanceTo(contact) - WheelRadius, .04f, SuspensionRange);
                var velocity = LinearVelocity + AngularVelocity.Cross(offset);
                var spring = Mathf.Clamp(Spring * (SuspensionRange - extension) - velocity.Dot(up) * 3400, 0, 23500);
                ApplyForce(up * spring, offset);

                var travel = forward.Rotated(up, wheelDirection).Slide(normal).Normalized();
                var lateral = travel.Cross(normal).Normalized();
                var grip = Handbrake && i >= 2 && Mathf.Abs(speed) > 3 ? .48f : 1.1f;
                var sideForce = -velocity.Dot(lateral) * 2400;
                var brake = Handbrake || reversing;
                var speedLimit = Throttle >= 0 ? TopSpeed : 8;
                var ratio = Mathf.Abs(speed) / speedLimit;
                var driving = i >= 2 ? Throttle * 4100 * (1 - Mathf.SmoothStep(.82f, 1, ratio)) : 0;
                var drag = brake ? -velocity.Dot(travel) * 2100 : driving - velocity.Dot(travel) * 25;
                var tireForce = (travel * drag + lateral * sideForce).LimitLength(grip * spring);
                ApplyForce(tireForce, offset);
                _spin[i] = Mathf.PosMod(_spin[i] - velocity.Dot(travel) * dt / WheelRadius, Mathf.Tau);
            }
            _wheelHubs[i].Position = _mounts[i] - Vector3.Up * extension;
            _wheelHubs[i].Rotation = new Vector3(0, wheelDirection, 0);
            _tires[i].Rotation = new Vector3(_spin[i], 0, 0);
        }
    }
}
