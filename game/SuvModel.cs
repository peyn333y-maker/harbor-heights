using Godot;

public partial class SuvModel : Node3D
{
    private readonly StandardMaterial3D _paint = new() { VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true,
        Metallic=.44f, Roughness=.41f, ClearcoatEnabled=true,Clearcoat=.47f, ClearcoatRoughness=.28f };
    private readonly StandardMaterial3D _flat = new() { VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true,Roughness=.82f };
    private readonly StandardMaterial3D _glass = new() { VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true,
        AlbedoColor=new Color(.55f,.76f,.78f,.15f), Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode=BaseMaterial3D.CullModeEnum.Disabled, Roughness=.25f,Metallic=.02f };
    private readonly Material _rubber = WorldArt.Paint("232d2c",.99f);
    private readonly Material _plastic = WorldArt.Paint("293537",.87f);
    private readonly Material _fabric = WorldArt.Paint("777e72",.96f);
    private readonly Material _leather = WorldArt.Paint("3c4746",.77f);
    private readonly Material _silver = WorldArt.Paint("969e93",.39f,.59f);
    private readonly Color _body = new("32766e");
    private const float RoofY=1.393f;
    private Node3D _wheel=null!;
    private Node3D _driver=null!;
    private Label3D _speed=null!;
    private readonly MeshInstance3D[] _arms=new MeshInstance3D[2];
    private int _speedDigits=-1;

    public Camera3D Cockpit { get; private set; } = null!;

    public void Build()
    {
        Skin();
        Greenhouse();
        FrontAndRear();
        Seating();
    }

    private Vector3 Profile(float z,int section,int side)
    {
        var taper=Mathf.SmoothStep(1.68f,2.15f,MathF.Abs(z));
        var arch=Arch(z);
        var w=1.022f - taper*.16f;
        var top=.536f - taper*.109f;
        return section switch
        {
            0=>new Vector3(side*(w-.107f), -.225f + arch + taper*.094f, z),
            1=>new Vector3(side*w, Mathf.Max(-.115f+arch,.025f) + taper*.07f, z),
            2=>new Vector3(side*(w-.013f),top-.086f,z),
            3=>new Vector3(side*(w-.089f),top,z),
            4=>new Vector3(side*w*.51f,top+.023f,z),
            _=>new Vector3(0f,top+.034f,z),
        };
    }

    private static float Arch(float z)
    {
        var d=Mathf.Min(Mathf.Abs(z+1.22f),Mathf.Abs(z-1.05f));
        return d < .535f ? .423f*Mathf.Sqrt(Mathf.Max(0,1-d*d/(.535f*.535f))) : 0;
    }

    private void Skin()
    {
        List<float> ring=new() {-2.17f,-2.12f,-2.04f,2.06f,2.12f,2.17f};
        for(int i=0;i<=48;i++) ring.Add(Mathf.Lerp(-2.0f,2.0f,i/48f));
        ring.Sort();
        var panels=new DetailMesh(_paint);
        var trim=new DetailMesh(_flat);
        var seal=new Color("283b38");
        var dark=new Color("2c3636");
        for(int side=-1;side<=1;side+=2)
        for(int k=1;k<ring.Count;k++)
        {
            var a=ring[k-1];var b=ring[k];
            for(int section=0;section<5;section++)
            {
                var p=Profile(a,section,side); var q=Profile(b,section,side);
                var r=Profile(b,section+1,side); var s=Profile(a,section+1,side);
                var n=(s-p).Cross(q-p).Normalized();
                if(n.Dot(new Vector3(side*.5f, .4f,0))<0)n=-n;
                if (section==0)
                {
                    var ledge = new Vector3(side*.026f,0,0);
                    trim.Quad(p,q,r+ledge,s+ledge,new Vector3(side,.04f,0).Normalized(),dark);
                }
                else
                    panels.Quad(p,q,r,s,n,_body.Lightened(section>=3?.04f:0));
            }
        }
        panels.Finish(this,"Painted body and hood");
        trim.Finish(this,"Wheel-arch guards");
        WorldArt.Box(this,new Vector3(0,-.223f,.06f),new Vector3(.97f,.217f,3.18f),_plastic);
        foreach(var z in new[]{-1.22f,1.05f})
        {
            WorldArt.Rod(this,new Vector3(-.91f,-.399f,z),new Vector3(.91f,-.399f,z),.054f,.054f,_plastic,11);
            WorldArt.Ball(this,new Vector3(0,-.390f,z),new Vector3(.31f,.296f,.32f),_plastic,12);
            for(int side=-1;side<=1;side+=2)
                WorldArt.Rod(this,new Vector3(side*.87f,-.365f,z),new Vector3(side*.73f,.086f,z+.015f),.058f,.044f,_silver,9);
        }
        for(int side=-1;side<=1;side+=2)
        {
            var door=new DetailMesh(_flat);
            for(int i=0;i<3;i++)
            {
                var z=new[]{-.84f,.17f,1.06f}[i];
                var x=1.015f*side;
                door.Box(new Vector3(x,.335f,z),new Vector3(.012f,.285f,.018f),seal);
                if(i<2)
                {
                    var handle=new Vector3(x+side*.011f,.427f,z+.19f);
                    door.Box(handle,new Vector3(.023f,.051f,.252f),seal);
                    door.Box(handle+Vector3.Right*side*.013f,new Vector3(.026f,.025f,.192f),new Color("68847c"));
                }
            }
            door.Box(new Vector3(side*1.012f,.201f,-.075f),new Vector3(.024f,.05f,1.58f),seal);
            door.Box(new Vector3(side*.99f,-.170f,-.065f),new Vector3(.093f,.080f,1.31f),dark);
            door.Finish(this,"Door joints and handles");
            var mirror=new Vector3(side*1.129f,.692f,-.711f);
            WorldArt.Rod(this,new Vector3(side*.974f,.623f,-.743f),mirror,.038f,.043f,_plastic);
            WorldArt.Ball(this,mirror,new Vector3(.359f,.158f,.234f),WorldArt.Paint("35665f",.4f,.38f),14);
            WorldArt.Ball(this,mirror+Vector3.Back*.088f,new Vector3(.296f,.116f,.076f),_silver,14);
        }
    }

