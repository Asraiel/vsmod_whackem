namespace ModConfig
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;

    public class ModConfig
    {
        public static ModConfig Current { get; set; }

#pragma warning disable IDE1006 // Naming Styles
        public string ShepherdsCrookAffectedEntitiesDesc = "The entities affected by the Shepherd's Crook. Tested by checking whether one of the configured values is part of the entity code.";
        public string[] ShepherdsCrookAffectedEntities = new string[] { "bighorn", "pig", "chicken" };
        public string ShepherdsCrookAngerDurabilityCostDesc = "The durability cost of the anger mode.";
        public int ShepherdsCrookAngerDurabilityCost = 1;
        public string ShepherdsCrookScareDurabilityCostDesc = "The durability cost of the scare mode.";
        public int ShepherdsCrookScareDurabilityCost = 1;
        public string ShepherdsCrookCalmDurabilityCostDesc = "The durability cost of the calm mode.";
        public int ShepherdsCrookCalmDurabilityCost = 1;

        public static string filename = "whackem.json";
#pragma warning restore IDE1006 // Naming Styles

        public static void Load(ICoreAPI api)
        {
            ModConfig config = null;
            var logname = "whackem-mod-logs.txt";

            try
            {
                for (var attempts = 1; attempts < 4; attempts++)
                {
                    try
                    {
                        config = api.LoadModConfig<ModConfig>(filename);
                    }
                    catch (JsonReaderException e)
                    {
                        var badLineNum = e.LineNumber;
                        api.Logger.Error($"[whackEmMod Error] Unable to parse config JSON. Attempt {attempts} to salvage the file...");
                        var configFilepath = Path.Combine(GamePaths.ModConfig, filename);
                        var badConfigFilepath = Path.Combine(GamePaths.Logs, "ERROR_" + filename);
                        var translocatorlocatorLogFilepath = Path.Combine(GamePaths.Logs, logname);
                        if (attempts == 1)
                        {
                            if (File.Exists(badConfigFilepath))
                            {
                                File.Delete(badConfigFilepath);
                            }
                            File.Copy(configFilepath, badConfigFilepath);
                            File.WriteAllText(translocatorlocatorLogFilepath, e.ToString());
                        }
                        if (attempts != 3)
                        {
                            var lines = new List<string>(File.ReadAllLines(configFilepath));
                            lines.RemoveAt(badLineNum - 1);
                            File.WriteAllText(configFilepath, string.Join("\n", lines.ToArray()));
                        }
                    }
                }
                try
                {
                    config = api.LoadModConfig<ModConfig>(filename);
                }
                catch (JsonReaderException)
                {
                    api.Logger.Error("[whackEmMod Error] Unable to salvage config.");
                }
            }
            catch (System.Exception e)
            {
                api.Logger.Error("[whackEmMod Error] Something went really wrong with reading the config file.");
                File.WriteAllText(Path.Combine(GamePaths.Logs, logname), e.ToString());
            }

            if (config == null)
            {
                api.Logger.Warning("[whackEmMod Warning] Unable to load valid config file. Generating default config.");
                config = new ModConfig();
            }
            Save(api, config);
            Current = config;
        }
        public static void Save(ICoreAPI api, ModConfig config)
        {
            api.StoreModConfig(config, filename);
        }
    }
}

