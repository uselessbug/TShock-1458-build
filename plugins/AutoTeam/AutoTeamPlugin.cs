using System.Text.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace UselessBug.TShockPlugins.AutoTeam;

[ApiVersion(2, 1)]
public sealed class AutoTeamPlugin : TerrariaPlugin
{
    private sealed class Config
    {
        public int Team { get; set; } = 1;
        public bool OnlyIfNoTeam { get; set; } = true;
    }

    private Config _config = new();
    private string ConfigPath => Path.Combine(TShock.SavePath, "AutoTeam.json");

    public override string Name => "AutoTeam";
    public override string Author => "uselessbug";
    public override string Description => "Automatically assigns players to a Terraria team after joining.";
    public override Version Version => new(1, 0, 1);

    public AutoTeamPlugin(Main game) : base(game)
    {
        // TShock uses Order = 0. Hook priority defaults to plugin Order and higher
        // priorities run first, so -10 keeps our greet handler after TShock's.
        Order = -10;
    }

    public override void Initialize()
    {
        // This plugin itself initializes before TShock because of Order = -10.
        // Defer config loading until TShock has processed -configpath and initialized SavePath.
        TShock.Initialized += OnTShockInitialized;
        ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TShock.Initialized -= OnTShockInitialized;
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
        }
        base.Dispose(disposing);
    }

    private void OnTShockInitialized()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            Directory.CreateDirectory(TShock.SavePath);

            if (File.Exists(ConfigPath))
                _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new Config();
            else
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));

            if (_config.Team is < 0 or > 5)
            {
                TShock.Log.ConsoleWarn($"[AutoTeam] Invalid team {_config.Team}; using Red team (1).");
                _config.Team = 1;
            }

            TShock.Log.ConsoleInfo($"[AutoTeam] Config loaded from '{ConfigPath}'.");
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[AutoTeam] Failed to load config; using defaults: {ex}");
            _config = new Config();
        }
    }

    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var player = TShock.Players[args.Who];
        if (player is null || !player.Active)
            return;

        if (_config.Team == 0)
            return;

        if (_config.OnlyIfNoTeam && player.TPlayer.team != 0)
            return;

        if (player.TPlayer.team == _config.Team)
            return;

        player.TPlayer.team = _config.Team;
        player.LastPvPTeamChange = DateTime.UtcNow;

        // PlayerTeam (45) serializes the team from Main.player[number].team.
        TSPlayer.All.SendData(PacketTypes.PlayerTeam, number: player.Index);
    }
}
