using System.Security.Cryptography;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace UselessBug.TShockPlugins.AutoRegister;

[ApiVersion(2, 1)]
public sealed class AutoRegisterPlugin : TerrariaPlugin
{
    public override string Name => "AutoRegister";
    public override string Author => "uselessbug";
    public override string Description => "Automatically creates a TShock account for first-time players and lets native UUID login authenticate it.";
    public override Version Version => new(1, 0, 0);

    public AutoRegisterPlugin(Main game) : base(game)
    {
        // TShock itself uses Order = 0. Server hooks run higher priorities first,
        // so create the account before TShock handles ServerJoin.
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

        // Never touch an existing same-name account. TShock will perform its normal
        // password/UUID checks, including privileged accounts.
        if (TShock.UserAccounts.GetUserAccountByName(player.Name) is not null)
            return;

        // If this UUID is already bound to another account (for example an owner
        // account whose name differs from the Terraria character name), do not create
        // a second account or alter the existing binding.
        var accounts = TShock.UserAccounts.GetUserAccounts();
        if (accounts?.Any(a => !string.IsNullOrEmpty(a.UUID) && a.UUID == player.UUID) == true)
            return;

        var account = new UserAccount
        {
            Name = player.Name,
            UUID = player.UUID,
            Group = TShock.Config.Settings.DefaultRegistrationGroupName
        };

        // A random password exists only to satisfy TShock's account schema. Players
        // authenticate through TShock's native UUID login and never need this value.
        account.CreateBCryptHash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

        try
        {
            TShock.UserAccounts.AddUserAccount(account);
            TShock.Log.ConsoleInfo($"[AutoRegister] Created account '{account.Name}' in group '{account.Group}'.");
        }
        catch (UserAccountExistsException)
        {
            // A simultaneous registration won the race. Leave the account untouched.
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[AutoRegister] Failed to create account for '{player.Name}': {ex}");
        }
    }
}
