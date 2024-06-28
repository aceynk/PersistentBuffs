using Force.DeepCloner;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PersistentBuffs;

public class ModEntry : Mod
{
    public static void Log(string v)
    {
        _log.Log(v, LogLevel.Debug);
    }
    
    public static IMonitor _log = null!;
    //public NetStringList DayEndBuffs = null!;
    //public Dictionary<string, int> DayEndBuffRemaining = new();

    public string DEBname = "aceynk.PersistentBuffs.DayEndBuffs";
    public string DEBRname = "aceynk.PersistentBuffs.DayEndBuffRemaining";

    public NetString CondenseBuffs(NetStringList value)
    {
        if (value == new NetStringList())
        {
            return new NetString();
        }
        
        string output = string.Join("/", value);

        return new NetString(output);
    }

    public NetStringList DecodeBuffs(NetString value)
    {
        if (value == new NetString())
        {
            return new NetStringList();
        }
        
        NetStringList output = new NetStringList(((string)value).Split("/"));

        return output;
    }

    public NetString CondenseBuffRemaining(Dictionary<string, int> value)
    {
        if (value == new Dictionary<string, int>())
        {
            return new NetString();
        }
        
        List<string> output = value.Keys.Select(v => v + ":" + value[v]).ToList();

        return new NetString(string.Join("/", output));
    }

    public Dictionary<string, int> DecodeBuffRemaining(NetString value)
    {
        if (value == new NetString())
        {
            return new Dictionary<string, int>();
        }
        
        List<string> itemList = value.ToString().Split("/").ToList();
        Dictionary<string, int> output = new();

        foreach (string item in itemList)
        {
            if (item == "") continue;
            string key = item.Split(":")[0];
            int val = int.Parse(item.Split(":")[1]);

            output[key] = val;
        }

        return output;
    }

    public override void Entry(IModHelper helper)
    {
        //Config = Helper.ReadConfig<ModConfig>();
        _log = Monitor;

        Helper.Events.Content.AssetRequested += OnAssetRequested;
        Helper.Events.GameLoop.DayEnding += OnDayEnding;
        Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private static List<string> UsedIds(Dictionary<string, bool> idBoolDict)
    {
        return idBoolDict.Keys.Where(v => idBoolDict[v]).ToList();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (!Game1.player.modData.Keys.Contains(DEBname))
        {
            Game1.player.modData[DEBname] = new NetString();
        }

        if (!Game1.player.modData.Keys.Contains(DEBRname))
        {
            Game1.player.modData[DEBRname] = new NetString();
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        NetStringList DayEndBuffs = DecodeBuffs(new NetString(Game1.player.modData[DEBname]));
        Dictionary<string, int> DayEndBuffRemaining =
            DecodeBuffRemaining(new NetString(Game1.player.modData[DEBRname]));
        
        NetStringList curBuffIds = Game1.player.buffs.AppliedBuffIds;
        Dictionary<string, bool> protectedIdsDict = Game1.content.Load<Dictionary<string, bool>>("aceynk.PersistentBuffs/PersistentBuffIds");

        List<string> protectedIds = UsedIds(protectedIdsDict);
        
        List<string> curBuffIdsList = curBuffIds.Where(v => protectedIds.Contains(v)).ToList();

        foreach (string buffId in curBuffIdsList)
        {
            DayEndBuffRemaining[buffId] = Game1.player.buffs.AppliedBuffs[buffId].millisecondsDuration;
        }

        DayEndBuffs = curBuffIds.DeepClone();

        Game1.player.modData[DEBname] = CondenseBuffs(DayEndBuffs);
        Game1.player.modData[DEBRname] = CondenseBuffRemaining(DayEndBuffRemaining);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        NetStringList DayEndBuffs = new();
        Dictionary<string, int> DayEndBuffRemaining = new();
        
        try
        {
            DayEndBuffs = DecodeBuffs(new NetString(Game1.player.modData[DEBname]));
            DayEndBuffRemaining =
                DecodeBuffRemaining(new NetString(Game1.player.modData[DEBRname]));
        }
        catch
        {
            Log("Failed to fetch modData on day start.");
        }

        if (DayEndBuffs == null)
        {
            return;
        }
        
        if (DayEndBuffs != new NetStringList())
        {
            foreach (string buffId in DayEndBuffs)
            {
                if (buffId == "") continue;
                Buff thisBuff = new Buff(buffId, duration: DayEndBuffRemaining[buffId]);
                Game1.player.applyBuff(thisBuff);
            }
        }

        DayEndBuffs.Clear();
        DayEndBuffRemaining.Clear();
        
        Game1.player.modData[DEBname] = CondenseBuffs(DayEndBuffs);
        Game1.player.modData[DEBRname] = CondenseBuffRemaining(DayEndBuffRemaining);
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("aceynk.PersistentBuffs/PersistentBuffIds"))
        {
            e.LoadFrom(() => new Dictionary<string, bool>(), AssetLoadPriority.High);
        }
    }
}