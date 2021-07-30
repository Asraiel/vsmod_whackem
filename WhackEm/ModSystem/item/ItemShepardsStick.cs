namespace ShepardsStick
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Util;
    using System.Text;
    using Cairo;
#pragma warning disable IDE0005
    using System.Linq;
#pragma warning restore IDE0005
    using Vintagestory.API.Server;
    using WhackEm.ModSystem.Entity.Behavior;

    public class ItemShepardsStick : Item
    {
        // TODOs
        // - model
        // - recipe
        // - particels
        // - sounds
        // - toolmode icons:
        //      anger ò.ó
        //      scare ó.ò
        //      calm  u.u
        // - check generation > 0

        private SkillItem[] toolModes;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (entitySel != null)
            {
                if (this.IsLifestock(entitySel))
                {
                    var entity = entitySel.Entity;
                    if (entity.Alive)
                    {
                        var player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                        var toolMode = this.GetToolMode(slot, player, blockSel);
                        var modConfig = ModConfig.ModConfig.Current;
                        var cost = this.GetCost(modConfig, toolMode);
                        if (byEntity.Api.Side == EnumAppSide.Server)
                        {
                            if (!entity.HasBehavior<BehaviorShepardsStick>())
                            {
                                entity.AddBehavior(new BehaviorShepardsStick(entity));
                            }
                            var beh = entity.GetBehavior<BehaviorShepardsStick>();
                            if (beh != null)
                            {
                                beh.OnHitByShepardsStick(toolMode);
                            }
                            else
                            {
                                this.PrintMessage(byEntity, "Behavior not found!");
                            }
                            this.ApplyDurabilityDamageServer(cost, slot, byEntity);
                        }
                        else
                        {
                            this.ApplyDurabilityDamageClient(cost, slot, byEntity);
                        }
                    }
                }
                handling = EnumHandHandling.PreventDefaultAction;
            }
        }

        private int GetCost(ModConfig.ModConfig modConfig, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    return modConfig.ShepardsStickAngerDurabilityCost;
                case 1:
                    return modConfig.ShepardsStickScareDurabilityCost;
                case 2:
                default:
                    return modConfig.ShepardsStickCalmDurabilityCost;
            }
        }

        private void ApplyDurabilityDamageServer(int durabilityDamage, ItemSlot slot, EntityAgent byEntity)
        {
            if (slot != null && slot.Itemstack != null)
            {
                var capi = byEntity.Api as ICoreServerAPI;
                var world = capi.World as IWorldAccessor;
                var serverplayer = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer;

                slot.Itemstack.Collectible.DamageItem(world, byEntity, serverplayer.InventoryManager.ActiveHotbarSlot, durabilityDamage);
                slot.MarkDirty();
            }
        }

        private void ApplyDurabilityDamageClient(int durabilityDamage, ItemSlot slot, EntityAgent byEntity)
        {
            if (slot != null && slot.Itemstack != null)
            {
                var capi = byEntity.Api as ICoreClientAPI;
                var world = capi.World as IWorldAccessor;
                var player = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

                slot.Itemstack.Collectible.DamageItem(world, byEntity, player.InventoryManager.ActiveHotbarSlot, durabilityDamage);
                slot.MarkDirty();
            }
        }

        private void PrintMessage(EntityAgent byEntity, string message)
        {
            if (byEntity.Api.Side == EnumAppSide.Server)
            {
                var byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer;
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[Server] [WhackEm Mod] " + message, EnumChatType.Notification);
            }
            else
            {
                var capi = byEntity.Api as ICoreClientAPI;
                capi.ShowChatMessage("[Client] [WhackEm Mod] " + message);
            }
        }

        private bool IsLifestock(EntitySelection entitySel)
        {
            var modConfig = ModConfig.ModConfig.Current;
            var entities = modConfig.ShepardsStickAffectedEntities;

            foreach (var entity in entities)
            {
                if (entitySel.Entity.Code.Path.Contains(entity))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.toolModes = ObjectCacheUtil.GetOrCreate(api, "shepardsStickToolModes", () =>
            {
                SkillItem[] modes;

                modes = new SkillItem[3];
                modes[0] = new SkillItem() { Code = new AssetLocation("anger"), Name = Lang.Get("whackem:shepardsstick_angermode") };
                modes[1] = new SkillItem() { Code = new AssetLocation("scare"), Name = Lang.Get("whackem:shepardsstick_scaremode") };
                modes[2] = new SkillItem() { Code = new AssetLocation("calm"), Name = Lang.Get("whackem:shepardsstick_calmmode") };

                if (api is ICoreClientAPI capi)
                {
                    _ = modes[0].WithIcon(capi, (cr, x, y, w, h, c) => DrawIcons(cr, x, y, w, h, c, 0));
                    _ = modes[1].WithIcon(capi, (cr, x, y, w, h, c) => DrawIcons(cr, x, y, w, h, c, 1));
                    _ = modes[2].WithIcon(capi, (cr, x, y, w, h, c) => DrawIcons(cr, x, y, w, h, c, 2));
                }

                return modes;
            });
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return this.toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(this.toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            for (var i = 0; this.toolModes != null && i < this.toolModes.Length; i++)
            {
                this.toolModes[i]?.Dispose();
            }
        }

        private void SpawnParticles(EntitySelection entitySel, EntityAgent byEntity)
        {
            //var byPlayer = (byEntity as EntityPlayer).Player;
            //var pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition.ToVec3f().ToVec3d());
            //byEntity.World.SpawnCubeParticles(blockSel.Position, pos, 0.5f, 8, 0.7f, byPlayer);
        }

        private void PlaySound(int toolmode, EntityAgent byEntity)
        {
            switch (toolmode)
            {
                case 0:
                    this.PlaySoundAnger(byEntity);
                    break;
                case 1:
                    this.PlaySoundScare(byEntity);
                    break;
                case 2:
                default:
                    this.PlaySoundCalm(byEntity);
                    break;
            }
        }

        private void PlaySoundScare(EntityAgent byEntity)
        {
            //var pos = byEntity.Pos;
            //byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/padlock"), pos.X, pos.Y, pos.Z, null);
        }

        private void PlaySoundAnger(EntityAgent byEntity)
        {
            //var pos = byEntity.Pos;
            //byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), pos.X, pos.Y, pos.Z, null);
        }

        private void PlaySoundCalm(EntityAgent byEntity)
        {
            //var pos = byEntity.Pos;
            //byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), pos.X, pos.Y, pos.Z, null);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            _ = dsc.AppendLine("\n" + this.GetFlavorText());
        }

        private string GetFlavorText()
        {
            // TODO
            return "";
        }

#pragma warning disable IDE0060
        private static void DrawIcons(Context cr, int x, int y, float width, float height, double[] rgba, int toolMode)
#pragma warning restore IDE0060
        {
            cr.SetSourceRGB(1D, 1D, 1D);
            switch (toolMode)
            {
                case 0: // TODO ò.ó
                    cr.MoveTo(11, 24);
                    cr.LineTo(37, 10);
                    cr.LineTo(37, 38);
                    cr.LineTo(11, 24);
                    cr.Fill();
                    break;
                case 1: // TODO ó.ò
                    cr.Rectangle(16, 16, 16, 16);
                    cr.Fill();
                    break;
                case 2: // TODO u.u
                default:
                    cr.Rectangle(16, 16, 16, 16);
                    cr.Fill();
                    break;
            }
        }
    }
}
