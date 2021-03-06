// -----------------------------------------------------------------------
// <copyright file="EventHandlers.cs" company="Build">
// Copyright (c) Build. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace ArmorShield
{
    using System.Collections.Generic;
    using ArmorShield.Events.EventArgs;
    using ArmorShield.Models;
    using Exiled.API.Features;
    using Exiled.API.Features.Items;
    using InventorySystem.Items.Armor;
    using PlayerStatsSystem;

    /// <summary>
    /// General event handlers.
    /// </summary>
    public class EventHandlers
    {
        private readonly Dictionary<ushort, AhpStat.AhpProcess> ahpProcesses = new();
        private readonly Plugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlers"/> class.
        /// </summary>
        /// <param name="plugin">The <see cref="Plugin{TConfig}"/> class reference.</param>
        public EventHandlers(Plugin plugin) => this.plugin = plugin;

        /// <inheritdoc cref="Events.Handlers.Player.OnItemAdded"/>
        public void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (TryGetBodyArmor(ev.Player, out BodyArmor bodyArmor))
                UpdateShield(ev.Player, bodyArmor);
        }

        /// <inheritdoc cref="Events.Handlers.Player.OnRemovingItem"/>
        public void OnRemovingItem(RemovingItemEventArgs ev)
        {
            if (ev.Item is null)
                return;

            if (ahpProcesses.TryGetValue(ev.Item.Serial, out AhpStat.AhpProcess ahpProcess))
            {
                ev.Player.ReferenceHub.playerStats.GetModule<AhpStat>().ServerKillProcess(ahpProcess.KillCode);
                ahpProcesses.Remove(ev.Item.Serial);
            }

            if (TryGetBodyArmor(ev.Player, out BodyArmor bodyArmor, ev.Item))
                UpdateShield(ev.Player, bodyArmor);
        }

        public void OnAnyPlayerDamaged(ReferenceHub target, DamageHandlerBase handler)
        {
            Player player = Player.Get(target);
            if (!TryGetBodyArmor(player, out BodyArmor bodyArmor) ||
                !ahpProcesses.TryGetValue(bodyArmor.ItemSerial, out AhpStat.AhpProcess ahpProcess) ||
                !plugin.Config.ArmorShields.TryGetValue(bodyArmor.ItemTypeId, out ConfiguredAhp configuredAhp))
                return;

            ahpProcess.SustainTime = configuredAhp.Sustain;
        }

        private static bool TryGetBodyArmor(Player player, out BodyArmor armor, Item toExclude = null)
        {
            foreach (Item item in player.Items)
            {
                if (toExclude != item && item.Base is BodyArmor bodyArmor)
                {
                    armor = bodyArmor;
                    return true;
                }
            }

            armor = null;
            return false;
        }

        private void UpdateShield(Player player, BodyArmor armor)
        {
            if (!ahpProcesses.ContainsKey(armor.ItemSerial) && plugin.Config.ArmorShields.TryGetValue(armor.ItemTypeId, out ConfiguredAhp configuredAhp))
                ahpProcesses.Add(armor.ItemSerial, configuredAhp.AddTo(player));
        }
    }
}