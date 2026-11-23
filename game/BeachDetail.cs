using Godot;

public partial class BeachDetail : Node3D
{
    private readonly List<Node3D> _fish = new();
    private Vector3 _shoal;
    private readonly RandomNumberGenerator _rng = new() { Seed = 840271 };
    private readonly Material _timber = WorldArt.Paint("8e7659");
    private readonly Material _paint = WorldArt.Paint("eae3ce");
    private readonly Material _teal = WorldArt.Paint("2b7773");
    private readonly Material _red = WorldArt.Paint("bd5d44");
    private readonly Material _steel = WorldArt.Paint("465653",.62f,.2f);
    private readonly StandardMaterial3D _batch = new() { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness=.95f };

    public void Build(float coast,float z,Vector3 kiosk,Vector3 tower)
    {
        Boardwalk(coast,z,kiosk);
        Patio(kiosk);
        LifeguardKit(tower);
        Pier(coast,z - 63);
        PlantBanks(coast,z);
        Scatter(coast,z);
        WaterBed(coast,z);
        Shoal(coast,z);
        for (int i=-2;i<=2;i++)
        {
            var corner=new Vector3(coast-30.2f,-.052f,z+31+i*28.5f);
            if (Mathf.Abs(corner.Z-(z-17))<12) continue;
            Lamp(corner);
        }
    }

    private void Boardwalk(float coast,float z,Vector3 kiosk)
    {
        var wood = new DetailMesh(_batch);
        var iron = new DetailMesh(_batch);
        var posts = new DetailMesh(_batch);
        var planks = new[] { new Color("b89971"),new Color("c4ad88"),new Color("ab8b66"),new Color("bf9e79"),new Color("ad9778") };
        for (int i=-235;i<238;i++)
        {
            var k=z+i*.33f;
            var end=Mathf.Abs(k-kiosk.Z)<5.1f ? 5.55f : 4.4f;
            wood.Box(new Vector3(coast-30.7f,-.060f,k),new Vector3(end,.042f,.314f),planks[_rng.RandiRange(0,planks.Length-1)]);
            if (i%3==0)
                foreach (var x in new[]{coast-32.16f,coast-29.31f})
                    iron.Box(new Vector3(x,-.033f,k),new Vector3(.041f,.012f,.040f),new Color("6e685b"));
        }
        for (int i=-15;i<=15;i++)
        {
            var k=z+i*4.8f;
            if (MathF.Abs(k-(z-17))<11||MathF.Abs(k-kiosk.Z)<6||MathF.Abs(k-(z-63))<3.2f) continue;
            var at=new Vector3(coast-33.36f,-.14f,k);
            posts.Box(at+Vector3.Up*.46f,new Vector3(.14f,.92f,.13f),new Color("948366"));
            posts.Box(at+Vector3.Up*.932f,new Vector3(.174f,.046f,.16f),new Color("c4ab81"));
            if (i>=15 || MathF.Abs(k+4.8f-(z-17))<11 || MathF.Abs(k+4.8f-kiosk.Z)<6) continue;
            posts.Box(at+new Vector3(0,.69f,2.4f),new Vector3(.083f,.076f,4.8f),new Color("92795d"));
            posts.Box(at+new Vector3(0,.38f,2.4f),new Vector3(.055f,.052f,4.8f),new Color("a58b69"));
        }
        wood.Finish(this,"Boardwalk planks",215);
        iron.Finish(this,"Boardwalk fasteners",42);
        posts.Finish(this,"Walkway fencing",210);
        for(var i=0;i<5;i++)
        {
            var at = new Vector3(coast-31.8f,-.045f,z + (i-2)*27 + 12);
            if(Mathf.Abs(at.Z-z+17)<10) continue;
            Bench(at);
        }
    }

    private void Bench(Vector3 at)
    {
        for (var i=0;i<4;i++)
        {
            WorldArt.Box(this,at+new Vector3((i-1.5f)*.115f,.48f,0),new Vector3(.097f,.065f,1.62f),_timber);
            WorldArt.Box(this,at+new Vector3(-.245f-i*.016f,.62f+i*.083f,0),new Vector3(.062f,.069f,1.62f),_timber);
        }
        foreach(var z in new[]{-.59f,.59f})
        {
            WorldArt.Rod(this,at+new Vector3(.21f,.02f,z),at+new Vector3(.16f,.51f,z),.031f,.029f,_steel);
            WorldArt.Rod(this,at+new Vector3(-.24f,.02f,z),at+new Vector3(-.30f,.90f,z),.031f,.029f,_steel);
            WorldArt.Rod(this,at+new Vector3(.19f,.50f,z),at+new Vector3(-.29f,.56f,z),.029f,.029f,_steel);
        }
    }