    private void Greenhouse()
    {
        var paint=new DetailMesh(_paint);
        var window=new DetailMesh(_glass);
        var roof = WorldArt.Paint("354747",.51f,.25f);
        var frontFoot = new Vector3(.967f,.555f,-.968f);
        var frontTop = new Vector3(.799f,RoofY-.085f,-.389f);
        var rearFoot = new Vector3(.957f,.551f,1.72f);
        var rearTop = new Vector3(.812f,RoofY-.09f,1.40f);
        Vector3 Mirror(Vector3 p)=> new(-p.X,p.Y,p.Z);
        window.Quad(frontFoot,Mirror(frontFoot),Mirror(frontTop),frontTop,new Vector3(0,.52f,-.854f),Colors.White);
        window.Quad(rearFoot,Mirror(rearFoot),Mirror(rearTop),rearTop,new Vector3(0,.39f,.92f),Colors.White);
        for(int side=-1;side<=1;side+=2)
        {
            var a=side<0?Mirror(frontFoot):frontFoot;
            var b=side<0?Mirror(frontTop):frontTop;
            var c=side<0?Mirror(rearFoot):rearFoot;
            var d=side<0?Mirror(rearTop):rearTop;
            window.Quad(a,c,d,b,new Vector3(side,.22f,0).Normalized(),Colors.White);
            WorldArt.Rod(this,a,b,.056f,.048f,WorldArt.Paint("3d7970",.53f,.28f),9);
            WorldArt.Rod(this,c,d,.075f,.054f,WorldArt.Paint("3d7970",.53f,.28f),9);
            for(int i=1;i<3;i++)
            {
                var slot=i==1?.435f:.745f;
                WorldArt.Rod(this,a.Lerp(c,slot),b.Lerp(d,slot),.047f,.043f,_plastic,8);
            }
            WorldArt.Rod(this,a,c,.036f,.043f,_plastic,8);
            WorldArt.Rod(this,b,d,.040f,.048f,roof,8);
            var rack1 = new Vector3(side*.64f,RoofY+.08f,-.27f);
            var rack2 = new Vector3(side*.64f,RoofY+.08f,1.25f);
            WorldArt.Rod(this,rack1,rack2,.032f,.032f,_plastic,9);
            WorldArt.Rod(this,rack1-Vector3.Up*.09f,rack1,.062f,.046f,_plastic,9);
            WorldArt.Rod(this,rack2-Vector3.Up*.09f,rack2,.062f,.046f,_plastic,9);
        }
        var f=new Vector3(.801f,RoofY-.064f,-.41f);
        var r=new Vector3(.810f,RoofY-.069f,1.41f);
        var ft=new Vector3(.721f,RoofY,-.325f);
        var rt=new Vector3(.718f,RoofY,1.335f);
        paint.Quad(ft,rt,Mirror(rt),Mirror(ft),Vector3.Up,_body.Darkened(.25f));
        paint.Quad(f,r,rt,ft,new Vector3(.4f,.91f,0).Normalized(),_body);
        paint.Quad(Mirror(f),Mirror(r),Mirror(rt),Mirror(ft),new Vector3(-.4f,.91f,0).Normalized(),_body);
        paint.Quad(f,ft,Mirror(ft),Mirror(f),new Vector3(0,.7f,-.7f),_body);
        paint.Quad(r,rt,Mirror(rt),Mirror(r),new Vector3(0,.7f,.7f),_body);
        paint.Finish(this,"Roof skin");
        WorldArt.Box(this,new Vector3(0,RoofY-.098f,.55f),new Vector3(1.58f,.038f,1.81f),roof);
        WorldArt.Rod(this,frontFoot,Mirror(frontFoot),.035f,.035f,_plastic,8);
        WorldArt.Rod(this,frontTop,Mirror(frontTop),.044f,.044f,_plastic,8);
        WorldArt.Rod(this,rearTop,Mirror(rearTop),.039f,.039f,_plastic,8);
        window.Finish(this,"Windshield, rear and door glass");
        var glass=GetNode<MeshInstance3D>("Windshield, rear and door glass");
        glass.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
    }

