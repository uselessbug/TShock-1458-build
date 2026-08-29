using System.Security.Cryptography;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace UselessBug.TShockPlugins.QuickRegister;

[ApiVersion(2, 1)]
public sealed class QuickRegisterPlugin : TerrariaPlugin
{
    public override string Name => "QuickRegister";
    public override string Author => "uselessbug";
    public override string Description => "Automatically creates a TShock account for first-time players and lets native UUID login authenticate it.";
    public override Version Version => new(1, 0, 0);

    public QuickRegisterPlugin(Main game) : base(game)
    {
        Order = 10;
    }

    public override void Initialize()
    {
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        base.Dispose(disposing);
    }

    private static void OnJoin(JoinEventArgs args)
    {
        if (TShock.Config.Settings.DisableUUIDLogin)
            return;

        var player = TShock.Players[args.Who];
        if (player is null || string.IsNullOrWhiteSpace(player.Name) || string.IsNullOrWhiteSpace(player.UUID))
            return;

        if (TShock.UserAccounts.GetUserAccountByName(player.Name) is not null)
            return;

        var accounts = TShock.UserAccounts.GetUserAccounts();
        if (accounts?.Any(a => !string.IsNullOrEmpty(a.UUID) && a.UUID == player.UUID) == true)
            return;

        var account = new UserAccount
        {
            Name = player.Name,
            UUID = player.UUID,
            Group = TShock.Config.Settings.DefaultRegistrationGroupName
        };

        account.CreateBCryptHash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

        try
        {
            TShock.UserAccounts.AddUserAccount(account);
            TShock.Log.ConsoleInfo($"[QuickRegister] Created account '{account.Name}' in group '{account.Group}'.");
        }
        catch (UserAccountExistsException)
        {
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[QuickRegister] Failed to create account for '{player.Name}': {ex}");
        }
    }
}
