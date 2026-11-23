using Godot;

public partial class HarborPlayer : CharacterBody3D
{
    private float _stride;
    private float _stepAt;
    private float _yaw;
    private float _pitch;
    private float _stroke;
    private float _sipTimer;
    private float _reachTimer;
    private float _holdY;
    private bool _wetSound;
    private bool _exhausted;
    private PlayerBody _body = null!;
    public bool Resting { get; set; } = true;
    public bool Running { get; private set; }
    public bool Swimming { get; private set; }
    public bool Zoomed { get; set; }
    public bool StowCup { get; set; }
    public float Energy { get; private set; } = 1;
    public int Sips { get; private set; }
    public bool HaveCup => Sips > 0 || _sipTimer > 0;
    public bool IsSipping => _sipTimer > 0;
    public Camera3D Eye { get; private set; } = null!;
    public Action<bool>? StepSound { get; set; }
    public Action? Splash { get; set; }

    public void Build(Vector3 spawn)
    {
        Name = "FirstPerson";
        Position = spawn;
        CollisionLayer = 8;
        CollisionMask = 7;
        FloorSnapLength = .33f;
        AddChild(new CollisionShape3D { Position = Vector3.Up * .88f,
            Shape = new CapsuleShape3D { Radius = .30f, Height = 1.76f } });
        Eye = new Camera3D { Position = new Vector3(0, .98f, -.065f), Fov = 76, Near = .045f, Far = 12000, Current = true };
        AddChild(Eye);
        _body = new PlayerBody();
        AddChild(_body);
        _body.Build();
        _body.Pose(0,0,false,true,0,Eye,false,0,0);
        Face(new Vector3(.76f, 0, .65f), -.045f);
    }

    public void Face(Vector3 heading, float pitch = 0)
    {
        _yaw = Mathf.Atan2(-heading.X, -heading.Z);
        _pitch = pitch;
        SetView();
    }

    public void Mouse(Vector2 delta)
    {
        var sensitivity = Zoomed ? .00085f : .0021f;
        _yaw -= delta.X * sensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.Y * sensitivity, -1.46f, 1.43f);
        SetView();
    }

    private void SetView()
    {
        Rotation = new Vector3(0, _yaw, 0);
        Eye.Rotation = new Vector3(_pitch, 0, 0);
    }

    public void Embody(bool visible)
    {
        _body.Visible = visible;
        Zoomed = false;
        if (!visible) Swimming = false;
    }

    public void Serve() => Sips = 3;

    public bool Sip()
    {
        if (Sips == 0 || IsSipping || Swimming) return false;
        Sips--;
        _sipTimer = 1.13f;
        return true;
    }

    public void Reach() => _reachTimer = .43f;

    public void Walk(Vector2 input, bool sprint, bool jump, bool rise, bool dive, HarborWater water, float dt)
    {
        if (input.LengthSquared() > .01f || jump) { Resting = false; Zoomed = false; }
        var current = Velocity;
        var sample = water.Sample(GlobalPosition);
        var depth = sample.Depth(GlobalPosition.Y);
        var wasSwimming = Swimming;
        Swimming = water.Offshore(GlobalPosition) && (wasSwimming ? depth > .47f : depth > .68f);
        if (Swimming && !wasSwimming) _holdY = sample.Surface - 1.25f;
        if (Swimming != _wetSound)
        {
            _wetSound = Swimming;
            Splash?.Invoke();
        }
        if (Energy < .035f) _exhausted = true;
        if (Energy > .30f) _exhausted = false;
        Running = !Swimming && sprint && !_exhausted && input.LengthSquared() > .1f && !Resting;
        Energy = Mathf.Clamp(Energy + (Running ? -.051f : .032f) * dt, 0, 1);
        var speed = Running ? 7.5f : 4.4f;
        var wish = Basis * new Vector3(input.X, 0, -input.Y).LimitLength(1);
        FloorSnapLength = Swimming ? 0 : .33f;
        if (Swimming)
        {
            var swimSpeed = sprint ? 3.75f : 2.9f;
            var target = wish * swimSpeed + sample.Flow * (water.SurgeActive ? .69f : .18f);
            _holdY = Mathf.Clamp(_holdY + ((rise ? 1 : 0) - (dive ? 1 : 0)) * dt * 2.35f,
                sample.Surface - 5.0f, sample.Surface - 1.24f);
            if (!dive) _holdY = Mathf.MoveToward(_holdY, sample.Surface - 1.25f, dt * (rise ? 4f : .37f));
            current = new Vector3(Mathf.MoveToward(current.X,target.X,8*dt),
                Mathf.MoveToward(current.Y,Mathf.Clamp((_holdY-GlobalPosition.Y)*4,-2.6f,3.8f),dt*11),
                Mathf.MoveToward(current.Z,target.Z,8*dt));
        }
        else
        {
            var wet = Mathf.Clamp(depth / 1.4f, 0, 1);
            var target = wish * speed * (1 - .49f * wet) + sample.Flow * wet * .48f;
            var rate = IsOnFloor() ? 28 : 8;
            current.X = Mathf.MoveToward(current.X,target.X,rate*dt);
            current.Z = Mathf.MoveToward(current.Z,target.Z,rate*dt);
            current.Y = IsOnFloor() && current.Y <= 0 ? -.4f : current.Y - 18*dt;
            if (jump && IsOnFloor() && depth < .55f) current.Y = 5.1f;
            if (depth > 1.1f) current.Y = Mathf.MoveToward(current.Y,.70f,12*dt);
        }
        Velocity = current;
        MoveAndSlide();

        var pace = IsOnFloor() || Swimming ? GetRealVelocity().Slide(Vector3.Up).Length() : 0;
        _stride += dt * pace * 2.25f;
        _stroke += dt * (1.15f + pace * 1.27f);
        var targetEye = Swimming ? 1.49f : Resting ? .98f : 1.61f;
        var bob = Swimming ? Mathf.Cos(_stroke*2)*.036f : pace > .8f ? Mathf.Cos(_stride*2)*.023f*pace/speed : 0;
        Eye.Position = new Vector3(Mathf.Sin(Swimming ? _stroke : _stride) * (pace > .8f ? .014f : 0),
            Mathf.Lerp(Eye.Position.Y,targetEye+bob,Mathf.Min(1,8*dt)), Swimming ? -.12f : -.065f);
        Eye.Fov = Mathf.Lerp(Eye.Fov, Zoomed ? 25 : Running ? 80 : 76, Mathf.Min(1,9*dt));
        if (!Swimming && _stride > _stepAt && pace > .8f)
        {
            _stepAt = _stride + Mathf.Pi;
            StepSound?.Invoke(Running);
        }
        if (Swimming && Mathf.Sin(_stroke*2) > .97f && Mathf.Sin((_stroke-dt*(1.15f+pace*1.27f))*2) <= .97f && pace > .6f)
            Splash?.Invoke();

        var before = _sipTimer;
        _sipTimer = Mathf.Max(0, _sipTimer - dt);
        if (before > .56f && _sipTimer <= .56f) Energy = Mathf.Min(1, Energy + .44f);
        _reachTimer = Mathf.Max(0, _reachTimer - dt);
        var sipping = _sipTimer > 0 ? Mathf.Sin(Mathf.Pi * _sipTimer / 1.13f) : 0;
        var reaching = _reachTimer > 0 ? Mathf.Sin(Mathf.Pi * _reachTimer / .43f) : 0;
        _body.Pose(_stride,pace,Swimming,Resting,_stroke,Eye,HaveCup && !Swimming && !StowCup,sipping,reaching);
    }
}