    private void Patio(Vector3 kiosk)
    {
        var crates=new DetailMesh(_batch);
        var sign=kiosk+new Vector3(1.624f,1.75f,-.92f);
        WorldArt.Box(this,sign+Vector3.Up*.17f,new Vector3(.032f,.49f,.61f),_paint);
        WorldArt.Sign(this,"ЛИМОН / МЯТА\nСО ЛЬДОМ",sign+new Vector3(.033f,.17f,0),Vector3.Right,.00121f,"236764");
        for(var i=0;i<7;i++)
        {
            var cup=new Node3D{Position=kiosk+new Vector3(1.54f,1.14f,(i-3)*.16f+.39f), Scale=Vector3.One*.80f};
            AddChild(cup); PlayerBody.Lemonade(cup);
        }
        for(var i=0;i<3;i++)
        {
            var at=kiosk+new Vector3(-1.37f+i*.62f,.20f,1.98f);
            crates.Box(at,new Vector3(.52f,.40f,.49f),new Color("a47549"));
            for(var band=-1;band<=1;band++)
                crates.Box(at+new Vector3(0,band*.122f,.257f),new Vector3(.53f,.106f,.033f),new Color("c09255"));
            for(var n=0;n<5;n++)
                WorldArt.Ball(this,at+new Vector3(_rng.RandfRange(-.17f,.17f),.215f,_rng.RandfRange(-.17f,.17f)),
                    new Vector3(.075f,.064f,.066f),WorldArt.Paint(n%2==0?"deab42":"bfb65d"),10);
        }
        crates.Finish(this,"Fruit crates",110);

        var pergola=kiosk+new Vector3(-2.1f,.08f,5.7f);
        var shade = new DetailMesh(_batch);
        var cloth=new StandardMaterial3D { VertexColorUseAsAlbedo=true, VertexColorIsSrgb=true, CullMode=BaseMaterial3D.CullModeEnum.Disabled, Roughness=.94f };
        var sail = new DetailMesh(cloth);
        var linen=new Color("e9cc90");
        for(int i=-1;i<=1;i+=2)
        for(int j=-1;j<=1;j+=2)
        {
            var foot=pergola+new Vector3(i*2.25f,0,j*2.57f);
            shade.Box(foot+Vector3.Up*1.38f,new Vector3(.164f,2.76f,.164f),new Color("948060"));
            shade.Box(foot+Vector3.Up*.15f,new Vector3(.222f,.24f,.222f),new Color("8a9283"));
        }
        for(int i=-8;i<=8;i++)
        {
            var a=pergola+new Vector3(i*.264f,2.76f,-2.69f);
            shade.Box(a+Vector3.Back*2.69f,new Vector3(.07f,.131f,5.4f),new Color("a68c61"));
            if(i==8)continue;
            for(int j=0;j<8;j++)
            {
                var left=a+new Vector3(.039f,-.096f+Mathf.Abs(j-4)*.018f,j*.671f);
                var next=left+new Vector3(0, .018f*Mathf.Sign(j-3.5f),.671f);
                sail.Quad(left,left+Vector3.Right*.192f,next+Vector3.Right*.192f,next,Vector3.Up,linen.Lightened(i%2==0?.12f:0));
            }
        }
        shade.Finish(this,"Cafe pergola",150);
        sail.Finish(this,"Canvas roof",130);
        for(int i=-1;i<=1;i+=2)
        {
            var at=pergola+Vector3.Back*i*1.25f;
            WorldArt.Rod(this,at,at+Vector3.Up*.76f,.40f,.043f,_steel,14);
            WorldArt.Rod(this,at+Vector3.Up*.70f,at+Vector3.Up*.77f,.57f,.57f,_timber,20);
            Bench(at+Vector3.Right*1.04f);
            var drink=new Node3D { Position=at+new Vector3(.22f,.77f,.11f) }; AddChild(drink); PlayerBody.Lemonade(drink);
        }

        var shower=kiosk+new Vector3(4.45f,.015f,12.15f);
        WorldArt.Box(this,shower+Vector3.Down*.014f,new Vector3(1.8f,.09f,1.55f),WorldArt.Paint("8a9687"));
        WorldArt.Rod(this,shower-Vector3.Right*.54f,shower+new Vector3(-.54f,2.29f,0),.045f,.042f,_steel);
        WorldArt.Rod(this,shower+new Vector3(-.54f,2.29f,0),shower+new Vector3(.12f,2.28f,0),.037f,.025f,_steel);
        WorldArt.Ball(this,shower+new Vector3(.11f,2.25f,0),new Vector3(.24f,.092f,.22f),_steel);
        WorldArt.Sign(this,"ДУШ",shower+new Vector3(-.49f,1.66f,0),Vector3.Right,.0021f,"305a5b");
    }

