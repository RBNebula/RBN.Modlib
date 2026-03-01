namespace RBN.Modlib;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class RbnModlibPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.rbplex.rbnmodlib";
    public const string PluginName = "RBN.Modlib";
    public const string PluginVersion = "1.3.0";

    private void Awake()
    {
        Game.GameHooks.Initialize(Logger);
    }

    private void OnDestroy()
    {
        Game.GameHooks.Shutdown();
    }
}