    private void FrontAndRear()
    {
        var painted=WorldArt.Paint("397970",.40f,.47f);
        var bumper=WorldArt.Paint("374547",.76f,.13f);
        var silver=WorldArt.Paint("889791",.42f,.51f);
        var bright=new StandardMaterial3D { AlbedoColor=new Color("d2e8db"),EmissionEnabled=true,Emission=new Color("d6e8db"),EmissionEnergyMultiplier=.35f,
            Roughness=.37f,Metallic=.16f };
        var red=new StandardMaterial3D { AlbedoColor=new Color("8f3530"),EmissionEnabled=true,Emission=new Color("ad382e"),EmissionEnergyMultiplier=.27f,
            Roughness=.41f, Metallic=.06f};
        for(int side=-1;side<=1;side+=2)
        {
            var front=new Vector3(side*.649f,.369f,-2.143f);
            var rear=new Vector3(side*.719f,.346f,2.155f);
            WorldArt.Ball(this,front,new Vector3(.387f,.128f,.112f),_plastic,16);
            WorldArt.Ball(this,front+Vector3.Forward*.034f,new Vector3(.352f,.084f,.073f),bright,16);
            WorldArt.Ball(this,rear,new Vector3(.241f,.165f,.096f),red,16);
            WorldArt.Box(this,rear+new Vector3(0,-.127f,.014f),new Vector3(.22f,.044f,.053f),bright);
        }
        WorldArt.Box(this,new Vector3(0,.321f,-2.141f),new Vector3(1.15f,.292f,.087f),painted);
        WorldArt.Box(this,new Vector3(0,.295f,-2.192f),new Vector3(.966f,.205f,.034f),_plastic);
        for(int i=-6;i<=6;i++)
            WorldArt.Box(this,new Vector3(i*.067f,.296f,-2.218f),new Vector3(.022f,.176f,.023f),silver);
        foreach(var side in new[]{-1,1})
        {
            var front=side==-1;
            WorldArt.Ball(this,new Vector3(0,.021f,side*2.128f),new Vector3(1.92f,.269f,.32f),bumper,16);
            WorldArt.Box(this,new Vector3(0,-.087f,side*2.195f),new Vector3(1.05f,.137f,.106f),silver);
            if(!front) WorldArt.Box(this,new Vector3(0,.31f,2.122f),new Vector3(1.43f,.302f,.108f),painted);
            var plate=new Vector3(0,front?.082f:.252f,side*2.304f);
            WorldArt.Box(this,plate,new Vector3(.474f,.114f,.033f),WorldArt.Paint("d8d9cb"));
            WorldArt.Sign(this,"HH  042",plate+Vector3.Back*(side*.025f),Vector3.Back*side,.00070f,"293b3d");
        }
        WorldArt.Sign(this,"T I D E L I N E",new Vector3(0,.447f,2.185f),Vector3.Back,.00065f,"c3d6c8");
    }