    private void LifeguardKit(Vector3 at)
    {
        var ring=WorldArt.Mesh(this,new TorusMesh{InnerRadius=.185f,OuterRadius=.272f,Rings=23,RingSegments=12},at+new Vector3(.941f,2.17f,1.01f),_red);
        ring.Rotation = new Vector3(0,0,Mathf.Pi*.5f);
        var bin=at+new Vector3(-3.33f,.49f,-1.56f);
        WorldArt.Rod(this,bin-Vector3.Up*.47f,bin+Vector3.Up*.39f,.33f,.37f,_teal,14);
        WorldArt.Rod(this,bin+Vector3.Up*.39f,bin+Vector3.Up*.45f,.377f,.35f,_paint,14);
        WorldArt.Ball(this,at+new Vector3(-1.63f,.54f,-2.35f),new Vector3(.20f,1.45f,.50f),_red,14).Rotation = new Vector3(.26f,0,.24f);
    }

    private void Pier(float coast,float z)
    {
        var edge=HarborWater.Shore(coast,z);
        var deck=new DetailMesh(_batch);
        var piles=new DetailMesh(_batch);
        for(int i=0;i<98;i++)
        {
            var center=new Vector3(edge-7.2f+i*.288f,.128f,z);
            deck.Box(center,new Vector3(.271f,.10f,3.26f),new Color("a78965").Lightened(_rng.RandfRange(0,.18f)));
        }
        for(int i=0;i<9;i++)
        for(int side=-1;side<=1;side+=2)
        {
            var at=new Vector3(edge-6.8f+i*3.31f,-.94f,z+side*1.46f);
            piles.Box(at,new Vector3(.22f,2.03f,.22f),new Color("736d56"));
            piles.Box(at+Vector3.Up*1.55f,new Vector3(.14f,1.15f,.14f),new Color("9c8664"));
            if(i<8) piles.Box(at+new Vector3(1.65f,1.94f,0),new Vector3(3.31f,.082f,.102f),new Color("b79e76"));
        }
        deck.Finish(this,"Jetty decking",210); piles.Finish(this,"Jetty structure",225);
        WorldArt.Solid(this,new Vector3(edge+6.66f,.087f,z),new Vector3(28.14f,.185f,3.21f));
        var ramp=new Node3D { Position=new Vector3(edge-8.98f,-.025f,z), Rotation=new Vector3(0,0,.105f) };
        AddChild(ramp);
        WorldArt.Box(ramp,Vector3.Zero,new Vector3(3.9f,.13f,3.24f),_timber);
        WorldArt.Solid(ramp,Vector3.Zero,new Vector3(3.9f,.13f,3.20f));
        WorldArt.Sign(this,"SALT BEACH PIER",new Vector3(edge-6.59f,1.22f,z+1.65f),Vector3.Left,.0021f);
    }

    private void Lamp(Vector3 at)
    {
        var head=at+new Vector3(.58f,3.38f,0);
        WorldArt.Rod(this,at,at+Vector3.Up*3.43f,.088f,.049f,_steel);
        WorldArt.Rod(this,at+Vector3.Up*3.43f,head,.042f,.033f,_steel);
        WorldArt.Ball(this,head,new Vector3(.52f,.18f,.28f),_steel,12);
        WorldArt.Ball(this,head+Vector3.Down*.071f,new Vector3(.39f,.075f,.20f),_paint,12);
    }

    private void PlantBanks(float coast,float z)
    {
        var leafMat=new StandardMaterial3D { VertexColorUseAsAlbedo=true, VertexColorIsSrgb=true, CullMode=BaseMaterial3D.CullModeEnum.Disabled, Roughness=.95f };
        var grass=new DetailMesh(leafMat);
        var stone=new DetailMesh(_batch);
        for(int i=0;i<112;i++)
        {
            var dz=_rng.RandfRange(-95,115);
            var strip=Mathf.Abs(dz)>9? _rng.RandfRange(-28,-24.2f) : _rng.RandfRange(-29,-27);
            if (Mathf.Abs(dz+17)<9 || Mathf.Abs(dz-8)<7 || Mathf.Abs(dz-77)<12) continue;
            var at=new Vector3(coast+strip,-.169f,z+dz);
            for(int j=0;j<15;j++)
            {
                var angle=_rng.Randf()*Mathf.Tau;
                var d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var axis=d.Cross(Vector3.Up);
                var h=_rng.RandfRange(.14f,.65f);
                var spread=_rng.RandfRange(.10f,.33f);
                var root=at+d*.096f;
                var tip=root+d*spread+Vector3.Up*h;
                var mid=root+d*spread*.34f+Vector3.Up*h*.61f;
                var color=new Color(j%4==0?"af934f":j%3==0?"687652":"7e8956").Lightened(_rng.RandfRange(0,.18f));
                grass.Quad(root-axis*.018f,root+axis*.018f,mid+axis*.010f,mid-axis*.010f,axis,color);
                grass.Triangle(mid-axis*.010f,mid+axis*.010f,tip,axis,color.Lightened(.13f));
            }
            if(i%5==0)
                stone.Box(at + new Vector3(.18f,.024f,.24f),
                    new Vector3(.23f,.047f,.14f),new Color("a7a28b"));
        }
        grass.Finish(this,"Sea oats and dune grass",135);
        stone.Finish(this,"Beach pebbles",60);
    }

