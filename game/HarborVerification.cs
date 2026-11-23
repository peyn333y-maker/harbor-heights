#if DEBUG
using Godot;

public partial class HarborGame
{
    private async void RunVerification()
    {
        var mode = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--harbor-check=", StringComparison.Ordinal));
        if (mode == null) return;
        try
        {
            if (mode.EndsWith("visual", StringComparison.Ordinal)) await PreviewScene();
            else if (mode.EndsWith("beach", StringComparison.Ordinal)) await VerifyBeach();
            else if (mode.EndsWith("quick", StringComparison.Ordinal)) await VerifyQuickStart();
            else if (mode.EndsWith("loss", StringComparison.Ordinal)) await VerifyLoss();
            else await VerifyRoute(mode.EndsWith("clock", StringComparison.Ordinal));
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"Harbor verification failed: {exception}");
            GetTree().Quit(2);
        }
    }

    private async Task VerifyRoute(bool runClock)
    {
        var shortestTime = HorizontalDistance(_car.GlobalPosition, _road.Checkpoint) / EscapeCar.TopSpeed;
        Verify(shortestTime > 120, $"driving time bound {shortestTime:0.0} seconds");
        Begin(false);
        if (runClock)
        {
            while (_age < 126) await PhysicsTick();
            Verify(_storm > 0 && _stage == Stage.Beach, $"fog before any warning at {_age:0.00}s");
            while (_age < WarningAt) await PhysicsTick();
            await PhysicsTick();
            Verify(_stage == Stage.Warning, $"warning at {_age:0.00}s");
        }
        else _age = WarningAt - .02f;

        if (runClock)
        {
            var arrival = _age + 22;
            while (_player.GlobalPosition.DistanceTo(_car.GlobalPosition) > 3.2f && _age < arrival)
            {
                _player.Face((_car.GlobalPosition - _player.GlobalPosition).Slide(Vector3.Up).Normalized());
                _testInput = new Vector2(0, 1);
                await PhysicsTick();
            }
            _testInput = Vector2.Zero;
            Verify(_player.GlobalPosition.DistanceTo(_car.GlobalPosition) < 3.6f,
                $"walked from the beach to the SUV at {_age:0.00}s (player {_player.GlobalPosition}, SUV {_car.GlobalPosition})");
        }
        else
        {
            _player.GlobalPosition = _car.GlobalPosition + _car.GlobalBasis.X * 2.1f - Vector3.Up * .66f;
            _player.Resting = false;
            while (_stage == Stage.Beach) await PhysicsTick();
        }
        _interact = true;
        await PhysicsTick();
        Verify(_inCar && _car.DashboardCamera.Current, "SUV interaction and cockpit camera");
        if (!runClock) _age = DisasterAt - .35f;
        var waiting = 0;
        while (_stage == Stage.Warning)
        {
            if (runClock && waiting++ % 600 == 0)
                GD.Print($"Waiting {_age:0.0}s in SUV at {_car.GlobalPosition}, speed {_car.Speed:0.0}, up {_car.GlobalBasis.Y}");
            await PhysicsTick();
        }
        Verify(_age >= DisasterAt && _age < DisasterAt + .04f, $"tsunami triggered at {_age:0.00}s ({_stage}, SUV {_car.GlobalPosition}, submerged {_drowning:0.0}, tipped {_tipped:0.0})");
        Verify(_water.Sample(new Vector3(_water.Front + 36, 0, 0)).Surface < 6,
            "offshore surge grows after the phase transition");

        var driveTime = _age;
        var timer = 0;
        var dynamicDebris = false;
        while (_stage == Stage.Tsunami && _age - driveTime < 230)
        {
            var current = _car.GlobalPosition;
            var next = _road.Navigation(current).Target;
            var toward = (next - current).Slide(Vector3.Up).Normalized();
            var forward = -_car.GlobalBasis.Z.Slide(Vector3.Up).Normalized();
            var angle = forward.SignedAngleTo(toward, Vector3.Up);
            _testInput = new Vector2(Mathf.Clamp(-angle * 2.8f, -1, 1), Mathf.Abs(angle) > .54f && _car.Speed > 11 ? -.12f : 1);
            if (timer % 30 == 0 && !dynamicDebris)
                dynamicDebris = _damage.GetChildren().OfType<RigidBody3D>().Any(body => body.LinearVelocity.Length() > 3);
            if (++timer % 1500 == 0)
                GD.Print($"Test drive {_age - driveTime:0}s: X={current.X:0}, Z={current.Z:0}, speed={_car.Speed:0.0}, buildings fallen={_damage.Collapsed}");
            await PhysicsTick();
        }
        Verify(_stage == Stage.Rescue, $"reached rescue checkpoint in {_age - driveTime:0.0}s ({_stage}, X={_car.GlobalPosition.X:0})");
        Verify(_damage.Collapsed > 40, $"collapsed structures ({_damage.Collapsed})");
        Verify(dynamicDebris, "the flood moved rigid debris");
        _testInput = new Vector2(0, -1);
        while (_car.Speed > 1.85f) await PhysicsTick();
        _testInput = Vector2.Zero;
        _interact = true;
        await PhysicsTick();
        Verify(!_inCar && _player.Eye.Current, "walkable checkpoint after leaving the SUV");

        while (!_rescue.Landed) await PhysicsTick();
        var wait = _age;
        while (_player.GlobalPosition.DistanceTo(_rescue.Door) > 2.9f && _age - wait < 34)
        {
            _player.Face((_rescue.Door - _player.GlobalPosition).Slide(Vector3.Up).Normalized());
            _testInput = Vector2.Up * -1;
            if (++timer % 240 == 0) GD.Print($"Walking to rescue. Player {_player.GlobalPosition}, door {_rescue.Door}, on floor {_player.IsOnFloor()}.");
            await PhysicsTick();
        }
        _testInput = Vector2.Zero;
        _interact = true;
        await PhysicsTick();
        Verify(_stage == Stage.Flight && _rescue.Seat.Current, $"boarding interaction and departure camera (player {_player.GlobalPosition}, door {_rescue.Door}, stage {_stage})");
        while (_stage == Stage.Flight) await PhysicsTick();
        Verify(_stage == Stage.Won, "evacuation ending");
    }

    private async Task VerifyQuickStart()
    {
        Begin(false);
        Pause(true);
        SkipToEvacuation();
        Verify(!GetTree().Paused && _inCar && _stage == Stage.Warning && _car.DashboardCamera.Current,
            "skipping the beach from pause resumes play at the wheel");
        var start = _age;
        while (_stage == Stage.Warning && _age-start < 4) await PhysicsTick();
        Verify(_stage == Stage.Tsunami && _age-start < 2.1f, "quick start reaches the disaster in under three seconds");
    }

    private async Task VerifyLoss()
    {
        Begin(false);
        _age = DisasterAt + 14;
        _stage = Stage.Warning;
        EnterCar();
        while (_stage != Stage.Lost && _age < DisasterAt + 75) await PhysicsTick();
        Verify(_stage == Stage.Lost, $"flood failure at {_age:0.0}s ({_stage}, SUV {_car.GlobalPosition})");
        Verify(GetTree().Paused, "failure screen pauses the simulation");
    }

    private async Task VerifyBeach()
    {
        Begin(false);
        _player.Resting = false;
        await WalkToward(_beach.LemonadeStand, 2.2f, 24);
        _interact = true;
        await PhysicsTick();
        Verify(_player.Sips == 3 && _player.HaveCup, "the cafe gives the player a carried drink");
        _testSprint = true;
        await WalkToward(_beach.Swim.Start, 1.6f, 23);
        _testSprint = false;
        var tired = _player.Energy;
        _hand = true;
        for (int i = 0; i < 78; i++) await PhysicsTick();
        Verify(_player.Sips == 2 && _triedLemonade && _player.Energy > tired+.18f, "drinking replenishes sprint energy");

        _interact = true;
        await PhysicsTick();
        Verify(_beach.Swim.Active, "the swim course starts from its shore sign");
        var stop = _age + 70;
        while (_beach.Swim.Active && _age < stop)
        {
            var target = _beach.Swim.Target;
            while (_beach.Swim.Active && _beach.Swim.Target == target && _age < stop)
            {
                AimAndMove(target);
                await PhysicsTick();
            }
        }
        _testInput = Vector2.Zero;
        Verify(_beach.Swim.Complete && _stage == Stage.Beach, $"completed all four buoys in {_beach.Swim.Best:0.0}s");
        _testDive = true;
        for (int i = 0; i < 140; i++) await PhysicsTick();
        Verify(_player.Swimming && _player.Eye.Environment == _underwater && _drowning == 0, "can dive in calm water without drowning");
        _testDive = false;
        _testRise = true;
        for (int i = 0; i < 145; i++) await PhysicsTick();
        _testRise = false;
        Verify(_player.Swimming && _player.Eye.Environment == null, "swam back to the surface");
        await WalkToward(_beach.Swim.Start, 1.85f, 23);
        Verify(!_player.Swimming && _stage == Stage.Beach, "can swim back and walk onto dry sand");

        await WalkToward(_beach.Match.Entry, 2.1f, 31);
        _interact = true;
        await PhysicsTick();
        Verify(_beach.Match.Active, "the volleyball match starts with E");
        var serve = _beach.Match.Ball.GlobalPosition;
        await WalkToward(serve, 1.32f, 10);
        for(int round=0; round<6;round++)
        {
            for(int tick=0;tick<135 && !_beach.Match.ServeReady;tick++) await PhysicsTick();
            Verify(_beach.Match.ServeReady, "the match supplies another ball after the point");
            _player.Face(round==0 ? Vector3.Right : new Vector3(.52f,0,round%2==0?.85f:-.85f),.05f);
            _hand = true;
            for(int tick=0;tick<28 && _beach.Match.Ball.LinearVelocity.Length()<=4;tick++) await PhysicsTick();
            Verify(_beach.Match.Ball.LinearVelocity.Length()>4, "an aimed hit launches the volleyball");
            for(int i=0;i<360 && _beach.Match.Home+_beach.Match.Away <= round;i++) await PhysicsTick();
            Verify(_beach.Match.Home+_beach.Match.Away==round+1, $"the rally awards a point ({_beach.Match.Score})");
        }
        for(int tick=0;tick<105 && !_beach.Match.Complete;tick++) await PhysicsTick();
        Verify(_beach.Match.Complete && _beach.Match.Home==5, "won a full volleyball match against the receiver");
        Verify(_age<WarningAt && _drowning==0, $"played beach activities within the five-minute opening ({_age:0.0}s)");
    }

    private void AimAndMove(Vector3 point)
    {
        var offset=(point-_player.GlobalPosition).Slide(Vector3.Up);
        if(offset.LengthSquared()>.001f) _player.Face(offset.Normalized());
        _testInput=new Vector2(0,1);
    }

    private async Task WalkToward(Vector3 point,float distance,float limit)
    {
        var end=_age+limit;
        while (HorizontalDistance(_player.GlobalPosition,point)>distance && _age<end)
        {
            AimAndMove(point);
            await PhysicsTick();
        }
        _testInput=Vector2.Zero;
        Verify(HorizontalDistance(_player.GlobalPosition,point) <= distance + .01f,
            $"reached beach location {point} (player {_player.GlobalPosition})");
    }

    private async Task PreviewScene()
    {
        await Capture("01-menu");
        Begin(false);
        await Capture("02-beach");
        _player.Resting=false;
        _player.GlobalPosition = _beach.LemonadeStand + new Vector3(1.15f,.09f,-.72f);
        _player.Face(new Vector3(.69f,0,.71f),-.81f);
        _player.Serve();
        for(int i=0;i<24;i++) await PhysicsTick();
        await Capture("09-drink-and-legs");
        _player.GlobalPosition = new Vector3(_coast-26, -.12f, _beach.Spawn.Z+35);
        _player.Face(new Vector3(.54f,0,-.83f),-.075f);
        await Capture("10-boardwalk");
        var swim = _beach.Swim.Start;
        _player.GlobalPosition = new Vector3(swim.X + 26, -1.63f,swim.Z+5);
        _player.Face(new Vector3(.86f,0,.51f),-.14f);
        _testInput=new Vector2(0,1);
        for(int i=0;i<70;i++) await PhysicsTick();
        await Capture("11-surface");
        _testDive=true;
        for(int i=0;i<84;i++) await PhysicsTick();
        _testDive=false;
        await Capture("12-underwater");
        _testInput=Vector2.Zero;
        _player.GlobalPosition = _beach.Match.Entry + new Vector3(1.6f,.12f,.32f);
        _player.Face(new Vector3(.61f,0,.79f),-.045f);
        _beach.Match.Toggle();
        _player.Face((_beach.Match.Ball.GlobalPosition - _player.Eye.GlobalPosition).Slide(Vector3.Up).Normalized(),-.29f);
        await Capture("13-volleyball");
        _beach.Match.Abort();
        var carView=_car.GlobalPosition + _car.GlobalBasis * new Vector3(-3.95f,0,-4.35f);
        _player.GlobalPosition = new Vector3(carView.X,.08f,carView.Z);
        _player.Face((_car.GlobalPosition-carView).Slide(Vector3.Up).Normalized(),-.09f);
        await Capture("14-tideline");
        EnterCar();
        await Capture("15-cockpit-day");
        ExitCar();
        var district=_road.Route.First(p=>p.X < -901);
        _player.GlobalPosition=district + new Vector3(0,6,3.8f);
        _player.Face(new Vector3(-.65f,0,.76f),-.18f);
        await Capture("16-west-district");
        _player.GlobalPosition=_beach.Spawn;
        _player.Resting = false;
        _player.GlobalPosition += new Vector3(-5f, 0, -3.5f);
        _player.Face(new Vector3(.76f, 0, .65f), -.057f);
        await Capture("03-beach-detail");
        _age = DisasterAt + 10.3f;
        _stage = Stage.Warning;
        _player.GlobalPosition = new Vector3(_coast - 17, -.1f, _beach.Spawn.Z);
        _player.Face(Vector3.Right, .12f);
        await Capture("04-surge");
        _age = DisasterAt + 14;
        _player.GlobalPosition = new Vector3(_coast - 48, 4.0f, _car.GlobalPosition.Z + 13);
        _player.Face(new Vector3(1, 0, -.16f), .20f);
        await Capture("05-surge-coast");
        _player.GlobalPosition = _car.GlobalPosition;
        EnterCar();
        await Capture("06-cockpit");
        ExitCar();
        _player.GlobalPosition = _road.Pad + new Vector3(7.5f, 0, 5);
        _player.Face(new Vector3(-1, 0, -.52f));
        CallRescue();
        for (int i = 0; i < 820; i++) await PhysicsTick();
        await Capture("07-rescue");
        BoardRescue();
        for (int i = 0; i < 770; i++) await PhysicsTick();
        await Capture("08-ending");
    }

    private async Task Capture(string title)
    {
        for (var i = 0; i < 6; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        var path = System.IO.Path.Combine(OS.GetEnvironment("TEMP"), "harbor-v2-" + title + ".png");
        Verify(image.SavePng(path) == Error.Ok, $"captured {path}");
    }

    private async Task PhysicsTick() => await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

    private static void Verify(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        GD.Print($"Verified: {label}");
    }
}
#endif
