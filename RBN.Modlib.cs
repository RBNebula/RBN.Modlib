namespace RBN.Modlib;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public sealed class RbnModlibPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Game.GameHooks.Initialize(Logger);
    }

    private void OnDestroy()
    {
        Game.GameHooks.Shutdown();
    }
}