    private void Scatter(float coast,float z)
    {
        var sand=new DetailMesh(_batch);
        var near=new DetailMesh(_batch);
        for(int i=0;i<290;i++)
        {
            var dz=_rng.RandfRange(-83,90);
            var edge=HarborWater.Shore(coast,z+dz);
            var x=edge-_rng.RandfRange(1.0f,29.0f);
            var depth=Mathf.Clamp((x-(coast-32))/(edge-(coast-32)),0,1);
            var at=new Vector3(x,-.14f-.12f*depth+.010f,z+dz);
            if(i%4==0)
            {
                var width=_rng.RandfRange(.019f,.058f);
                near.Box(at,new Vector3(width,.009f,width*.7f),new Color(i%3==0?"f2ddbb":"c1a083"));
            }
            else
            {
                var radius=_rng.RandfRange(.04f,.145f);
                sand.Triangle(at+new Vector3(-radius,.002f,-radius*.4f),at+new Vector3(radius,.002f,0),
                    at+new Vector3(-radius*.5f,.002f,radius*.55f),Vector3.Up,new Color(i%2==0?"b6a082":"c0a787"));
            }
        }
        sand.Finish(this,"Sand detail",36); near.Finish(this,"Shell fragments",29);
    }

    private void Shoal(float coast,float z)
    {
        _shoal = new Vector3(HarborWater.Shore(coast,z+31)+23,-1.85f,z+34);
        var scales=WorldArt.Paint("92aebb",.34f,.33f);
        var finMaterial=new StandardMaterial3D { VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true,
            CullMode=BaseMaterial3D.CullModeEnum.Disabled,Roughness=.57f };
        for(int i=0;i<13;i++)
        {
            var fish=new Node3D { Scale=Vector3.One*_rng.RandfRange(.78f,1.24f) };
            AddChild(fish);
            _fish.Add(fish);
            var fin=new DetailMesh(finMaterial);
            var silver=new Color("728e93");
            fin.Triangle(new Vector3(0,.004f,.092f),new Vector3(0,.061f,.164f),new Vector3(0,-.042f,.171f),Vector3.Right,silver);
            fin.Triangle(new Vector3(0,.02f,-.035f),new Vector3(0,.05f,.046f),new Vector3(0,.018f,.048f),Vector3.Right,silver.Lightened(.27f));
            fin.Finish(fish,"Fins",85);
            WorldArt.Ball(fish,Vector3.Zero,new Vector3(.036f,.052f,.223f),scales,10)
                .CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        }
        WaterLife(0,false);
    }

    public void WaterLife(float time,bool fleeing)
    {
        for(int i=0;i<_fish.Count;i++)
        {
            var phase=i*.47f+time*(fleeing?.96f:.49f);
            var u=i*.945f;
            var fish=_fish[i];
            fish.Position = _shoal+new Vector3(Mathf.Cos(phase)*(3.4f+.25f*u),Mathf.Sin(u+phase*.55f)*.42f,
                Mathf.Sin(phase)*(4.0f+.29f*u));
            fish.Rotation=new Vector3(0,Mathf.Atan2(Mathf.Sin(phase),-Mathf.Cos(phase))+.11f*Mathf.Sin(time*13+u),0);
            fish.Visible=!fleeing;
        }
    }

    private void WaterBed(float coast,float z)
    {
        for(int i=0;i<124;i++)
        {
            var atZ=z+(i<72?_rng.RandfRange(16,61):_rng.RandfRange(-47,100));
            var offset=i<72?_rng.RandfRange(12,36):_rng.RandfRange(6.4f,48);
            var atX=HarborWater.Shore(coast,atZ)+offset;
            var baseY=offset<=32 ? Mathf.Lerp(-.58f,-5.4f,(offset-5)/27) : -5.4f-(offset-32)*.048f;
            var pos=new Vector3(atX,baseY,atZ);
            var size=_rng.RandfRange(.21f,1.2f);
            WorldArt.Ball(this,pos,new Vector3(size,size*.65f,size*.72f),WorldArt.Paint(i%4==0?"747763":"686e5b"),9);
            if(i%3==0)
                for (int a=-1;a<=1;a++)
                    WorldArt.Rod(this,pos+Vector3.Back*a*.045f,pos+new Vector3(.10f,.37f+size*.42f,a*.15f),.017f,.004f,
                        WorldArt.Paint("526e56"),5);
        }
    }
}
