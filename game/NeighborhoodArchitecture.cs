using Godot;

internal static class NeighborhoodArchitecture
{
    private static readonly StandardMaterial3D Masonry = new() { VertexColorUseAsAlbedo=true, VertexColorIsSrgb=true, Roughness=.91f };
    private static readonly StandardMaterial3D Glass = new() { VertexColorUseAsAlbedo=true, VertexColorIsSrgb=true,
        Roughness=.24f, Metallic=.24f };
    private static readonly Color Eaves=new("d1c5a8"), Timber=new("7f6c56"), Lintel=new("847f71"), Blinds=new("758f8d");
    private static readonly Color Iron=new("4e5651"), Road=new("646762"), Pavement=new("aaa48c");
    private static readonly Color[] GlassColors={new("385961"),new("4e676d"),new("4b5e63"),new("5e7371")};

    public static void Building(Node3D node,Vector3 size,int kind,Color tint,RandomNumberGenerator rng)
    {
        var core = new DetailMesh(Masonry);
        var detail = new DetailMesh(Masonry);
        var windows = new DetailMesh(Glass);
        core.Box(new Vector3(0,size.Y*.5f-.08f,0),size,tint);
        core.Box(new Vector3(0,.18f,0),new Vector3(size.X+.16f,.42f,size.Z+.16f),tint.Darkened(.28f));
        if (kind==0) House(core,detail,windows,size,tint,rng);
        else if (kind==1) Rowhouse(core,detail,windows,size,tint,rng);
        else Warehouse(core,detail,windows,size,tint,rng);
        core.Finish(node,"Massing and roof");
        detail.Finish(node,"Porches, joints and frames",305);
        windows.Finish(node,"Recessed glazing",445);
    }

    private static void House(DetailMesh core,DetailMesh details,DetailMesh windows,Vector3 s,Color wall,RandomNumberGenerator rng)
    {
        var top=s.Y-.035f;
        var roofTone=new Color(rng.Randf()<.48f?"70554a":"485154");
        Gable(core,Vector3.Zero,s.X+.91f,s.Z+.94f,top,.238f*s.Z,roofTone);
        var asym=rng.RandfRange(-.23f,.23f)*s.X;
        Gable(core,new Vector3(asym,0,s.Z*.33f),s.X*.37f,s.Z*.52f,s.Y*.65f,s.Z*.11f,roofTone.Lightened(.06f));
        var chimney=new Vector3(s.X*.30f,top+s.Z*.18f,-s.Z*.198f);
        core.Box(chimney,new Vector3(.86f,1.78f,.81f),wall.Darkened(.22f));
        core.Box(chimney+Vector3.Up*.91f,new Vector3(1.03f,.18f,.95f),Lintel);
        for (var i=0;i<3;i++)
        {
            var x=(i-1)*s.X*.29f;
            Window(details,windows,new Vector3(x,top*.68f,s.Z*.5f),Vector3.Back,s.X*.134f,top*.265f,i);
            Window(details,windows,new Vector3(x,top*.63f,-s.Z*.5f),Vector3.Forward,s.X*.134f,top*.226f,2-i);
            if(i!=1) Window(details,windows,new Vector3(x,top*.29f,s.Z*.5f),Vector3.Back,s.X*.145f,top*.25f,i+1);
            if(i>0)
            {
                var z=(i-1.5f)*s.Z*.51f;
                Window(details,windows,new Vector3(s.X*.5f,top*.63f,z),Vector3.Right,s.Z*.15f,top*.23f,i);
                Window(details,windows,new Vector3(-s.X*.5f,top*.63f,z),Vector3.Left,s.Z*.15f,top*.23f,i+1);
            }
        }
        var entry = new Vector3(asym*.23f,1.14f,s.Z*.5f+.058f);
        details.Box(entry,new Vector3(1.34f,2.17f,.08f),Eaves);
        details.Box(entry+Vector3.Back*.055f,new Vector3(1.13f,2.0f,.045f),new Color("486260"));
        details.Box(entry+new Vector3(.42f,-.13f,.092f),new Vector3(.091f,.055f,.036f),new Color("ba9e6d"));
        core.Box(new Vector3(0,.17f,s.Z*.5f+.76f),new Vector3(s.X*.68f,.38f,1.62f),Timber);
        core.Box(new Vector3(0,2.70f,s.Z*.5f+.79f),new Vector3(s.X*.70f,.18f,1.73f),roofTone);
        details.Box(new Vector3(0,2.59f,s.Z*.5f+1.64f),new Vector3(s.X*.70f,.13f,.07f),Eaves);
        for(var i=-1;i<=1;i++)
            details.Box(new Vector3(i*s.X*.30f,1.41f,s.Z*.5f+1.51f),new Vector3(.16f,2.47f,.15f),Eaves);
        core.Box(new Vector3(0,.056f,s.Z*.5f+1.88f),new Vector3(1.90f,.13f,.71f),Lintel);
        for (int side=-1;side<=1;side+=2)
        for (int i=0;i<5;i++)
        {
            var x=side*(1.17f+i*s.X*.044f);
            details.Box(new Vector3(x,.67f,s.Z*.5f+1.55f),new Vector3(.059f,.73f,.064f),Eaves);
        }
        for (int i=1;i<Mathf.FloorToInt(s.Y/.31f);i++)
        {
            details.Box(new Vector3(0,i*.31f,-s.Z*.5f-.014f),new Vector3(s.X,.024f,.034f),wall.Darkened(.20f));
            details.Box(new Vector3(s.X*.5f+.015f,i*.31f,0),new Vector3(.034f,.029f,s.Z),wall.Darkened(.19f));
            details.Box(new Vector3(-s.X*.5f-.015f,i*.31f,0),new Vector3(.034f,.026f,s.Z),wall.Darkened(.19f));
        }
    }

