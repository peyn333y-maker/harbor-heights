using Godot;

public partial class PlayerBody : Node3D
{
    private readonly MeshInstance3D[] _thigh = new MeshInstance3D[2];
    private readonly MeshInstance3D[] _shin = new MeshInstance3D[2];
    private readonly MeshInstance3D[] _upper = new MeshInstance3D[2];
    private readonly MeshInstance3D[] _fore = new MeshInstance3D[2];
    private readonly MeshInstance3D[] _shoes = new MeshInstance3D[2];
    private readonly Node3D[] _hands = new Node3D[2];
    private MeshInstance3D _torso = null!;
    private MeshInstance3D _waist = null!;
    private Node3D _cup = null!;

    public void Build()
    {
        var cotton = WorldArt.Paint("5a9b99", .98f);
        var shorts = WorldArt.Paint("514a46", .91f);
        var skin = WorldArt.Paint("d29a74", .73f);
        _torso = WorldArt.Ball(this, new Vector3(0, 1.19f, .092f), new Vector3(.365f,.54f,.25f), cotton, 14);
        _waist = WorldArt.Ball(this, new Vector3(0, .91f, .076f), new Vector3(.34f,.23f,.245f), shorts);
        for (var i = 0; i < 2; i++)
        {
            _thigh[i] = Segment(shorts, .082f, .104f);
            _shin[i] = Segment(skin, .047f, .072f);
            _upper[i] = Segment(cotton, .049f, .072f);
            _fore[i] = Segment(skin, .033f, .049f);
            _shoes[i] = WorldArt.Ball(this, Vector3.Zero, new Vector3(.109f,.105f,.244f), WorldArt.Paint("263535"), 14);
            WorldArt.Box(_shoes[i], new Vector3(0,-.27f,0), new Vector3(.87f,.19f,.87f), WorldArt.Paint("d0cabc"));
            _hands[i] = new Node3D();
            AddChild(_hands[i]);
            Hand(_hands[i], i == 0 ? -1 : 1, skin);
        }
        _cup = new Node3D { Position = new Vector3(0,.055f,-.024f), Visible = false };
        _hands[1].AddChild(_cup);
        Lemonade(_cup);
    }

    public static void Lemonade(Node3D parent)
    {
        var paper = WorldArt.Paint("f4e6bf");
        var orange = WorldArt.Paint("ecab4b");
        WorldArt.Rod(parent, Vector3.Zero, Vector3.Up * .139f, .040f,.053f, paper, 18);
        WorldArt.Rod(parent, Vector3.Up * .037f, Vector3.Up * .094f, .044f,.050f, orange, 18);
        WorldArt.Rod(parent, Vector3.Up * .138f, Vector3.Up * .149f, .057f,.057f, paper, 18);
        WorldArt.Rod(parent, new Vector3(.012f,.147f,0), new Vector3(.028f,.206f,-.018f), .005f,.005f, WorldArt.Paint("cc5e48"), 8);
    }

    public static void Hand(Node3D parent, int side, Material skin)
    {
        WorldArt.Ball(parent, new Vector3(0,0,-.029f), new Vector3(.074f,.043f,.094f), skin, 12);
        for (var f = 0; f < 4; f++)
        {
            var x = (f - 1.5f) * .0175f;
            var end = -.096f - .009f * Mathf.Sin(f * 1.1f);
            WorldArt.Rod(parent, new Vector3(x,0,-.052f), new Vector3(x,-.011f,end), .0093f,.0075f, skin, 8);
            WorldArt.Ball(parent, new Vector3(x,-.012f,end), Vector3.One * .015f, skin, 8);
        }
        WorldArt.Rod(parent, new Vector3(-side*.032f,0,-.023f), new Vector3(-side*.049f,-.011f,-.060f), .013f,.010f, skin, 8);
    }

