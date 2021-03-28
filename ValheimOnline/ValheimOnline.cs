using System;
using System.IO;
using System.Net.NetworkInformation;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;


namespace ValheimOnline
{

    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    public class ValheimOnline : BaseUnityPlugin
    {
        public const string Name = ModInfo.Name;
        public const string Guid = ModInfo.Guid;
        public const string Version = ModInfo.Version;

        public static ConfigEntry<string> ServerVaultPath;
        public static ConfigEntry<string> ServerSafeZonePath;
        public static ConfigEntry<string> ServerDefaultCharacterPath;
        public static ConfigEntry<string> ServerBattleZonePath;
        public static ConfigEntry<string> ServerZonePath;
        public static ConfigEntry<int> ServerSaveInterval;
		public static ConfigEntry<int> NexusID;
        public static ConfigEntry<bool> ServerPVPEnforced;
        public static ConfigEntry<bool> PVPSharePosition;
		public static ConfigEntry<bool> AllowCharacterSave;
		public static ConfigEntry<bool> AllowSinglePlayer;
		public static ConfigEntry<bool> PVPisEnabled;
		public static ConfigEntry<bool> PositionEnforced;

        public void Awake()
        {
			Debug.Log("Haz awoke!!?!");

#if DEBUG
            Debug.Log("Development Version Activated!!!");
            Debug.Log("Warning: This may break your game (90% stable)");
            Debug.Log("***Do Not Release To Public***");
#endif


            // Process through the configurations

            // Nexus ID For Nexus Update
            ValheimOnline.NexusID = base.Config.Bind<int>("ValheimOnline", "NexusID", 626, "Nexus ID to make Nexus Update Happy!");



			if (Util.isServer())
			{
				Debug.Log("[Server Mode]");
                // Load Paths
                ValheimOnline.ServerVaultPath = base.Config.Bind<string>("ValheimOnline", "ServerVaultPath", Path.Combine(Utils.GetSaveDataPath(), "characters_vault"), "SERVER ONLY: The root directory for the server vault.");
                ValheimOnline.ServerSafeZonePath = base.Config.Bind<string>("ValheimOnline", "ServerSafeZonePath", Path.Combine(Utils.GetSaveDataPath(), "safe_zones.txt"), "SERVER ONLY: The file path to the safe zone file. If it does not exist, it will be created with a default safe zone.");
                ValheimOnline.ServerDefaultCharacterPath = base.Config.Bind<string>("ValheimOnline", "ServerDefaultCharacterPath", Path.Combine(Utils.GetSaveDataPath(), "default_character.fch"), "SERVER ONLY: The file path to the default character file. If it does not exist, it will be created with a default character file.");
                ValheimOnline.ServerBattleZonePath = base.Config.Bind<string>("ValheimOnline", "ServerBattleZonePath", Path.Combine(Utils.GetSaveDataPath(), "Battle_zones.txt"), "SERVER ONLY: The file path to the Battle zone file. If it does not exist, it will be created with a default Battle zone.");
                ValheimOnline.ServerZonePath = base.Config.Bind<string>("ValheimOnline", "ServerZonePath", Path.Combine(Utils.GetSaveDataPath(), "zones.txt"), "SERVER ONLY: The file path to the zone file. If it does not exist, it will be created with a default zone.");


                // Load Settings
                // Server Save Interval
                ValheimOnline.ServerSaveInterval = base.Config.Bind<int>("ValheimOnline", "ServerSaveInterval", 600, "SERVER ONLY: How often, in seconds, to save a copy of each character. Too low may result in performance issues. Too high may result in lost data in the event of a server crash.");
                // Is the server enforcing PVP?
                ValheimOnline.ServerPVPEnforced = base.Config.Bind<bool>("ValheimOnline", "ServerPVPEnforced", false, "SERVER ONLY: Are we going to enforce a PVP mode (PVPisEnabled).");
                // What are we enforcing PVP On (TRUE) or off (FALSE)
                ValheimOnline.PVPisEnabled = base.Config.Bind<bool>("ValheimOnline", "PVPisEnabled", false, "SERVER ONLY: Enforce the servers PVP mode and prevent users from changing.");
                // Is the server enforcing Shared Positions?
                ValheimOnline.PositionEnforced = base.Config.Bind<bool>("ValheimOnline", "PositionEnforced", true, "SERVER ONLY: Are we going to enforce sharing positioning?.");
                // What are we enforcing Share Position (TRUE) or Don't Share (FALSE)
                ValheimOnline.PVPSharePosition = base.Config.Bind<bool>("ValheimOnline", "PVPSharePosition", true, "SERVER ONLY: What mode are we enforcing? Share the users position or not?");


                // Setup client state configuration
                Client.PVPEnforced = ValheimOnline.ServerPVPEnforced.Value;
                Client.PVPSharePosition = ValheimOnline.PVPSharePosition.Value;
                Client.PVPisEnabled = ValheimOnline.PVPisEnabled.Value;
                Client.PositionEnforced = ValheimOnline.PositionEnforced.Value;
            }
			else
            {
                Debug.Log("[Client Mode]");

                ValheimOnline.AllowCharacterSave = base.Config.Bind<bool>("ValheimOnline", "AllowCharacterSave", false, "CLIENT ONLY: Should we allow the client to not only send the character back to the server but save a local copy. (WARNING: THIS WILL OVERWRITE YOUR LOCAL CHARACTER FILE!! PLEASE USE A BLANK CHARACTER FILE!)");
                ValheimOnline.AllowSinglePlayer = base.Config.Bind<bool>("ValheimOnline", "AllowSinglePlayer", false, "CLIENT ONLY: Should we allow the client to play Single Player?  (WARNING: LOTS OF CONSOLE ERRORS RIGHT NOW BUT WORKS!)");
                
                // Leave the client state configuration default (Will grab from the server)
            }


            // Run the grand patch all and hope everything works (This is fine...)
            new Harmony(ModInfo.Guid).PatchAll();


            // Process through the server data needed
            if (Util.isServer())
            {
                Debug.Log("[Server Mode]");

                /*
                 * Setup default character file for server to use.
                 */
                if (!File.Exists(ValheimOnline.ServerDefaultCharacterPath.Value))
                {
                    Debug.Log($"Creating default character file at {ValheimOnline.ServerDefaultCharacterPath.Value}");
                    File.WriteAllBytes(ValheimOnline.ServerDefaultCharacterPath.Value,
                        global::ValheimOnline.Properties.Resources._default_character);
                }
                else
                {
                    Debug.Log($"Loading default character file from {ValheimOnline.ServerDefaultCharacterPath.Value}");
                    ServerState.default_character = File.ReadAllBytes(ValheimOnline.ServerDefaultCharacterPath.Value);
                }

                Debug.Log($"Loaded default character file (Size: {ServerState.default_character.Length})");

                /*
                 * Setup safe zones.
                 */
                ZoneHandler.LoadZoneData(ValheimOnline.ServerZonePath.Value);


                if (!File.Exists(ValheimOnline.ServerSafeZonePath.Value))
                {
                    Debug.Log($"Creating safe zone file at {ValheimOnline.ServerSafeZonePath.Value}");
                    string text = "# format: name x z radius\nDefaultSafeZone 0.0 0.0 5.0";
                    File.WriteAllText(ValheimOnline.ServerSafeZonePath.Value, text);
                }

                foreach (string text2 in File.ReadAllLines(ValheimOnline.ServerSafeZonePath.Value))
                {
                    if (!string.IsNullOrWhiteSpace(text2) && text2[0] != '#')
                    {
                        string[] array2 = text2.Split(Array.Empty<char>());
                        if (array2.Length != 4)
                        {
                            Debug.Log($"Safe zone {text2} is not correctly formatted.");
                        }
                        else
                        {
                            ServerState.SafeZone safeZone;
                            safeZone.name = array2[0];
                            safeZone.position.x = float.Parse(array2[1]);
                            safeZone.position.y = float.Parse(array2[2]);
                            safeZone.radius = float.Parse(array2[3]);
                            Debug.Log(string.Format("Loaded safe zone {0} ({1}, {2}) radius {3}", new object[]
                            {
                                safeZone.name,
                                safeZone.position.x,
                                safeZone.position.y,
                                safeZone.radius
                            }));
                            ServerState.SafeZones.Add(safeZone);
                        }
                    }
                }

                // Battle Zone
                if (!File.Exists(ValheimOnline.ServerBattleZonePath.Value))
                {
                    Debug.Log($"Creating battle zone file at {ValheimOnline.ServerBattleZonePath.Value}");
                    string text = "# format: name x z radius\nDefaultBattleZone 0.0 0.0 5.0";
                    File.WriteAllText(ValheimOnline.ServerBattleZonePath.Value, text);
                }

                foreach (string text2 in File.ReadAllLines(ValheimOnline.ServerBattleZonePath.Value))
                {
                    if (!string.IsNullOrWhiteSpace(text2) && text2[0] != '#')
                    {
                        string[] array2 = text2.Split(Array.Empty<char>());
                        if (array2.Length != 4)
                        {
                            Debug.Log($"Battle zone {text2} is not correctly formatted.");
                        }
                        else
                        {
                            ServerState.BattleZone battleZone;
                            battleZone.name = array2[0];
                            battleZone.position.x = float.Parse(array2[1]);
                            battleZone.position.y = float.Parse(array2[2]);
                            battleZone.radius = float.Parse(array2[3]);
                            Debug.Log(string.Format("Loaded Battle zone {0} ({1}, {2}) radius {3}", new object[]
                            {
                                battleZone.name,
                                battleZone.position.x,
                                battleZone.position.y,
                                battleZone.radius
                            }));
                            ServerState.BattleZones.Add(battleZone);
                        }
                    }
                }
            }
        }
    }
}