    private static void Rowhouse(DetailMesh core,DetailMesh details,DetailMesh glass,Vector3 s,Color tint,RandomNumberGenerator rng)
    {
        var stories=Mathf.Clamp(Mathf.RoundToInt(s.Y/3.15f),2,5);
        var pitch=s.Y/stories;
        var roof=new Color("62615b");
        var trim=tint.Lightened(.24f);
        core.Box(Vector3.Up*s.Y,new Vector3(s.X+.43f,.47f,s.Z+.43f),trim);
        core.Box(Vector3.Up*(s.Y+.22f),new Vector3(s.X-.68f,.105f,s.Z-.69f),roof);
        core.Box(new Vector3(-s.X*.23f,s.Y+.62f,s.Z*.12f),new Vector3(s.X*.22f,1.11f,s.Z*.31f),tint.Darkened(.22f));
        core.Box(new Vector3(s.X*.27f,s.Y+.78f,-s.Z*.21f),new Vector3(s.X*.30f,1.42f,s.Z*.32f),roof);
        // Each block can use an offset entrance wing, an enclosed corner bay or a setback terrace.
        var projection = rng.RandfRange(1.1f,2.45f);
        var offset = rng.RandfRange(-.30f,-.17f)*s.X;
        if (rng.Randf()<.5f) offset=-offset;
        var wingWidth=s.X*rng.RandfRange(.18f,.28f);
        var wingHeight=s.Y*rng.RandfRange(.74f,.93f);
        var bayFront=s.Z*.5f+projection;
        core.Box(new Vector3(offset,wingHeight*.5f,s.Z*.5f+projection*.44f),
            new Vector3(wingWidth+.41f,wingHeight,projection*1.12f),tint.Lightened(.09f));
        core.Box(new Vector3(offset,wingHeight,bayFront-projection*.51f),
            new Vector3(wingWidth+.86f,.24f,projection+.43f),trim);
        core.Box(new Vector3(-offset*.43f,s.Y+.70f,-s.Z*.16f),
            new Vector3(s.X*.36f,1.22f,s.Z*.33f),tint.Darkened(.11f));
        for(int wall=0;wall<stories-1;wall++)
        {
            var level=1.25f+wall*pitch;
            Window(details,glass,new Vector3(offset,level+.38f,bayFront),Vector3.Back,wingWidth*.67f,pitch*.59f,wall+2);
            for (int side=-1;side<=1;side+=2)
                details.Box(new Vector3(offset+side*wingWidth*.50f,level+.29f,bayFront+.022f),
                    new Vector3(.128f,pitch*.92f,.153f),trim);
        }
        core.Box(new Vector3(offset,.697f,bayFront+.40f),new Vector3(wingWidth*.72f,1.27f,.94f),tint.Darkened(.24f));
        var bays=Mathf.Clamp(Mathf.RoundToInt(s.X/3.6f),4,8);
        for(int floor=0;floor<stories;floor++)
        {
            var y=1.12f + floor*pitch;
            for(int face=-1;face<=1;face+=2)
            {
                var z=s.Z*.5f*face;
                details.Box(new Vector3(0,(floor+1)*pitch-.20f,z+face*.06f),new Vector3(s.X+.15f,.20f,.24f),trim);
                for(int i=0;i<bays;i++)
                {
                    var x=((i+.5f)/bays-.5f)*s.X;
                    var width=s.X/bays;
                    if (face==1 && Mathf.Abs(x-offset) < wingWidth*.48f && y<wingHeight-.44f) continue;
                    Window(details,glass,new Vector3(x,y+.46f,z),Vector3.Back*face,width*(floor==0?.78f:.63f),pitch*.58f,i+floor);
                    if (floor>0 && (i+face)%3==0)
                    {
                        var at=new Vector3(x,y-.53f,z+face*.53f);
                        core.Box(at,new Vector3(width*.83f,.19f,1.14f),trim);
                        details.Box(at+new Vector3(0,.77f,face*.53f),new Vector3(width*.82f,.067f,.057f),Iron);
                        for(int bar=-2;bar<=2;bar++)
                            details.Box(at+new Vector3(bar*width*.196f,.43f,face*.53f),new Vector3(.045f,.72f,.05f),Iron);
                    }
                    if (floor==0 && face==1 && i%2==0)
                    {
                        var color=new Color(i%4==0?"446e6b":"af6c52");
                        var a=new Vector3(x-width*.44f,y+1.2f,z+.03f);
                        details.Quad(a,a+Vector3.Right*width*.88f,a+new Vector3(width*.88f,-.2f,1.08f),a+new Vector3(0,-.2f,1.08f),
                            Vector3.Up,color);
                        details.Box(a+new Vector3(width*.44f,-.27f,1.08f),new Vector3(width*.89f,.17f,.076f),color);
                    }
                }
            }
            for(int face=-1;face<=1;face+=2)
            for(int i=-1;i<=1;i++)
                Window(details,glass,new Vector3(face*s.X*.5f,y+.42f,i*s.Z*.28f),Vector3.Right*face,s.Z*.139f,pitch*.54f,i+floor+4);
        }
        core.Box(new Vector3(0,.13f,s.Z*.5f+.88f),new Vector3(s.X+.45f,.21f,1.75f),Pavement);
        for(int i=-2;i<=2;i++)
        {
            var x=i*s.X*.195f;
            if (Mathf.Abs(x-offset)<wingWidth*.58f) continue;
            details.Box(new Vector3(x,.285f,s.Z*.5f+3.89f),new Vector3(1.53f,.58f,.59f),Lintel);
            details.Box(new Vector3(x,.576f,s.Z*.5f+3.89f),new Vector3(1.41f,.084f,.53f),new Color("617a50"));
        }
    }