    public void Pose(float step, float pace, bool swim, bool rest, float swimPhase, Camera3D eye, bool cup, float sip, float reach)
    {
        var move = Mathf.Clamp(pace / 6f, 0, 1);
        var view = eye.Position;
        _torso.Position = new Vector3(0, swim ? .91f : rest ? .64f : 1.17f, .105f);
        _torso.Rotation = new Vector3(swim ? -.92f : rest ? .34f : 0,0,0);
        _waist.Position = new Vector3(0, swim ? .79f : rest ? .36f : .92f, .09f);
        for (var i = 0; i < 2; i++)
        {
            var side = i == 0 ? -1 : 1;
            var stride = Mathf.Sin(step + i * Mathf.Pi);
            var hip = new Vector3(side*.088f, _waist.Position.Y, .067f);
            var foot = new Vector3(side*.105f, .055f + move * Mathf.Max(0,stride)*.12f, -.064f - stride * .27f * move);
            var knee = hip.Lerp(foot,.5f) + Vector3.Forward * (.054f + move*Mathf.Max(stride,0)*.115f);
            if (rest)
            {
                knee = new Vector3(side*.11f,.33f,-.27f);
                foot = new Vector3(side*.13f,.14f,-.61f);
            }
            if (swim)
            {
                knee = new Vector3(side*.097f,.66f,.39f);
                foot = new Vector3(side*.107f,.66f + stride*.10f,.72f);
            }
            Join(_thigh[i], hip, knee);
            Join(_shin[i], knee, foot);
            _shoes[i].Position = foot + (swim ? Vector3.Back*.085f : Vector3.Forward*.069f);
            _shoes[i].Rotation = new Vector3(swim ? 1.05f : stride*move*.26f,0,0);

            var shoulder = new Vector3(side*.19f, swim ? 1.10f : rest ? .75f : 1.38f, .045f);
            var wrist = new Vector3(side*.225f, rest ? .37f : .91f + .19f*move, -.14f + stride*.24f*move);
            var elbow = shoulder.Lerp(wrist,.53f) + new Vector3(side*.057f,0,.063f);
            if (swim)
            {
                var phase = swimPhase + i*Mathf.Pi;
                wrist = new Vector3(side*(.27f + .17f*Mathf.Sin(phase)), 1.16f + .27f*Mathf.Cos(phase), -.41f - .34f*Mathf.Sin(phase));
                elbow = shoulder.Lerp(wrist,.53f) + new Vector3(side*.15f,.09f,.065f);
            }
            if (reach > 0 && i == 0)
            {
                wrist = wrist.Lerp(view + eye.Basis * new Vector3(-.08f,-.16f,-.52f), reach);
                elbow = shoulder.Lerp(wrist,.51f) + new Vector3(-.055f,0,.06f);
            }
            if (cup && i == 1)
            {
                var holding = view + eye.Basis * new Vector3(.23f,-.35f,-.36f);
                var drinking = view + eye.Basis * new Vector3(.09f,-.18f,-.155f);
                wrist = holding.Lerp(drinking, sip);
                elbow = shoulder.Lerp(wrist,.54f) + new Vector3(.089f,-.12f,.028f);
            }
            Join(_upper[i], shoulder, elbow);
            Join(_fore[i], elbow, wrist);
            _hands[i].Position = wrist;
            _hands[i].Rotation = cup && i == 1 ? new Vector3(-sip*.41f,0,-.04f) :
                new Vector3(swim ? -.12f : -.87f + move*stride*.33f, side*.13f, 0);
        }
        _cup.Visible = cup;
    }

    private MeshInstance3D Segment(Material material, float distal, float proximal) =>
        WorldArt.Mesh(this, new CylinderMesh { TopRadius = distal, BottomRadius = proximal,
            Height = 1, RadialSegments = 12, Rings = 2 }, Vector3.Zero, material);

    private static void Join(MeshInstance3D mesh, Vector3 a, Vector3 b)
    {
        var offset = b - a;
        mesh.Position = (a + b) * .5f;
        mesh.Quaternion = new Quaternion(Vector3.Up, offset.Normalized());
        mesh.Scale = new Vector3(1, offset.Length() + .026f, 1);
    }
}
