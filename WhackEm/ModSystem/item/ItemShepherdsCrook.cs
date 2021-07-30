namespace ShepherdsCrook
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Util;
    using System.Text;
    using Cairo;
#pragma warning disable IDE0005
#pragma warning restore IDE0005
    using Vintagestory.API.Server;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.GameContent;
    using System.Collections.Generic;

    public class ItemShepherdsCrook : Item
    {
        // TODOs
        // - particles?
        // - sounds?

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


                        if (this.IsGenHighEnough(modConfig, entity, toolMode))
                        {
                            if (byEntity.Api.Side == EnumAppSide.Server)
                            {
                                this.OnHitByShepherdsCrook(entity, toolMode);
                                this.ApplyDurabilityDamageServer(cost, slot, byEntity);
                            }
                            else
                            {
                                this.ApplyDurabilityDamageClient(cost, slot, byEntity);
                            }
                            handling = EnumHandHandling.PreventDefaultAction;
                        }
                        else
                        {
                            this.PrintMessageClient(byEntity, Lang.Get("whackem:shepherdscrook_gentoolow"));
                        }
                    }
                }
            }
        }

        private bool IsGenHighEnough(ModConfig.ModConfig modConfig, Entity entity, int toolMode)
        {
            var gen = (entity as EntityAgent).WatchedAttributes.GetInt("generation");
            var minGen = this.GetMinGen(modConfig, toolMode);
            return gen >= minGen;
        }

        private int GetMinGen(ModConfig.ModConfig modConfig, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    return modConfig.ShepherdsCrookAngerMinGen;
                case 1:
                    return modConfig.ShepherdsCrookScareMinGen;
                case 2:
                default:
                    return modConfig.ShepherdsCrookCalmMinGen;
            }
        }

        private void OnHitByShepherdsCrook(Entity entity, int toolMode)
        {
            var behEmo = entity.GetBehavior<EntityBehaviorEmotionStates>();
            if (behEmo != null)
            {
                switch (toolMode)
                {
                    case 0: // anger
                        behEmo.TryTriggerState("aggressiveondamage", 0);
                        break;
                    case 1: // scare
                        behEmo.TryTriggerState("fleeondamage", 0);
                        break;
                    case 2: // calm
                        this.KillAllActiveStates(behEmo);
                        break;
                    default:
                        break;
                }
            }
        }

        private void KillAllActiveStates(EntityBehaviorEmotionStates behEmo)
        {
            for (var i = 0; i < 4; i++)
            {
                try
                {
                    var dur = behEmo.ActiveStatesById[i];
                    if (dur > 0)
                    {
                        behEmo.ActiveStatesById[i] = 0;
                    }
                }
                catch (KeyNotFoundException)
                {
                    // ignore
                }
            }
        }

        private int GetCost(ModConfig.ModConfig modConfig, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    return modConfig.ShepherdsCrookAngerDurabilityCost;
                case 1:
                    return modConfig.ShepherdsCrookScareDurabilityCost;
                case 2:
                default:
                    return modConfig.ShepherdsCrookCalmDurabilityCost;
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

        private void PrintDebugMessageBothSides(EntityAgent byEntity, string message)
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

        private void PrintMessageClient(EntityAgent byEntity, string message)
        {
            if (byEntity.Api.Side != EnumAppSide.Server)
            {
                var capi = byEntity.Api as ICoreClientAPI;
                capi.ShowChatMessage(message);
            }
        }

        private bool IsLifestock(EntitySelection entitySel)
        {
            var modConfig = ModConfig.ModConfig.Current;
            var entities = modConfig.ShepherdsCrookAffectedEntities;

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
            this.toolModes = ObjectCacheUtil.GetOrCreate(api, "shepherdsCrookToolModes", () =>
            {
                SkillItem[] modes;

                modes = new SkillItem[3];
                modes[0] = new SkillItem() { Code = new AssetLocation("anger"), Name = Lang.Get("whackem:shepherdscrook_angermode") };
                modes[1] = new SkillItem() { Code = new AssetLocation("scare"), Name = Lang.Get("whackem:shepherdscrook_scaremode") };
                modes[2] = new SkillItem() { Code = new AssetLocation("calm"), Name = Lang.Get("whackem:shepherdscrook_calmmode") };

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
            return Lang.Get("whackem:shepherdscrook_flavortext");
        }

#pragma warning disable IDE0060
        private static void DrawIcons(Context cr, int x, int y, float width, float height, double[] rgba, int toolMode)
#pragma warning restore IDE0060
        {
            cr.SetSourceRGB(1D, 1D, 1D);
            switch (toolMode)
            {
                // canvas is 48x48
                case 0: // ò.ó
                    // left eye
                    cr.Arc(12, 26, 8, 0, 7D);
                    cr.Stroke();
                    // right eye
                    cr.Arc(36, 26, 8, 0, 7D);
                    cr.Stroke();
                    // nose
                    cr.Arc(24.5, 34, 1, 0, 7D);
                    cr.Stroke();
                    // left eyebrow
                    cr.MoveTo(18, 14);
                    cr.RelLineTo(-10, -8);
                    cr.Stroke();
                    // right eyebrow
                    cr.MoveTo(30, 14);
                    cr.RelLineTo(10, -8);
                    cr.Stroke();
                    break;
                case 1: // ó.ò
                    // left eye
                    cr.Arc(12, 26, 8, 0, 7D);
                    cr.Stroke();
                    // right eye
                    cr.Arc(36, 26, 8, 0, 7D);
                    cr.Stroke();
                    // nose
                    cr.Arc(24.5, 34, 1, 0, 7D);
                    cr.Stroke();
                    // left eyebrow
                    cr.MoveTo(18, 6);
                    cr.RelLineTo(-10, 8);
                    cr.Stroke();
                    // right eyebrow
                    cr.MoveTo(30, 6);
                    cr.RelLineTo(10, 8);
                    cr.Stroke();
                    break;
                case 2: // TODO u.u
                default:
                    // left eye
                    cr.MoveTo(5, 18);
                    cr.RelLineTo(0, 16);
                    cr.RelLineTo(14, 0);
                    cr.RelLineTo(0, -16);
                    cr.Stroke();
                    // right eye
                    cr.MoveTo(29, 18);
                    cr.RelLineTo(0, 16);
                    cr.RelLineTo(14, 0);
                    cr.RelLineTo(0, -16);
                    cr.Stroke();
                    // nose
                    cr.Arc(24.5, 34, 1, 0, 7D);
                    cr.Stroke();
                    break;
            }
        }
    }
}
