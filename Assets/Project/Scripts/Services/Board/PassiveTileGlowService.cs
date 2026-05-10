using System;
using System.Collections.Generic;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Grid;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Services.Board
{
    public class PassiveTileGlowService : IDisposable
    {
        private readonly IGridView _gridView;
        private readonly GridConfig _gridConfig;
        private readonly IBuffService _buffService;
        private readonly TileKindPaletteConfig _palette;
        private readonly IDisposable _passiveActivatedSubscription;
        private readonly IDisposable _passiveDisabledSubscription;
        private readonly IDisposable _buffsChangedSubscription;
        private readonly IDisposable _moveUsedSubscription;


        public PassiveTileGlowService(EventBus eventBus, IGridView gridView, GridConfig gridConfig,
            IBuffService buffService, TileKindPaletteConfig palette)
        {
            _gridView = gridView;
            _gridConfig = gridConfig;
            _buffService = buffService;
            _palette = palette;

            _passiveActivatedSubscription = eventBus.Subscribe<HeroPassiveActivatedEvent>(_ => Refresh());
            _passiveDisabledSubscription = eventBus.Subscribe<HeroPassiveDisabledEvent>(_ => Refresh());
            _buffsChangedSubscription = eventBus.Subscribe<BuffsChangedEvent>(_ => Refresh());
            _moveUsedSubscription = eventBus.Subscribe<MoveUsedEvent>(_ => Refresh());
        }

        public void Dispose()
        {
            ClearAll();
            _passiveActivatedSubscription.Dispose();
            _passiveDisabledSubscription.Dispose();
            _buffsChangedSubscription.Dispose();
            _moveUsedSubscription.Dispose();
        }

        public void Refresh()
        {
            var activeKinds = CollectActiveBoostedKinds();

            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _gridView.GetTile(new GridPoint(x, y));
                    if (!tile)
                        continue;

                    var active = activeKinds.Contains(tile.Kind);
                    var color = active ? _palette.GetColor(tile.Kind, Color.white) : Color.white;
                    tile.SetGlowActive(active, color);
                }
        }

        private HashSet<TileKind> CollectActiveBoostedKinds()
        {
            var result = new HashSet<TileKind>();
            foreach (TileKind kind in Enum.GetValues(typeof(TileKind)))
            {
                if (false == kind.IsColor())
                    continue;

                if (_buffService.HasMatchEnergyBuff(Project.Scripts.Shared.Heroes.BattleSide.Player, kind))
                    result.Add(kind);
            }

            return result;
        }

        private void ClearAll()
        {
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _gridView.GetTile(new GridPoint(x, y));
                    if (tile)
                        tile.SetGlowActive(false, Color.white);
                }
        }
    }
}