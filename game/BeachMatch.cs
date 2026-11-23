using Godot;

public partial class BeachMatch : Node3D
{
    private enum Rally { Ready, Toss, Live, Reset, Finished }
    private Rally _rally;
    private Node3D _opponent = null!;
    private AnimationPlayer? _animation;
    private string _pose = "";
    private float _recover;
    private float _age;
    private float _hitLock;
    private int _side;
    private bool _opponentHit;
    private bool _playerHit;
    private bool _crossed;
    private Vector3 _serveAim;
    public RigidBody3D Ball { get; private set; } = null!;
    public bool Active { get; private set; }
    public bool ServeReady => Active && _rally == Rally.Ready;
    public bool Ended => Active && _rally == Rally.Finished;
    public int Home { get; private set; }
    public int Away { get; private set; }
    public bool Complete { get; private set; }
    public Vector3 Entry => GlobalPosition + new Vector3(-4.1f, 0, -4.35f);
    public string Score => _rally == Rally.Finished ? $"Финал   {Home} : {Away}" : $"Волейбол   {Home} : {Away}   ·   до 5";

    public void Build(Vector3 at)
    {
        Position = at;
        Ball = new RigidBody3D
        {
            Position = new Vector3(-3.8f,.22f,1.15f), Mass = .27f, CollisionLayer = 4,
            CollisionMask = 1, Freeze = true, ContinuousCd = true,
            PhysicsMaterialOverride = new PhysicsMaterial { Bounce = .58f, Friction = .49f }
        };
        Ball.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = .135f } });
        Ball.AddChild(BallSkin());
        AddChild(Ball);
        _opponent = GD.Load<PackedScene>("res://assets/models/beach-woman.glb").Instantiate<Node3D>();
        _opponent.Scale = Vector3.One * .439f;
        _opponent.Position = new Vector3(4.3f,0,.35f);
        _opponent.Rotation = new Vector3(0,-Mathf.Pi*.5f,0);
        AddChild(_opponent);
        _animation = _opponent.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().FirstOrDefault();
        Animated("Idle");
    }

    public bool Access(Vector3 player) => player.DistanceTo(Entry) < 2.9f ||
        Active && player.DistanceTo(GlobalPosition) < 10.5f;
    public bool CanHit(Vector3 eye) => Active && _rally is Rally.Ready or Rally.Live &&
        _hitLock <= 0 && !_playerHit && eye.DistanceTo(Ball.GlobalPosition) < 2.5f;

    public void Toggle()
    {
        if (Active) { Abort(); return; }
        Active = true;
        Home = Away = 0;
        _rally = Rally.Ready;
        ResetBall();
    }

    public void Abort()
    {
        Active = false;
        Ball.Freeze = false;
        Animated("Idle");
    }

    public void LeaveCourt(float dt)
    {
        Ball.Freeze = false;
        _opponent.Position += Vector3.Left * dt * 4.4f;
        Animated("Run");
        if (_opponent.Position.X < -30) _opponent.Hide();
    }

    private void ResetBall()
    {
        Ball.Freeze = true;
        Ball.Position = new Vector3(-3.55f,.145f,-1.33f);
        Ball.LinearVelocity = Vector3.Zero;
        Ball.AngularVelocity = Vector3.Zero;
        _side = -1;
        _age = 0;
        _hitLock = 0;
        _playerHit = _opponentHit = _crossed = false;
    }

    public bool Hit(Vector3 eye, Vector3 aim)
    {
        if (!CanHit(eye)) return false;
        if (_rally == Rally.Ready)
        {
            _rally = Rally.Toss;
            _age = 0;
            _serveAim = aim;
            return true;
        }
        SendAcross(aim);
        return true;
    }

    private void SendAcross(Vector3 aim)
    {
        Ball.Freeze = false;
        Ball.Sleeping = false;
        var z = Mathf.Clamp(aim.Z * 4.65f,-3.31f,3.31f);
        var intent = new Vector3(Mathf.Clamp(5.5f + aim.Y*3.5f,4.1f,7.6f), 0, z) - Ball.Position;
        var flight = .99f + Mathf.Clamp(intent.X*.032f,.07f,.40f);
        Ball.LinearVelocity = new Vector3(intent.X / flight,
            Mathf.Clamp((.10f - Ball.Position.Y + 4.9f*flight*flight)/flight,5.9f,8.4f), intent.Z/flight);
        Ball.AngularVelocity = new Vector3(6.7f,3f,-8.2f);
        _rally = Rally.Live;
        _playerHit = true;
        _hitLock = .24f;
    }

    public void Step(Vector3 eye, float dt)
    {
        if (!Active) return;
        _hitLock = Mathf.Max(0,_hitLock-dt);
        _age += dt;
        if (eye.DistanceTo(GlobalPosition) > 28) { Abort(); return; }
        if (_rally == Rally.Toss)
        {
            Ball.Position = new Vector3(-3.55f,Mathf.Lerp(.145f,1.18f,Mathf.SmoothStep(0,.29f,_age)),-1.33f);
            if (_age >= .29f)
            {
                _age = 0;
                SendAcross(_serveAim);
            }
            return;
        }
        if (_rally == Rally.Reset && _age > 1.18f)
        {
            if (Home >= 5 || Away >= 5)
            {
                Complete |= Home >= 5;
                _rally = Rally.Finished;
                Ball.Freeze = true;
                Animated(Home >= 5 ? "Clapping" : "Idle");
            }
            else
            {
                _rally = Rally.Ready;
                ResetBall();
            }
        }
        if (_rally != Rally.Live) return;
        var b = Ball.Position;
        var v = Ball.LinearVelocity;
        var side = b.X >= 0 ? 1 : -1;
        if (side != _side)
        {
            if (b.Y < 2.52f && Mathf.Abs(b.Z) < 3.85f) { Point(_side > 0); return; }
            _side = side;
            _crossed = true;
            _playerHit = side > 0;
            if (side > 0) _opponentHit = false;
        }

        _recover = Mathf.Max(0,_recover-dt);
        var home = new Vector3(4.65f,0,.3f);
        var target = side > 0 && _age > .79f ? new Vector3(Mathf.Clamp(b.X+v.X*.21f,1.5f,6f),0,Mathf.Clamp(b.Z+v.Z*.28f,-3.2f,3.2f)) : home;
        _opponent.Position = _opponent.Position.MoveToward(target,dt*2.52f);
        if (_recover <= 0) Animated(_opponent.Position.DistanceTo(target) > .15f ? "Walk" : "Idle");
        // A receiver can only return a descending ball within reach. Aim past their starting position to score.
        if (side > 0 && !_opponentHit && b.Y is > 1.03f and < 2.12f && v.Y < -.8f &&
            (b - _opponent.Position - Vector3.Up*1.25f).LengthSquared() < 1.35f)
        {
            var local = ToLocal(eye);
            var landing = new Vector3(Mathf.Clamp(local.X,-6.25f,-3.5f),.1f,
                Mathf.Clamp(local.Z+Mathf.Sin((Home+Away)*1.8f)*1.16f,-3.15f,3.15f));
            var time = 1.31f + Mathf.Clamp((b.X-landing.X)*.026f,.1f,.38f);
            Ball.LinearVelocity = new Vector3((landing.X-b.X)/time, (landing.Y-b.Y)/time+4.9f*time,(landing.Z-b.Z)/time);
            Ball.AngularVelocity = new Vector3(3.8f,1.6f,6.7f);
            _opponentHit = true;
            _recover = .48f;
            Animated("Clapping");
        }

        if (_age < .27f) return;
        if (b.Y <= .155f || MathF.Abs(b.Z) > 7 || MathF.Abs(b.X) > 13)
        {
            var inside = MathF.Abs(b.X) <= 7.12f && MathF.Abs(b.Z) <= 3.87f;
            var playerStruckLast = !_opponentHit || _playerHit && _side < 0;
            var score = inside ? side > 0 && _crossed : !playerStruckLast;
            Point(score);
        }
    }

    private void Point(bool player)
    {
        if (player) Home++; else Away++;
        _rally = Rally.Reset;
        _age = 0;
    }

    private void Animated(string pose)
    {
        if (_animation == null || _pose == pose) return;
        var clip = _animation.GetAnimationList().FirstOrDefault(c => c.EndsWith(pose,StringComparison.Ordinal));
        if (clip == null) return;
        _pose = pose;
        _animation.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
        _animation.Play(clip,.17);
    }

    public static MeshInstance3D BallSkin()
    {
        var color = new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = .66f };
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        surface.SetMaterial(color);
        Color[] colors = { new("fff2d7"),new("e8aa4f"),new("327388") };
        const int lon = 36, lat = 20;
        for (var y=0;y<lat;y++)
        for (var x=0;x<lon;x++)
        {
            var band=(x/6+(y>=10?2:0))%3;
            var seam= x%6==0 ? .96f : 1;
            Add(x,y); Add(x+1,y); Add(x+1,y+1);
            Add(x,y); Add(x+1,y+1); Add(x,y+1);
            void Add(int a,int b)
            {
                var u=a*Mathf.Tau/lon;
                var v=b*Mathf.Pi/lat;
                var normal=new Vector3(Mathf.Sin(v)*Mathf.Cos(u), Mathf.Cos(v),Mathf.Sin(v)*Mathf.Sin(u));
                surface.SetNormal(normal);
                surface.SetColor(colors[band].Darkened(seam < 1 ? .08f : 0));
                surface.AddVertex(normal*.135f);
            }
        }
        color.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        return new MeshInstance3D { Mesh=surface.Commit() };
    }
}
