using Godot;

public partial class EvacuationRoad : Node3D
{
    public const float RefugeX = -3375;
    private const float DistrictLength = 154;
    private const float BlockDepth = 137;
    private readonly RandomNumberGenerator _random = new() { Seed = 5910 };
    private readonly List<Vector3> _route = new();
    private readonly List<float> _distance = new();
    private readonly Material _asphalt = WorldArt.Paint("454a49");
    private readonly Material _stripe = WorldArt.Paint("d8c26f");
    private readonly Material _shoulder = WorldArt.Paint("b6b6a7");
    private float _westernEdge;
    private float _centralZ;
    private float _centralSlope;
    public Vector3 CarSpawn { get; private set; }
    public Vector3 CarDirection { get; private set; }
    public Vector3 Checkpoint { get; private set; }
    public Vector3 Pad { get; private set; }
    public float TotalLength => _distance[^1];
    public IReadOnlyList<Vector3> Route => _route;

    public void Build(Vector3[] downtown, float coast, FloodDamage damage, Node3D city)
    {
        _westernEdge = downtown[^1].X;
        _centralZ = downtown[^1].Z;
        _centralSlope = (downtown[^1].Z - downtown[^2].Z) / (downtown[^1].X - downtown[^2].X);
        var inlet = downtown[0];
        var inward = (downtown[3] - inlet).Normalized();
        CarDirection = inward;
        var bay = new Vector3(coast - 41, .02f, inlet.Z + 7.9f);
        WorldArt.Box(this, bay + Vector3.Down * .095f, new Vector3(17.3f, .015f, 14), _asphalt);
        WorldArt.Solid(this, bay + Vector3.Down * .22f, new Vector3(17.3f, .28f, 14));
        for (var bayMark = -3; bayMark <= 3; bayMark++)
            WorldArt.Box(this, bay + new Vector3(bayMark * 2.32f, -.08f, 3.8f), new Vector3(.069f, .013f, 4.74f), _shoulder);
        WorldArt.Sign(this, "P  /  SALT BEACH", bay + new Vector3(5.8f, 2.32f, 6), Vector3.Right, .0042f);
        var first = new Vector3(coast - 38.0f, 0, inlet.Z - (coast - 38.0f - inlet.X) * inward.Z / -inward.X);
        var connector = new List<Vector3> { first, inlet };
        Ribbon(this, connector, 15f, -.049f, _asphalt, true);
        CarSpawn = first + (inlet - first) * .24f + inward.Cross(Vector3.Up) * 2.3f + Vector3.Up * .92f;
        _route.Add(first);
        _route.AddRange(downtown);
        var extension = new List<Vector3> { downtown[^1] };
        for (var x = _westernEdge - 11; x > RefugeX - 22; x -= 11)
            extension.Add(new Vector3(x, 0, MainZ(x)));
        extension.Add(new Vector3(RefugeX - 32, 0, MainZ(RefugeX - 32)));
        _route.AddRange(extension.Skip(1));
        Ribbon(this, extension, 14.6f, -.045f, _asphalt, true);
        MarkRoad(this, extension);
        for (var n = 0; n < _route.Count; n++)
            _distance.Add(n == 0 ? 0 : _distance[n - 1] + _route[n].DistanceTo(_route[n - 1]));

        var districts = Mathf.FloorToInt((_westernEdge - RefugeX - 110) / DistrictLength);
        for (var column = 0; column < districts; column++)
            District(column, damage, city);
        for (var part = 1; part < districts; part += 3)
        {
            var x = _westernEdge - DistrictLength * part;
            RoadSign(x, MainZ(x), part == 1 ? "WEST  /  PINE RIDGE" : $"PINE RIDGE  /  {Mathf.RoundToInt((x - RefugeX) / 100) * 100} M");
        }

        Checkpoint = new Vector3(RefugeX + 30, 0, MainZ(RefugeX + 30));
        Pad = new Vector3(RefugeX, -.134f, MainZ(RefugeX) + 18);
        RescueSite();
    }

    private float MainZ(float x)
    {
        var d = _westernEdge - x;
        return _centralZ - _centralSlope * d * Mathf.Exp(-d / 65) +
            28 * Mathf.Sin(d * .0021f) * Mathf.SmoothStep(0, 250, d) + 18 * Mathf.Sin(d * .0069f);
    }

