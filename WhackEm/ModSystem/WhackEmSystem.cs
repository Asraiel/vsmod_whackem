namespace WhackEm.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;
    using ModConfig;
    using ShepardsStick;

    public class WhackEmSystem : ModSystem
    {

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Debug("[WhackEm] StartServer");
            base.Start(api);

            ModConfig.Load(api);
        }

        public override void Start(ICoreAPI api)
        {
            api.Logger.Debug("[WhackEm] Start");
            base.Start(api);

            api.RegisterItemClass("ItemShepardsStick", typeof(ItemShepardsStick));
        }
    }
}