    private void Seating()
    {
        WorldArt.Box(this,new Vector3(0,.407f,.371f),new Vector3(1.80f,.079f,2.68f),_rubber);
        var dash=WorldArt.Ball(this,new Vector3(0,.649f,-.740f),new Vector3(1.92f,.286f,.551f),_leather,18);
        dash.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        WorldArt.Box(this,new Vector3(0,.567f,-.553f),new Vector3(1.48f,.072f,.092f),_silver);
        WorldArt.Ball(this,new Vector3(-.421f,.744f,-.596f),new Vector3(.533f,.203f,.356f),_plastic,16);
        WorldArt.Box(this,new Vector3(-.416f,.733f,-.438f),new Vector3(.308f,.095f,.049f),WorldArt.Paint("1d3737",.45f));
        _speed = WorldArt.Sign(this,"00",new Vector3(-.414f,.733f,-.408f),Vector3.Back,.00077f,"9fe4d5");
        WorldArt.Box(this,new Vector3(.081f,.676f,-.506f),new Vector3(.346f,.158f,.073f),_plastic);
        WorldArt.Box(this,new Vector3(.081f,.679f,-.461f),new Vector3(.298f,.103f,.024f),WorldArt.Paint("274244",.43f));
        WorldArt.Sign(this,"94.1 FM",new Vector3(.081f,.683f,-.444f),Vector3.Back,.00052f,"8ed5bf");
        for(int i=0;i<5;i++)
        {
            var x=.267f+i*.080f;
            WorldArt.Box(this,new Vector3(x,.686f,-.484f),new Vector3(.049f,.063f,.042f),_plastic);
        }
        for (int side=-1;side<=1;side+=2)
        {
            var x=side*.42f;
            Seat(new Vector3(x,.529f,.169f));
            WorldArt.Box(this,new Vector3(side*.936f,.625f,.058f),new Vector3(.095f,.187f,1.43f),_leather);
            WorldArt.Box(this,new Vector3(side*.879f,.624f,.077f),new Vector3(.092f,.081f,.73f),_fabric);
            WorldArt.Box(this,new Vector3(side*.879f,.679f,.17f),new Vector3(.079f,.028f,.201f),_silver);
            WorldArt.Ball(this,new Vector3(side*.932f,.495f,.044f),new Vector3(.060f,.172f,.297f),_plastic,12);
        }
        WorldArt.Ball(this,new Vector3(0,.512f,1.119f),new Vector3(1.53f,.242f,.573f),_fabric,16);
        WorldArt.Ball(this,new Vector3(0,.782f,1.399f),new Vector3(1.57f,.632f,.292f),_fabric,16);
        WorldArt.Box(this,new Vector3(0,.533f,-.073f),new Vector3(.329f,.223f,.876f),_leather);
        WorldArt.Rod(this,new Vector3(.032f,.637f,-.16f),new Vector3(.021f,.752f,-.179f),.031f,.022f,_silver);
        WorldArt.Ball(this,new Vector3(.018f,.763f,-.180f),new Vector3(.077f,.071f,.069f),_plastic,12);
        WorldArt.Box(this,new Vector3(0,1.253f,-.361f),new Vector3(.219f,.063f,.035f),_plastic);
        WorldArt.Box(this,new Vector3(0,1.253f,-.338f),new Vector3(.196f,.044f,.016f),_silver);
        var column=new Vector3(-.419f,.753f,-.386f);
        WorldArt.Rod(this,new Vector3(-.419f,.581f,-.621f),column,.050f,.054f,_plastic);
        _wheel=new Node3D { Position=column };
        AddChild(_wheel);
        WorldArt.Mesh(_wheel,new TorusMesh { InnerRadius=.133f,OuterRadius=.169f,Rings=32,RingSegments=12 },Vector3.Zero,_rubber);
        WorldArt.Box(_wheel,Vector3.Zero,new Vector3(.282f,.025f,.031f),_silver);
        WorldArt.Box(_wheel,new Vector3(0,0,.071f),new Vector3(.045f,.022f,.137f),_plastic);
        WorldArt.Ball(_wheel,Vector3.Up*.024f,new Vector3(.135f,.057f,.108f),_plastic,13);
        _driver=new Node3D { Visible=false };
        AddChild(_driver);
        var skin=WorldArt.Paint("d29a74",.73f);
        foreach(var i in new[]{0,1})
        {
            _arms[i]=WorldArt.Mesh(_driver,new CylinderMesh { TopRadius=.032f,BottomRadius=.054f,Height=1,RadialSegments=12 },Vector3.Zero,skin);
            WorldArt.Ball(_driver,new Vector3(-.42f+(i==0?-.096f:.105f),.583f,-.017f),new Vector3(.172f,.184f,.42f),
                WorldArt.Paint("514a46"),13);
        }
        for(int i=0;i<2;i++)
        {
            var direction=i==0?-1:1;
            var hand=new Node3D { Name=$"DriverHand{i}",Position=new Vector3(direction*.139f,.009f,-.035f),
                Rotation=new Vector3(0,0,-direction*.28f),Visible=false };
            _wheel.AddChild(hand);
            PlayerBody.Hand(hand,direction,skin);
        }
        Cockpit=new Camera3D { Position=new Vector3(-.418f,1.040f,.193f),Fov=75,Near=.040f,Far=12000 };
        AddChild(Cockpit);
        Update(.0f,.0f);
    }

