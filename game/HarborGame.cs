using Godot;

public partial class HarborGame : Node3D
{
    public const float DisasterAt = 300;
    private const float WarningAt = 253;
    private enum Stage { Loading, Title, Beach, Warning, Tsunami, Rescue, Flight, Won, Lost }
    private Stage _stage = Stage.Loading;
    private Node3D _city = null!;
    private HarborPlayer _player = null!;
    private BeachLife _beach = null!;
    private HarborWater _water = null!;
    private FloodDamage _damage = null!;
    private EscapeCar _car = null!;
    private EvacuationRoad _road = null!;
    private RescueFlight _rescue = null!;
    private HarborHud _hud = null!;
    private HarborSound _sound = null!;
    private WorldEnvironment _weather = null!;
    private DirectionalLight3D _sun = null!;
    private ShaderMaterial _sky = null!;
    private Godot.Environment _underwater = null!;
    private float _coast;
    private float _age;
    private float _drowning;
    private float _tipped;
    private float _viewPitch;
    private float _viewYaw;
    private float _storm;
    private int _skyStep = -1;
    private float _toastUntil;
    private string _toast = "";
    private bool _inCar;
    private bool _paused;
    private bool _interact;
    private bool _jump;
    private bool _hand;
    private bool _lookBehind;
    private int _beachGuide;
    private int _weatherBeat;
    private bool _triedLemonade;
    private static bool _retry;
    private Vector2? _testInput = null;
    private bool _testSprint = false;
    private bool _testDive = false;
    private bool _testRise = false;
    private const float CheckpointRadius = 24;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _city = GetNode<Node3D>("City");
        _city.ProcessMode = ProcessModeEnum.Pausable;

        _hud = AddGameNode(new HarborHud(), false);
        _hud.Build();
        _hud.Menu("ПРИЛИВ", "Город готовится к вашему приезду.", "Загрузка…", () => { });
        _coast = (float)_city.Call("game_coast");
        _weather = _city.GetChildren().OfType<WorldEnvironment>().First();
        _sun = _city.GetChildren().OfType<DirectionalLight3D>().First();
        _sky = (ShaderMaterial)_weather.Environment.Sky.SkyMaterial;
        _underwater = (Godot.Environment)_weather.Environment.Duplicate();
        _underwater.BackgroundMode = Godot.Environment.BGMode.Color;
        _underwater.BackgroundColor = new Color("366567");
        _underwater.FogDensity = .077f;
        _underwater.FogLightColor = new Color("366567");
        _underwater.FogSkyAffect = 1;
        _underwater.AmbientLightColor = new Color("91c6bd");
        _underwater.AmbientLightEnergy = .75f;
        _underwater.GlowEnabled = false;

        _damage = AddGameNode(new FloodDamage());
        _road = AddGameNode(new EvacuationRoad());
        var downtown = _city.Call("game_road").AsVector3Array();
        _road.Build(downtown, _coast, _damage, _city);
        _beach = AddGameNode(new BeachLife());
        _beach.Build(_coast, downtown[0].Z + 17);
        _water = AddGameNode(new HarborWater());
        _water.Build(_coast);
        _rescue = AddGameNode(new RescueFlight());
        _rescue.Build(_road.Pad);
        _player = AddGameNode(new HarborPlayer());
        _player.Build(_beach.Spawn);
        _player.Eye.MakeCurrent();
        _city.Call("game_palms", downtown[0].Z + 17);
        _car = AddGameNode(new EscapeCar());
        _car.Build(_road.CarSpawn, _road.CarDirection);
        _sound = AddGameNode(new HarborSound());
        _sound.Build();
        _player.StepSound = _sound.Step;
        _player.Splash = _sound.Splash;
        _damage.CollapseSound = _sound.Crash;

