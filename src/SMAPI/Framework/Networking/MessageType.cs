using StardewValley;

namespace StardewModdingAPI.Framework.Networking
{
    /// <summary>Network message types recognised by SMAPI and Stardew Valley.</summary>
    internal enum MessageType : byte
    {
        /*********
        ** SMAPI
        *********/
        /// <summary>An initial GameLocation Sync Request for SMAPI to process.</summary>
        SyncRequest = 253,

        /// <summary>A data message intended for mods to consume.</summary>
        ModMessage = 254,

        /// <summary>Metadata context about a player synced by SMAPI.</summary>
        ModContext = 255,

        /*********
        ** Vanilla
        *********/
        /// <summary>Metadata about the host server sent to a farmhand.</summary>
        ServerIntroduction = Multiplayer.serverIntroduction,

        /// <summary>Metadata about a player sent to a farmhand or server.</summary>
        PlayerIntroduction = Multiplayer.playerIntroduction
    }
}