    public (Vector3 Target, float Remaining, float Offset) Navigation(Vector3 p)
    {
        var flat = new Vector2(p.X, p.Z);
        var nearest = 0;
        var offset = float.MaxValue;
        for (var i = 0; i < _route.Count; i++)
        {
            var gap = flat.DistanceSquaredTo(new Vector2(_route[i].X, _route[i].Z));
            if (gap >= offset) continue;
            offset = gap;
            nearest = i;
        }
        var goal = nearest;
        while (goal < _route.Count - 1 && _distance[goal] < _distance[nearest] + 40) goal++;
        return (_route[goal], Mathf.Max(0, TotalLength - _distance[nearest] - 95), Mathf.Sqrt(offset));
    }

    private static void Ribbon(Node3D parent, IReadOnlyList<Vector3> points, float width, float y, Material material, bool collision)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.TriangleStrip);
        surface.SetMaterial(material);
        for (var i = 0; i < points.Count; i++)
        {
            var direction = (points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(0, i - 1)]).Normalized();
            var side = direction.Cross(Vector3.Up);
            var at = new Vector3(points[i].X, y, points[i].Z);
            surface.SetNormal(Vector3.Up);
            surface.AddVertex(at + side * width * .5f);
            surface.SetNormal(Vector3.Up);
            surface.AddVertex(at - side * width * .5f);
        }
        var road = WorldArt.Mesh(parent, surface.Commit(), Vector3.Zero, null);
        road.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        if (collision)
        {
            // Triangulate explicitly for the collision shape because the render mesh is a strip.
            var faces = new List<Vector3>();
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1] + Vector3.Up * y;
                var b = points[i] + Vector3.Up * y;
                var side = (b - a).Normalized().Cross(Vector3.Up) * width * .5f;
                faces.AddRange(new[] { a - side, a + side, b - side, b - side, a + side, b + side });
            }
            var body = new StaticBody3D { CollisionMask = 0 };
            var shape = new ConcavePolygonShape3D { BackfaceCollision = true };
            shape.SetFaces(faces.ToArray());
            body.AddChild(new CollisionShape3D { Shape = shape });
            parent.AddChild(body);
        }
    }

    private void MarkRoad(Node3D parent, List<Vector3> extension)
    {
        var transforms = new List<Transform3D>();
        for (var i = 1; i < extension.Count - 1; i++)
        {
            var a = extension[i];
            var b = extension[i + 1];
            var at = a.Lerp(b, .45f) + Vector3.Up * -.034f;
            var axis = Basis.LookingAt((b - a).Normalized(), Vector3.Up);
            transforms.Add(new Transform3D(axis.ScaledLocal(new Vector3(.135f, .012f, 4.2f)), at));
        }
        BatchBoxes(parent, _stripe, transforms);
        for (var side = -1; side <= 1; side += 2)
        {
            var edge = extension.Select((point, i) => point + (extension[Math.Min(i + 1, extension.Count - 1)]
                - extension[Math.Max(0, i - 1)]).Normalized().Cross(Vector3.Up) * (side * 6.4f)).ToArray();
            Ribbon(parent, edge, .11f, -.032f, _shoulder, false);
        }
    }

    private static void BatchBoxes(Node3D parent, Material material, List<Transform3D> stamps)
    {
        if (stamps.Count == 0) return;
        var batch = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = Vector3.One, Material = material }, InstanceCount = stamps.Count };
        for (var i = 0; i < stamps.Count; i++) batch.SetInstanceTransform(i, stamps[i]);
        parent.AddChild(new MultiMeshInstance3D { Multimesh = batch, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    private void District(int column, FloodDamage damage, Node3D city)
    {
        var district = new Node3D { Name = $"WestDistrict{column + 1}" };
        AddChild(district);
        var east = _westernEdge - DistrictLength * column - 16;
        var x = east - DistrictLength*.5f;
        var central = MainZ(x);
        var green = WorldArt.Paint(column % 4 == 0 ? "707654" : "626f51");
        var planted = new List<Vector3>();
        var heights = new List<float>();
        var concrete = new DetailMesh(new StandardMaterial3D { VertexColorUseAsAlbedo=true, VertexColorIsSrgb=true, Roughness=.93f });
        WorldArt.Box(district, new Vector3(x,-.166f,_centralZ), new Vector3(DistrictLength,.079f,1100),green)
            .CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        WorldArt.Box(district, new Vector3(east,-.089f,_centralZ), new Vector3(9.3f,.054f,1093),_asphalt)
            .CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        var walkway=new Color("aaa591");
        for (var row=-4;row<=4;row++)
        {
            var roadZ=_centralZ+row*BlockDepth;
            if(row==0) continue;
            WorldArt.Box(district,new Vector3(x,-.092f,roadZ),new Vector3(DistrictLength,.05f,9.1f),_asphalt)
                .CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            for(int side=-1;side<=1;side+=2)
                concrete.Box(new Vector3(x,-.08f,roadZ+side*5.37f),new Vector3(DistrictLength,.072f,1.65f),walkway);
        }
        for (var row=-4;row<4;row++)
        {
            var shoreRow=Math.Abs(row + .5f)<1f;
            var z = shoreRow ? central+Mathf.Sign(row+.5f)*38 : _centralZ+(row+.5f)*BlockDepth;
            var kind = (column%6<2 && Math.Abs(row)<3) ? 2 : shoreRow && column%5!=4 ? 1 : 0;
            var vacant = !shoreRow && column>2 && _random.Randf()<.17f;
            if (!vacant)
            {
                var slots = kind==0 && !shoreRow ? 3 : 2;
                for (var lot=0;lot<slots;lot++)
                {
                    var at = new Vector3(x+(lot-(slots-1)*.5f)*(slots==3?39:64),0,z+_random.RandfRange(-4,4));
                    var size = kind switch {
                        2 => new Vector3(_random.RandfRange(31,47),_random.RandfRange(6.7f,11.0f),_random.RandfRange(20,27)),
                        1 => new Vector3(_random.RandfRange(20,34),_random.RandfRange(8.8f,16.1f),_random.RandfRange(14,20)),
                        _ => new Vector3(_random.RandfRange(11,17),_random.RandfRange(4.7f,6.5f),_random.RandfRange(10,14)),
                    };
                    Building(district,at,size,kind,row<0,damage);
                    NeighborhoodArchitecture.Lot(concrete, at, slots==3?32:53, size.Z+13,kind,column);
                    var nearMain = at+new Vector3(_random.RandfRange(-8,8),-.127f,Mathf.Sign(central-z)*(size.Z*.5f+7.2f));
                    planted.Add(nearMain); heights.Add(_random.RandfRange(5.1f,8.8f));
                }
            }
            for (var i=0;i<4;i++)
            {
                planted.Add(new Vector3(x+(i-1.5f)*26+_random.RandfRange(-5,5),-.12f,z+Mathf.Sign(z-central)*_random.RandfRange(16,31)));
                heights.Add(_random.RandfRange(5.0f,10.6f));
            }
        }
        for (int side=-1;side<=1;side+=2)
        {
            var paths=new List<Vector3>();
            for(int q=0;q<=16;q++)
            {
                var along=east-7-q*8.6f;
                paths.Add(new Vector3(along,0,MainZ(along)+side*10.91f));
            }
            Ribbon(district,paths,2.48f,.018f,WorldArt.Paint("a69d84"),false);
            var verge=paths.Select(point=>point+Vector3.Back*(side*.29f)).ToArray();
            Ribbon(district,verge,.11f,.023f,WorldArt.Paint("d0bd9c"),false);
            for(int d=0;d<3;d++)
            {
                var crossing=east-30-d*45.0f;
                var at=new Vector3(crossing,-.076f,MainZ(crossing)+side*18.55f);
                concrete.Box(at,new Vector3(d==1?4.5f:7.0f,.092f,16.92f),new Color("837f70"));
                concrete.Box(at+Vector3.Right*2.61f,new Vector3(.55f,.079f,16.85f),new Color("b1a38a"));
            }
        }
        for(int side=-1;side<=1;side+=2)
        for(int j=0;j<4;j++)
        {
            var lampX = east-21.5f-j*37.70f;
            var at=new Vector3(lampX,-.12f,MainZ(lampX)+side*9.90f);
            concrete.Box(at+Vector3.Up*2.56f,new Vector3(.115f,5.27f,.117f),new Color("606e69"));
            concrete.Box(at+new Vector3(0,5.09f,-side*.74f),new Vector3(.075f,.074f,1.56f),new Color("687774"));
            concrete.Box(at+new Vector3(0,5.00f,-side*1.42f),new Vector3(.332f,.16f,.55f),new Color("e0cea5"));
            if(j%2==1)
            {
                var walkX=lampX+10;
                planted.Add(new Vector3(walkX,-.139f,MainZ(walkX)+side*14.8f));
                heights.Add(_random.RandfRange(5.5f,8.8f));
            }
        }
        concrete.Finish(district,"Sidewalks, yards and street lighting",490);
        city.Call("game_district_trees", district, new Vector3(x,0,_centralZ), planted.ToArray(), heights.ToArray(), column);
    }

    private void Building(Node3D district, Vector3 at, Vector3 size, int kind, bool faceRoad, FloodDamage damage)
    {
        var building = new Node3D { Position = at, Rotation = new Vector3(0,faceRoad?0:Mathf.Pi,0) };
        district.AddChild(building);
        string[] colors = kind == 2 ? new[] { "a98367", "9a9888", "8d9e98", "b5aa92" } :
            kind == 1 ? new[] { "a98571","b3a68a","a8a49d","7e948f","b4a297","ae9a86" } :
            new[] { "c2baa1","a88d76","b5b19a","b0a796","93a19a","b9897c" };
        var tint = new Color(colors[_random.RandiRange(0, colors.Length-1)]);
        NeighborhoodArchitecture.Building(building,size,kind,tint,_random);
        WorldArt.Solid(building, new Vector3(0,size.Y*.5f-.095f,0),size);
        damage.Register(building, size, building.Basis, tint);
    }

    private void RoadSign(float x, float z, string title)
    {
        var post = new Vector3(x, 0, z - 9.7f);
        var teal = WorldArt.Paint("275e59");
        for (var side = -1; side <= 1; side += 2)
            WorldArt.Rod(this, post + Vector3.Back * side * 1.76f, post + new Vector3(0, 3.2f, side * 1.76f),
                .045f, .045f, _shoulder, 6);
        WorldArt.Box(this, post + Vector3.Up * 2.65f, new Vector3(.13f, 1.09f, 4.16f), teal);
        WorldArt.Sign(this, title, post + new Vector3(.08f, 2.65f, 0), Vector3.Right, .0051f);
    }

    private void RescueSite()
    {
        RoadSign(Checkpoint.X + 30, Checkpoint.Z, "RESCUE  /  PINE RIDGE");
        var tile = new Vector3(Pad.X + 14, Pad.Y - .07f, (Pad.Z + Checkpoint.Z) * .5f);
        WorldArt.Box(this, tile, new Vector3(54, .14f, 52), _asphalt);
        var white = WorldArt.Paint("ece8cf");
        var hazard = WorldArt.Paint("c19d52");
        for (var side = -1; side <= 1; side += 2)
        {
            WorldArt.Box(this, Pad + new Vector3(side * 1.25f, .010f, 0), new Vector3(.43f, .015f, 3.65f), white);
            WorldArt.Box(this, Pad + new Vector3(side * 7.2f, .010f, 0), new Vector3(.18f, .014f, 14.3f), hazard);
            WorldArt.Box(this, Pad + new Vector3(0, .010f, side * 7.2f), new Vector3(14.4f, .014f, .18f), hazard);
        }
        WorldArt.Box(this, Pad + Vector3.Up * .010f, new Vector3(2.48f, .014f, .39f), white);
        var edge = WorldArt.Paint("a0a791");
        for (var i = 0; i < 9; i++)
        {
            var at = Pad + new Vector3(-14, -.06f, -34 + i * 8);
            WorldArt.Box(this, at + Vector3.Up * .6f, new Vector3(.24f, 1.22f, .24f), edge);
            WorldArt.Box(this, at + new Vector3(0, 1.04f, 3.86f), new Vector3(.19f, .15f, 7.72f), edge);
        }
    }
}