    private static void Warehouse(DetailMesh core,DetailMesh details,DetailMesh glass,Vector3 s,Color tint,RandomNumberGenerator rng)
    {
        var roofColor=new Color("686c63");
        var fascia=new Color("5f6a62");
        var ridge=s.Z*.12f;
        Gable(core,Vector3.Zero,s.X+.68f,s.Z+.72f,s.Y,ridge,roofColor);
        core.Box(new Vector3(0,s.Y-.34f,s.Z*.5f+.11f),new Vector3(s.X+.82f,.78f,.42f),fascia);
        core.Box(new Vector3(s.X*.37f,2.20f,s.Z*.5f+1.02f),new Vector3(s.X*.24f,4.40f,2.15f),tint.Lightened(.21f));
        var loading = s.Z*.5f+.74f;
        core.Box(new Vector3(-s.X*.18f,.34f,loading),new Vector3(s.X*.63f,.57f,1.38f),Lintel);
        for(int q=0;q<4;q++)
        {
            var x=(q-1.5f)*s.X*.151f-s.X*.15f;
            var door=s.X*.117f;
            details.Box(new Vector3(x,1.85f,s.Z*.5f+.05f),new Vector3(door+.22f,3.20f,.13f),Iron);
            details.Box(new Vector3(x,1.85f,s.Z*.5f+.128f),new Vector3(door,3.02f,.073f),new Color("a39c87"));
            for(int slat=0;slat<10;slat++)
                details.Box(new Vector3(x,.47f+slat*.304f,s.Z*.5f+.15f),new Vector3(door,.035f,.04f),new Color("77776a"));
            var fasciaPos=new Vector3(x,4.01f,s.Z*.5f+.77f);
            core.Box(fasciaPos,new Vector3(door+.41f,.16f,1.58f),fascia);
        }
        for(int i=-3;i<=3;i++)
        for(int face=-1;face<=1;face+=2)
        {
            Window(details,glass,new Vector3(i*s.X*.13f,s.Y*.79f,s.Z*.5f*face),Vector3.Back*face,s.X*.09f,s.Y*.16f,i+6);
            core.Box(new Vector3(i*s.X*.135f,s.Y*.53f,-s.Z*.5f-.04f),new Vector3(.19f,s.Y,.25f),tint.Darkened(.20f));
        }
        for(int i=-1;i<=1;i++)
        {
            var head=new Vector3(i*s.X*.22f,s.Y+ridge*.83f,.18f);
            core.Box(head,new Vector3(1.69f,.63f,1.51f),Lintel);
            for(int grate=0;grate<4;grate++)
                details.Box(head+new Vector3(0,.34f,(grate-1.5f)*.276f),new Vector3(1.35f,.052f,.104f),Iron);
        }
        for(int side=-1;side<=1;side+=2)
        for(int i=-1;i<=1;i++)
            Window(details,glass,new Vector3(side*s.X*.5f,s.Y*.67f,i*s.Z*.26f),Vector3.Right*side,s.Z*.14f,s.Y*.19f,i+6);
    }

