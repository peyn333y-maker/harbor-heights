using Godot;

public partial class HarborSound : Node
{
    private AudioStreamPlayer _surf = null!;
    private AudioStreamPlayer _calm = null!;
    private AudioStreamPlayer _surge = null!;
    private AudioStreamPlayer _recovery = null!;
    private AudioStreamPlayer _engine = null!;
    private AudioStreamPlayer _siren = null!;
    private AudioStreamPlayer _step = null!;
    private AudioStreamPlayer _thunder = null!;
    private AudioStreamPlayer _splash = null!;
    private AudioStreamPlayer _ball = null!;
    private AudioStreamPlayer _sip = null!;
    private AudioStreamPlayer3D _rotor = null!;
    private AudioStreamWav _fracture = null!;
    private float _crashAt;
    private float _stepPitch;

    public void Build()
    {
        _surf = Playable("surf", true);
        _calm = Playable("calm", true);
        _surge = Playable("surge", true);
        _recovery = Playable("recovery");
        _thunder = Playable("thunder");
        _engine = Player(Synthesize(1, (t, _) =>
            (.5f * Sin(42, t) + .22f * Sin(84, t) + .14f * Sin(126, t)) * (.72f + .28f * Sin(14, t)), true));
        _siren = Player(Synthesize(4, (t, _) =>
        {
            var angle = Mathf.Tau * (520 * t - 125 / (Mathf.Pi * .5f) * Mathf.Cos(t * Mathf.Pi * .5f));
            return (.62f * Mathf.Sin(angle) + .16f * Mathf.Sin(angle * 2)) * Mathf.Sin(t / 4 * Mathf.Pi);
        }, true));
        _step = Player(Synthesize(.21f, (t, n) => (n * .49f + Sin(74, t) * .25f) * Mathf.Exp(-t * 22)
            * Mathf.Min(t * 95, 1)));
        _splash = Player(Synthesize(.43f, (t,n) => ((.79f*n+.24f*Sin(86,t))*Mathf.Exp(-t*8)
            + .23f*Sin(210 + 450*t,t)*Mathf.Exp(-t*16))*Mathf.Min(t*60,1)));
        _splash.VolumeDb = -20;
        _ball = Player(Synthesize(.15f,(t,n) => (.69f*Sin(110 - 180*t,t) + .25f*n)*Mathf.Exp(-t*38)));
        _ball.VolumeDb = -13;
        _sip = Player(Synthesize(.28f, (t,n) => (.46f*n + .30f*Sin(418+71*Mathf.Sin(t*32),t))
            * Mathf.Min(t*32,1)*Mathf.Exp(-t*12)));
        _sip.VolumeDb = -21;
        _fracture = Synthesize(1.12f, (t, n) => (.51f * n * Mathf.Abs(Sin(7, t)) + .45f * Sin(37, t) + .16f * Sin(60, t))
            * Mathf.Exp(-t * 3.6f) * Mathf.Min(t * 35, 1));
        _surf.VolumeDb = -18;
        _surf.Play();
        _calm.VolumeDb = -17;
        _calm.Play();
    }

    public void CallRotor(Node3D helicopter)
    {
        _rotor = new AudioStreamPlayer3D { Stream = Synthesize(1, (t, n) =>
        {
            var beat = Mathf.Pow(.5f + .5f * Sin(18, t), 5);
            return (.65f * Sin(55, t) + .14f * Sin(110, t) + .15f * n) * beat;
        }, true), UnitSize = 19, MaxDistance = 570, VolumeDb = -5 };
        helicopter.AddChild(_rotor);
        _rotor.Play();
    }

    public void Step(bool fast)
    {
        _stepPitch = Mathf.PosMod(_stepPitch + .17f, .36f);
        _step.PitchScale = .80f + _stepPitch + (fast ? .20f : 0);
        _step.VolumeDb = fast ? -16 : -20;
        _step.Play();
    }

    public void Crash(Vector3 at, float height)
    {
        var now = Time.GetTicksMsec() * .001f;
        if (_crashAt > now) return;
        _crashAt = now + .19f;
        var sound = new AudioStreamPlayer3D { Stream = _fracture, UnitSize = 18, MaxDistance = 290,
            PitchScale = Mathf.Clamp(1.32f - height * .008f, .66f, 1.24f), VolumeDb = -4 };
        AddChild(sound);
        sound.GlobalPosition = at;
        sound.Finished += sound.QueueFree;
        sound.Play();
    }

    public void Splash() => _splash.Play();
    public void BallHit() => _ball.Play();
    public void Sip() => _sip.Play();
    public void Muffle(bool below, float dt) =>
        _surf.PitchScale = Mathf.Lerp(_surf.PitchScale, below ? .74f : 1, Mathf.Min(1,dt*5));

    public void Warning()
    {
        _siren.VolumeDb = -17;
        _siren.Play();
        _thunder.VolumeDb = -10;
        _thunder.Play();
    }

    public void StartSurge()
    {
        _surge.VolumeDb = -23;
        _surge.Play();
    }

    public void Escape()
    {
        _surge.Stop();
        _siren.Stop();
        _calm.Stop();
        _recovery.VolumeDb = -16;
        _recovery.Play();
    }

    public void Mix(float fromShore, float storm, float surgeNear, bool inCar, float speed, float throttle, float dt)
    {
        _surf.VolumeDb = Mathf.Lerp(_surf.VolumeDb, -29 + surgeNear * 21 + 14 * Mathf.Exp(-Mathf.Max(0, fromShore) / 85), dt * 2);
        _calm.VolumeDb = Mathf.Lerp(_calm.VolumeDb, -17 - storm * 37, dt);
        if (_surge.Playing) _surge.VolumeDb = Mathf.Lerp(_surge.VolumeDb, -16 + surgeNear * 3, dt * .55f);
        if (_siren.Playing) _siren.VolumeDb = -16 - Mathf.Clamp(fromShore / 30, 0, 40);
        if (inCar && !_engine.Playing) _engine.Play();
        if (!inCar && _engine.Playing) _engine.Stop();
        if (inCar)
        {
            _engine.PitchScale = Mathf.Lerp(_engine.PitchScale, .58f + speed * .050f + Mathf.Abs(throttle) * .29f, dt * 6);
            _engine.VolumeDb = -20 + Mathf.Abs(throttle) * 5;
        }
    }

    private AudioStreamPlayer Playable(string file, bool loop = false)
    {
        var stream = GD.Load<AudioStreamMP3>($"res://assets/audio/{file}.mp3");
        stream.Loop = loop;
        return Player(stream);
    }

    private AudioStreamPlayer Player(AudioStream stream)
    {
        var play = new AudioStreamPlayer { Stream = stream };
        AddChild(play);
        return play;
    }

    private static float Sin(float hz, float t) => Mathf.Sin(Mathf.Tau * hz * t);

    private static AudioStreamWav Synthesize(float seconds, Func<float, float, float> wave, bool loop = false)
    {
        const int rate = 22050;
        var samples = (int)(rate * seconds);
        var pcm = new byte[samples * 2];
        var rng = new Random(1909);
        for (var i = 0; i < samples; i++)
        {
            var value = (short)(short.MaxValue * Mathf.Clamp(wave((float)i / rate, (float)(2 * rng.NextDouble() - 1)), -1, 1));
            pcm[i * 2] = (byte)value;
            pcm[i * 2 + 1] = (byte)(value >> 8);
        }
        return new AudioStreamWav { Data = pcm, MixRate = rate, Format = AudioStreamWav.FormatEnum.Format16Bits,
            LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled, LoopEnd = samples };
    }
}