    private void Seat(Vector3 at)
    {
        WorldArt.Ball(this,at,new Vector3(.413f,.163f,.520f),_fabric,16);
        WorldArt.Ball(this,at+new Vector3(0,.28f,.212f),new Vector3(.403f,.589f,.162f),_fabric,16)
            .Rotation=new Vector3(.18f,0,0);
        WorldArt.Ball(this,at+new Vector3(0,.584f,.271f),new Vector3(.231f,.194f,.137f),_leather,14);
        for(int side=-1;side<=1;side+=2)
            WorldArt.Ball(this,at+new Vector3(side*.163f,.041f,-.003f),new Vector3(.076f,.093f,.411f),_leather,11);
    }

    public void Driver(bool present)
    {
        _driver.Visible=present;
        for(int i=0;i<2;i++) _wheel.GetNode<Node3D>($"DriverHand{i}").Visible=present;
    }

    public void Update(float steering,float speed)
    {
        _wheel.Basis=new Basis(Vector3.Right,1.20f)*new Basis(Vector3.Up,-steering*2.35f);
        var kmh=Mathf.RoundToInt(speed*3.6f);
        if(kmh != _speedDigits)
        {
            _speedDigits=kmh;
            _speed.Text=$"{kmh:00}";
        }
        if(!_driver.Visible)return;
        for(int i=0;i<2;i++)
        {
            var side=i==0?-1:1;
            var elbow=new Vector3(-.42f+side*.238f,.622f,-.020f);
            var palm=_wheel.Position+_wheel.Basis*new Vector3(side*.139f,.009f,-.035f);
            var to=palm-elbow;
            _arms[i].Position=elbow.Lerp(palm,.5f);
            _arms[i].Quaternion=new Quaternion(Vector3.Up,to.Normalized());
            _arms[i].Scale=new Vector3(1,to.Length()+.025f,1);
        }
    }

    public static Node3D MakeTire(int side)
    {
        var root=new Node3D();
        var tire=WorldArt.Paint("222827",.93f);
        var rim=WorldArt.Paint("a3a59c",.39f,.65f);
        var hub=WorldArt.Paint("464e4b",.54f,.45f);
        void LateralRod(float from,float to,float r,Material mat)=>
            WorldArt.Rod(root,new Vector3(side*from,0,0),new Vector3(side*to,0,0),r,r,mat,24);
        var tyreMesh=WorldArt.Mesh(root,new TorusMesh { InnerRadius=.253f,OuterRadius=.438f,Rings=38,RingSegments=14 },Vector3.Zero,tire);
        tyreMesh.RotationDegrees=new Vector3(0,0,90);
        tyreMesh.Scale=new Vector3(1,.11f/.093f,1);
        LateralRod(-.094f,.17f,.276f,hub);
        LateralRod(.16f,.191f,.223f,rim);
        LateralRod(.192f,.204f,.176f,hub);
        for(int i=0;i<6;i++)
        {
            var a=i*Mathf.Tau/6;
            var spokes=new Node3D { Rotation=new Vector3(a,0,0) };
            root.AddChild(spokes);
            WorldArt.Box(spokes,new Vector3(side*.211f,.123f,0),new Vector3(.028f,.154f,.029f),rim);
            WorldArt.Ball(spokes,new Vector3(side*.234f,.063f,0),Vector3.One*.034f,rim,8);
        }
        LateralRod(.199f,.232f,.070f,rim);
        var treads=new DetailMesh(new StandardMaterial3D { VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true,Roughness=.96f });
        var c=new Color("373d39");
        for(int i=0;i<40;i++)
        {
            var a=i*Mathf.Tau/40;
            var b=a+.10f;
            var rimA=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a))*.440f;
            var rimB=new Vector3(0,Mathf.Cos(b),Mathf.Sin(b))*.440f;
            treads.Quad(rimA+Vector3.Left*.039f,rimA+Vector3.Right*.059f,
                rimB+Vector3.Right*.046f,rimB+Vector3.Left*.058f,(rimA+rimB).Normalized(),c);
        }
        treads.Finish(root,"Tread",65);
        return root;
    }
}
