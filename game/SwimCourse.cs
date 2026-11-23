using Godot;

public partial class SwimCourse : Node3D
{
    private readonly List<Node3D> _gates = new();
    private readonly List<MeshInstance3D> _lamps = new();
    private readonly Material _teal = WorldArt.Paint("1c857f", .38f, .15f);
    private readonly Material _gold = WorldArt.Paint("f6b752", .43f, .13f);
    private readonly Material _white = WorldArt.Paint("f4ead1", .44f);
    private float _time;
    private int _next;
    public Vector3 Start { get; private set; }
    public bool Active { get; private set; }
    public bool Complete { get; private set; }
    public float Best { get; private set; }
    public string Status => Active ? $"Заплыв   {_next}/4   ·   {_time:0.0} с" : Complete ? $"Лучший заплыв   {Best:0.0} с" : "";
    public Vector3 Target => Active ? _gates[_next].GlobalPosition : Start;

    public void Build(float coast, float z)
    {
        var shore = HarborWater.Shore(coast,z);
        Start = new Vector3(shore - 6.7f, -.19f,z);
        var frame = WorldArt.Paint("306c6c");
        for (int side=-1;side<=1;side+=2)
            WorldArt.Rod(this,Start+Vector3.Back*side*.68f,Start+new Vector3(0,1.53f,side*.68f),.033f,.033f,frame);
        WorldArt.Box(this,Start+Vector3.Up*1.16f,new Vector3(.064f,.65f,1.55f),frame);
        WorldArt.Sign(this,"ЗАПЛЫВ\nЧЕТЫРЕ БУЯ",Start+new Vector3(-.04f,1.19f,0),Vector3.Left,.00195f);
        WorldArt.Sign(this,"SWIM CIRCUIT",Start+new Vector3(.04f,1.19f,0),Vector3.Right,.00216f);
        Vector2[] points = { new(14,-3),new(30,8),new(26,25),new(12,27) };
        for (int i=0;i<points.Length;i++)
        {
            var p=points[i]; var gate=new Node3D { Position=new Vector3(shore+p.X,-.38f,z+p.Y) };
            _gates.Add(gate); AddChild(gate);
            WorldArt.Ball(gate,Vector3.Zero,new Vector3(.62f,.51f,.62f),_teal,16);
            WorldArt.Rod(gate,Vector3.Up*.10f,Vector3.Up*.43f,.24f,.16f,_white,16);
            _lamps.Add(WorldArt.Ball(gate,Vector3.Up*.49f,new Vector3(.22f,.21f,.22f),_teal));
            WorldArt.Sign(gate,(i+1).ToString(),new Vector3(-.166f,.295f,0),Vector3.Left,.00265f,"145d67");
        }
    }

    public void Toggle()
    {
        Active = !Active;
        _time=0; _next=0;
    }

    public void Stop() => Active=false;

    public void Step(HarborPlayer player, HarborWater water, float dt)
    {
        for (int i=0;i<_gates.Count;i++)
        {
            var gate=_gates[i];
            gate.Position = new Vector3(gate.Position.X, water.Sample(gate.GlobalPosition).Surface, gate.Position.Z);
            _lamps[i].MaterialOverride=Active && i==_next ? _gold : _teal;
        }
        if (!Active) return;
        _time+=dt;
        var d=player.GlobalPosition-Target;
        if (!player.Swimming || new Vector2(d.X,d.Z).Length()>2.45f) return;
        _next++;
        if (_next<_gates.Count) return;
        Active=false;
        Complete=true;
        if (Best<=0 || _time<Best) Best=_time;
    }
}