    private static void Window(DetailMesh mesh,DetailMesh glass,Vector3 at,Vector3 facing,float w,float h,int style)
    {
        var fence=Vector3.Up.Cross(facing);
        var frame=new Color(style%3==0?"b8ab92":"d0c4ab");
        var shell=Along(w+.225f,h+.19f,.083f,facing);
        mesh.Box(at+facing*.022f,shell,frame);
        glass.Box(at+facing*.078f,Along(w,h,.035f,facing),GlassColors[Math.Abs(style)%GlassColors.Length]);
        mesh.Box(at+facing*.11f,Along(.052f,h,.046f,facing),frame.Darkened(.32f));
        mesh.Box(at+facing*.11f+Vector3.Up*h*.18f,Along(w,.047f,.043f,facing),frame.Darkened(.24f));
        mesh.Box(at+facing*.104f-Vector3.Up*(h*.5f+.07f),Along(w+.37f,.11f,.23f,facing),Lintel);
        if(style%4==1)
        {
            for(int i=0;i<3;i++)
                mesh.Box(at+fence*w*.254f+Vector3.Up*(h*.35f-i*h*.091f)+facing*.101f,Along(w*.48f,.034f,.035f,facing),Blinds);
        }
    }

    private static Vector3 Along(float w,float h,float depth,Vector3 n)=>
        Mathf.Abs(n.X)>.5f ? new Vector3(depth,h,w) : new Vector3(w,h,depth);

    private static void Gable(DetailMesh mesh,Vector3 center,float x,float z,float top,float rise,Color color)
    {
        var e=new Vector3(-x*.5f,top,-z*.5f)+center;
        var f=new Vector3(x*.5f,top,-z*.5f)+center;
        var g=new Vector3(-x*.5f,top,z*.5f)+center;
        var h=new Vector3(x*.5f,top,z*.5f)+center;
        var a=new Vector3(-x*.5f,top+rise,0)+center;
        var b=new Vector3(x*.5f,top+rise,0)+center;
        mesh.Quad(e,f,b,a,new Vector3(0,z*.5f,-rise).Normalized(),color);
        mesh.Quad(a,b,h,g,new Vector3(0,z*.5f,rise).Normalized(),color.Lightened(.06f));
        mesh.Triangle(e,a,g,Vector3.Left,color.Darkened(.13f));
        mesh.Triangle(f,b,h,Vector3.Right,color.Darkened(.13f));
        mesh.Box(new Vector3(0,top-.051f,0)+center,new Vector3(x+.104f,.16f,z+.13f),color.Darkened(.16f));
        mesh.Box(new Vector3(0,top+rise+.025f,0)+center,new Vector3(x+.056f,.073f,.179f),color.Lightened(.13f));
    }

    public static void Lot(DetailMesh mesh,Vector3 at,float width,float depth,int kind,int column)
    {
        var face=at.Z;
        var stone = kind==2 ? Road : Pavement;
        mesh.Box(at+new Vector3(0,-.091f,0),new Vector3(width,.065f,depth),stone);
        mesh.Box(at+new Vector3(0,-.072f,-depth*.5f-.83f),new Vector3(width,.079f,1.25f),Pavement.Lightened(.13f));
        if(kind>0)
        {
            for(int i=-6;i<=6;i++)
                mesh.Box(new Vector3(at.X+i*width*.067f,-.047f,face-depth*.29f),new Vector3(.083f,.011f,3.81f),new Color("d5c9a7"));
        }
        else
        {
            for (int i=-4;i<=4;i++)
                mesh.Box(new Vector3(at.X+i*width*.101f,.21f,face-depth*.50f),new Vector3(.061f,.66f,.076f),Eaves);
            mesh.Box(new Vector3(at.X,.437f,face-depth*.5f),new Vector3(width*.86f,.058f,.056f),Eaves);
            mesh.Box(new Vector3(at.X,.12f,face-depth*.5f),new Vector3(width*.86f,.067f,.061f),Eaves);
        }
        if(column%3==0)
            mesh.Box(at+new Vector3(width*.45f,.41f,depth*.42f),new Vector3(.75f,.91f,1.21f),Iron);
    }
}