        if (!_city.Get("build_complete").AsBool()) await ToSignal(_city, "city_built");
        _damage.RegisterCity(_city);
        GD.Print($"Harbor Heights ready. Evacuation road {_road.TotalLength:0} m.");
        if (_retry)
        {
            _retry = false;
            Begin(true);
        }
        else
        {
            _stage = Stage.Title;
            _hud.Menu("ПРИЛИВ", "Вы приехали на пляж Солт-Бич провести тихий день у моря.",
                "Начать игру", () => Begin(false), "Сразу к эвакуации", () => Begin(true));
        }
#if DEBUG
        RunVerification();
#endif
    }

    private T AddGameNode<T>(T node, bool pause = true) where T : Node
    {
        node.ProcessMode = pause ? ProcessModeEnum.Pausable : ProcessModeEnum.Always;
        AddChild(node);
        return node;
    }

    private void Begin(bool checkpoint)
    {
        _stage = Stage.Beach;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _hud.Play();
        if (!checkpoint) return;
        _beach.Match.Abort();
        _beach.Swim.Stop();
        _age = DisasterAt - 1.8f;
        _stage = Stage.Warning;
        _player.GlobalPosition = _car.GlobalPosition + _car.GlobalBasis.X * 2.1f - Vector3.Up * .70f;
        _player.Resting = false;
        _player.Face(_road.CarDirection);
        SetWeather(1);
        _sound.Warning();
        EnterCar();
    }

    private static bool Held(Key key) => Input.IsPhysicalKeyPressed(key);
    private Vector2 Movement() => _testInput ?? new Vector2(
        (Held(Key.D) || Held(Key.Right) ? 1 : 0) - (Held(Key.A) || Held(Key.Left) ? 1 : 0),
        (Held(Key.W) || Held(Key.Up) ? 1 : 0) - (Held(Key.S) || Held(Key.Down) ? 1 : 0));

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Escape && _stage is >= Stage.Beach and <= Stage.Flight)
            {
                Pause(!_paused);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (!_paused)
            {
                if (key.PhysicalKeycode == Key.E) _interact = true;
                if (key.PhysicalKeycode == Key.Space) _jump = true;
                if (key.PhysicalKeycode == Key.Tab && _stage == Stage.Beach) _beachGuide = (_beachGuide + 1) % 3;
            }
        }

        if (!_paused && Input.MouseMode == Input.MouseModeEnum.Captured &&
            e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) _hand = true;
        if (_paused || Input.MouseMode != Input.MouseModeEnum.Captured || e is not InputEventMouseMotion motion) return;
        if (_inCar)
        {
            _viewYaw = Mathf.Clamp(_viewYaw - motion.Relative.X * .0021f, -1.23f, 1.23f);
            _viewPitch = Mathf.Clamp(_viewPitch - motion.Relative.Y * .0021f, -.65f, .52f);
        }
        else if (_stage is >= Stage.Beach and <= Stage.Rescue)
            _player.Mouse(motion.Relative);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && !_paused && _stage is >= Stage.Beach and <= Stage.Flight)
            Pause(true);
    }

    private void Pause(bool paused)
    {
        _paused = paused;
        GetTree().Paused = paused;
        Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        if (paused)
            _hud.Menu("Пауза", "Время и симуляция приостановлены.", "Продолжить",
                () => Pause(false), "Начать заново", () => Restart(false),
                _stage == Stage.Beach ? "Сразу к эвакуации" : "", _stage == Stage.Beach ? SkipToEvacuation : null);
        else
            _hud.Play();
        _interact = _jump = _hand = false;
    }

    private void SkipToEvacuation()
    {
        Pause(false);
        Begin(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_paused || _car == null || _stage is Stage.Lost or Stage.Won) return;
        var dt = (float)delta;
        if (_stage is Stage.Loading or Stage.Title)
        {
            _car.Drive(dt);
            return;
        }

        _age += dt;
        AdvanceStory();
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;
        if (_inCar && camera == _car.DashboardCamera)
        {
            _lookBehind = Held(Key.Q);
            var view = _lookBehind ? new Vector3(-.055f, Mathf.Pi, 0) : new Vector3(_viewPitch - .10f, _viewYaw, 0);
            camera.Rotation = view;
        }
        var eye = camera.GlobalPosition;
        var waves = Mathf.SmoothStep(75, 147, _age);
        var storm = Mathf.SmoothStep(117, 239, _age);
        if (_stage == Stage.Flight) storm *= .48f;
        SetWeather(storm);
        _water.Advance(_age, waves, _age < DisasterAt ? -1 : _age - DisasterAt);
        if (_age >= DisasterAt) _damage.Step(_water, eye, dt);
        var input = Movement();

        _car.Throttle = _inCar ? input.Y : 0;
        _car.Turn = _inCar ? -input.X : 0;
        _car.Handbrake = !_inCar || Held(Key.Space) || Mathf.IsZeroApprox(input.Y) && _car.Speed < .65f;
        _car.Drive(dt);
        if (_age >= DisasterAt)
        {
            _water.Push(_car, 1.35f, .36f, 1.10f, new Vector3(0, -.19f, .89f));
            _water.Push(_car, 1.35f, .31f, 1.05f, new Vector3(0, -.19f, -.94f));
            _beach.FloodProps(_water);
        }

        _player.StowCup = _beach.Match.Active;
        if (!_inCar && _stage != Stage.Flight)
            _player.Walk(input, _testSprint || Held(Key.Shift), _jump, _testRise || Held(Key.Space), _testDive || Held(Key.Ctrl), _water, dt);

        var swimming = !_inCar && _player.Swimming;
        var submerged = swimming && _water.Sample(eye).Depth(eye.Y) > .09f;
        _player.Eye.Environment = submerged ? _underwater : null;
        _sound.Muffle(submerged, dt);
        if (_stage <= Stage.Tsunami)
        {
            _beach.UpdatePeople(_age, _stage >= Stage.Warning, dt);
            if (_stage == Stage.Beach)
            {
                _beach.Match.Step(eye, dt);
                _beach.Swim.Step(_player, _water, dt);
            }
            else _beach.Match.LeaveCourt(dt);
        }
        if (_stage is Stage.Rescue or Stage.Flight) _rescue.Fly(dt);
        if (_stage == Stage.Flight && _rescue.Finished)
        {
            Win();
            return;
        }

        if (_stage <= Stage.Rescue)
        {
            if (_stage == Stage.Tsunami && _inCar && HorizontalDistance(_car.GlobalPosition, _road.Checkpoint) < CheckpointRadius)
                CallRescue();
            if (CheckSurvival(eye, dt)) return;
            Interactions();
        }
        UpdateHud(eye, -camera.GlobalBasis.Z);
        var waveNear = _age < DisasterAt ? 0 : Mathf.Exp(-Mathf.Abs(eye.X - _water.Front) / 190);
        _sound.Mix(_coast - eye.X, storm, waveNear, _inCar, _car.Speed, _car.Throttle, dt);
        _interact = _jump = _hand = false;
    }

    private void AdvanceStory()
    {
        if (_stage == Stage.Beach)
        {
            if (_weatherBeat == 0 && _age >= 72)
            {
                _weatherBeat++;
                Toast("У берега поднимаются волны. На вышке меняют флаг.", 7);
            }
            else if (_weatherBeat == 1 && _age >= 157)
            {
                _weatherBeat++;
                Toast("Смотритель: «Видимость падает. Следите за сигналами вышки».", 7);
            }
            else if (_weatherBeat == 2 && _age >= 217)
            {
                _weatherBeat++;
                Toast("С берега видна тёмная полоса. Держитесь ближе к набережной.", 8);
            }
        }
        if (_stage == Stage.Beach && _age >= WarningAt)
        {
            _stage = Stage.Warning;
            _beach.Match.Abort();
            _beach.Swim.Stop();
            _player.Zoomed = false;
            _sound.Warning();
            Toast("Громкоговоритель: «Внимание! Покиньте берег. Двигайтесь на запад».", 10);
        }
        if (_stage == Stage.Warning && _age >= DisasterAt)
        {
            _stage = Stage.Tsunami;
            _sound.StartSurge();
            Toast("Волна идёт на город. Следуйте по западной трассе к Пайн-Ридж.", 8);
        }
    }

    private void SetWeather(float storm)
    {
        if (Mathf.Abs(storm - _storm) < .0015f) return;
        _storm = storm;
        var environment = _weather.Environment;
        environment.FogDensity = Mathf.Lerp(.00054f, .0045f, storm);
        environment.FogLightColor = new Color("9aadb8").Lerp(new Color("81958f"), storm);
        environment.AmbientLightEnergy = Mathf.Lerp(.83f, .58f, storm);
        environment.AdjustmentSaturation = Mathf.Lerp(1.14f, .83f, storm);
        _sun.LightEnergy = Mathf.Lerp(2.03f, .60f, storm);
        _sun.LightColor = new Color("ffd6a5").Lerp(new Color("bbcbd4"), storm);
        var step = Mathf.RoundToInt(storm * 22);
        if (step != _skyStep)
        {
            _sky.SetShaderParameter("storm", step / 22f);
            _skyStep = step;
        }
    }

    private void EnterCar()
    {
        _inCar = true;
        _player.CollisionLayer = 0;
        _player.CollisionMask = 0;
        _player.Velocity = Vector3.Zero;
        _player.Embody(false);
        _car.Cabin(true);
        _car.DashboardCamera.Rotation = new Vector3(-.10f,0,0);
        _car.DashboardCamera.MakeCurrent();
        _viewPitch = _viewYaw = 0;
    }

    private void ExitCar()
    {
        _inCar = false;
        _lookBehind = false;
        _player.Embody(true);
        var position = _car.GlobalPosition;
        var side = _car.GlobalBasis.X.Slide(Vector3.Up).Normalized();
        var left = position - side * 2.06f;
        var ray = PhysicsRayQueryParameters3D.Create(position - side, left + Vector3.Down * .23f, 5,
            new Godot.Collections.Array<Rid> { _car.GetRid() });
        var space = GetWorld3D().DirectSpaceState;
        if (space.IntersectRay(ray).Count > 0) left = position + side * 2.06f;
        var down = space.IntersectRay(PhysicsRayQueryParameters3D.Create(left + Vector3.Up, left + Vector3.Down * 3, 1));
        _player.GlobalPosition = down.Count > 0 ? down["position"].AsVector3() + Vector3.Up * .04f : left;
        _player.CollisionLayer = 8;
        _player.CollisionMask = 7;
        _player.Resting = false;
        _player.Face(-_car.GlobalBasis.Z.Slide(Vector3.Up).Normalized());
        _player.Eye.MakeCurrent();
        _car.Cabin(false);
    }

    private void CallRescue()
    {
        _stage = Stage.Rescue;
        _rescue.Call();
        _sound.CallRotor(_rescue);
        Toast("Пайн-Ридж: «Вас видим. Вертолёт заходит на посадку».", 11);
    }

    private void BoardRescue()
    {
        _stage = Stage.Flight;
        _player.CollisionLayer = _player.CollisionMask = 0;
        _player.Embody(false);
        _rescue.Board();
        _sound.Escape();
        Toast("Борт 07: «Мы забрали последнего. Уходим».", 7);
    }

    private bool CheckSurvival(Vector3 eye, float dt)
    {
        var depth = _water.Sample(eye).Depth(eye.Y);
        var flooded = _water.SurgeActive && (depth > (_inCar ? .22f : .23f) ||
            (!_inCar && _player.Swimming && _water.Sample(eye).Flow.Length() > 9.5f));
        _drowning = Mathf.MoveToward(_drowning, flooded ? 8 : 0, dt * (flooded ? 1 : 1.25f));
        _tipped = _inCar && _car.GlobalBasis.Y.Y < .24f && _car.Speed < 3.2f ? _tipped + dt : 0;
        var underwater = (!_inCar && _player.Swimming) ? Mathf.Clamp(depth * .57f,0,.31f) : 0;
        _hud.Tint(new Color(.034f,.21f,.23f,Mathf.Max(underwater,Mathf.Clamp(_drowning/7,0,.89f)*.69f)));
        if (_drowning >= 7.1f || _tipped > 7.0f && _age >= DisasterAt || Mathf.Abs(eye.Z) > 6900 ||
            eye.Y < -22 || eye.X < -6900)
        {
            Lose(_tipped > 7 ? "Машина перевернулась. До площадки ещё далеко." : "Вы оказались под водой.");
            return true;
        }
        return false;
    }

    private void Interactions()
    {
        if (_hand && !_inCar && _stage is >= Stage.Beach and <= Stage.Tsunami)
        {
            if (_beach.Match.Active)
            {
                if (_beach.Match.Hit(_player.Eye.GlobalPosition, -_player.Eye.GlobalBasis.Z))
                {
                    _player.Reach();
                    _sound.BallHit();
                }
            }
            else if (_player.Sip())
            {
                _triedLemonade = true;
                _sound.Sip();
            }
        }
        if (!_interact) return;
        if (_inCar)
        {
            if (_car.Speed < 2.7f) ExitCar();
            else Toast("Сбавьте скорость, чтобы выйти.", 2);
            return;
        }
        var position = _player.GlobalPosition;
        if (_stage == Stage.Rescue && _rescue.Landed && position.DistanceTo(_rescue.Door) < 3.4f)
        {
            BoardRescue();
            return;
        }
        if (_stage >= Stage.Warning && position.DistanceTo(_car.GlobalPosition) < 3.6f && _car.Speed < 2.7f)
        {
            EnterCar();
            return;
        }
        if (_stage != Stage.Beach) return;
        if (position.DistanceTo(_beach.LemonadeStand) < 2.7f)
        {
            _player.Serve();
            _sound.Sip();
            return;
        }
        if (_beach.Match.Access(position))
        {
            _beach.Swim.Stop();
            _beach.Match.Toggle();
            return;
        }
        if (position.DistanceTo(_beach.Swim.Start) < 2.85f)
        {
            _beach.Match.Abort();
            _beach.Swim.Toggle();
            return;
        }
        if (position.DistanceTo(_beach.Lookout) < 2.65f)
        {
            _player.Zoomed = !_player.Zoomed;
            if (_player.Zoomed) _player.Face(Vector3.Right,.045f);
            return;
        }
        foreach (var station in _beach.Stations)
        {
            if (position.DistanceTo(station.Position) > 2.7f) continue;
            var reply = _age > 160 && station.Prompt.Contains("спасателя") ?
                "Спасатель: «Ветер усиливается. Не заходите в воду»." : station.Reply;
            Toast(reply, 5);
            return;
        }
    }

    private void UpdateHud(Vector3 eye, Vector3 heading)
    {
        var runningTime = Mathf.Max(0, Mathf.FloorToInt(_age));
        var clock = $"{runningTime / 60:00}:{runningTime % 60:00}";
        var nearShore = eye.X > _coast - 95;
        var area = (nearShore ? "SALT BEACH" : eye.X > -470 ? "HARBOR HEIGHTS" : "WEST DISTRICT") + "   /   ПОБЕРЕЖЬЕ";
        var prompt = "";
        var position = _player.GlobalPosition;
        var goal = "Отдохните на пляже";
        var status = clock + (_age < 76 ? "   ·   Ясно" : _age < 117 ? "   ·   Волнение на море" : "   ·   Туман над заливом");
        var guide = false;
        var target = _car.GlobalPosition;
        var remaining = HorizontalDistance(eye, target);
        var guideTitle = "Машина";
        var activity = "";

        if (_stage == Stage.Beach && !_inCar)
        {
            goal = _weatherBeat >= 2 ? "С берега видно потемнение" : "Свободное время на пляже";
            var done = (_triedLemonade ? 1 : 0) + (_beach.Match.Complete ? 1 : 0) + (_beach.Swim.Complete ? 1 : 0);
            activity = $"Занятия   {done}/3";
            if (_player.HaveCup) activity += $"   ·   Лимонад {_player.Sips}/3";
            if (_beach.Match.Active)
            {
                goal = _beach.Match.Score;
                status = _beach.Match.Ended ? "Матч окончен" : _beach.Match.ServeReady ? "Подойдите к мячу и подайте" : "Разыгрывайте мяч до касания песка";
                target = _beach.Match.Ball.GlobalPosition;
                guideTitle = "Мяч";
                guide = true;
                prompt = _beach.Match.CanHit(eye) ? (_beach.Match.ServeReady ? "ЛКМ   ·   Подать" : "ЛКМ   ·   Отбить") : "";
            }
            else if (_beach.Swim.Active)
            {
                goal = _beach.Swim.Status;
                status = "Обогните четыре буйка по порядку";
                target = _beach.Swim.Target;
                guideTitle = "Следующий буй";
                guide = true;
            }
            else
            {
                if (_beach.Swim.Complete) activity += $"   ·   Заплыв {_beach.Swim.Best:0.0} с";
                (target, guideTitle) = _beachGuide switch
                {
                    1 => (_beach.Match.Entry,"Волейбол"),
                    2 => (_beach.Swim.Start,"Заплыв"),
                    _ => (_beach.LemonadeStand,"Лимонад")
                };
                guide = !_player.Zoomed;
                if (_player.HaveCup && !_player.IsSipping && !_player.Swimming) prompt = "ЛКМ   ·   Сделать глоток";
                if (position.DistanceTo(_beach.Lookout)<2.65f) prompt = _player.Zoomed ? "E   ·   Отойти от бинокля" : "E   ·   Посмотреть в бинокль";
                else if (position.DistanceTo(_beach.LemonadeStand)<2.7f) prompt = _player.HaveCup ? "E   ·   Наполнить стакан" : "E   ·   Взять лимонад";
                else if (_beach.Match.Access(position)) prompt = "E   ·   Начать матч";
                else if (position.DistanceTo(_beach.Swim.Start)<2.85f) prompt = "E   ·   Начать заплыв";
                else
                    foreach (var stop in _beach.Stations)
                    {
                        if (position.DistanceTo(stop.Position) >= 2.7f) continue;
                        prompt = "E   ·   " + stop.Prompt;
                        break;
                    }
            }
            remaining = HorizontalDistance(eye,target);
            if (_beach.Swim.Active && position.DistanceTo(_beach.Swim.Start)<2.85f) prompt = "E   ·   Прервать заплыв";
        }
        else if (_stage is Stage.Warning or Stage.Tsunami)
        {
            goal = _inCar ? "Езжайте на запад" : "Доберитесь до внедорожника";
            var distance = Mathf.Max(0, _water.Front - eye.X);
            status = _stage == Stage.Warning ? "Сирена   ·   Уходите с берега" : $"Цунами   ·   Волна в {distance:0} м";
            guide = true;
            if (_inCar)
            {
                var route = _road.Navigation(eye);
                (target, remaining) = (route.Target, route.Remaining);
                guideTitle = "Пайн-Ридж";
                if (route.Offset > 48) status = "Вернитесь на западную трассу";
            }
            else if (position.DistanceTo(_car.GlobalPosition) < 3.6f && _car.Speed < 2.7f) prompt = "E   ·   Сесть за руль";
        }
        else if (_stage == Stage.Rescue)
        {
            area = "PINE RIDGE   /   ПЛОЩАДКА 07";
            goal = _inCar ? "Остановитесь и выйдите" : _rescue.Landed ? "Подойдите к вертолёту" : "Дождитесь посадки";
            status = "Спасательная служба на связи";
            guide = true;
            target = _rescue.Landed ? _rescue.Door : _road.Pad;
            remaining = HorizontalDistance(eye, target);
            guideTitle = "Вертолёт";
            if (_inCar && _car.Speed < 2.7f) prompt = "E   ·   Выйти из машины";
            if (!_inCar && _rescue.Landed && position.DistanceTo(_rescue.Door) < 3.4f) prompt = "E   ·   Подняться на борт";
        }
        else if (_stage == Stage.Flight)
        {
            area = "RESCUE 07   /   ЭВАКУАЦИЯ";
            goal = "Вы покидаете побережье";
            status = "Борт 07   ·   На высоте";
        }

        _hud.UpdateStatus(area, goal, status, _stage is Stage.Warning or Stage.Tsunami);
        _hud.Message(prompt, _toastUntil > _age ? _toast : "");
        _hud.Guide(eye, target, heading, guideTitle, remaining, guide);
        _hud.Driving(_inCar, _car.Speed, _player.Swimming, _stage == Stage.Beach, _beach.Match.Active);
        _hud.BeachStatus(_stage == Stage.Beach && !_inCar, activity, _player.Energy, _player.Zoomed);
    }

    private void Toast(string text, float duration)
    {
        _toast = text;
        _toastUntil = _age + duration;
    }

    private void Lose(string why)
    {
        var canRetry = _age >= WarningAt;
        _stage = Stage.Lost;
        Freeze();
        _hud.Menu("Прилив", why, canRetry ? "Повторить эвакуацию" : "Начать заново",
            () => Restart(canRetry), canRetry ? "Сначала на пляж" : "", canRetry ? () => Restart(false) : null);
    }

    private void Win()
    {
        _stage = Stage.Won;
        Freeze();
        _hud.Tint(new Color(.05f, .11f, .13f, .26f));
        var time = Mathf.FloorToInt(_age);
        _hud.Menu("Вас спасли", $"Борт 07 доставит вас за пределы города.\nВремя в игре   {time / 60:00}:{time % 60:00}",
            "Снова на пляж", () => Restart(false));
    }

    private void Freeze()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().Paused = true;
    }

    private void Restart(bool checkpoint)
    {
        _retry = checkpoint;
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();
}

