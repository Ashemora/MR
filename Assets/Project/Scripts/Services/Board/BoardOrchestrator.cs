using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle.Energy;
using Project.Scripts.Tiles.Behaviours;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Grid;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Input;
using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Board
{
    public class BoardOrchestrator : IBoardOrchestrator, IDisposable
    {
        private const int ShuffleMaxAttempts = 10;


        private readonly IGridState _state;
        private readonly IGridView _view;
        private readonly IGridOperations _gridOps;
        private readonly IGravityHandler _gravity;
        private readonly IMatchFinder _matchFinder;
        private readonly ISwapInputHandler _swapHandler;
        private readonly IMoveChecker _moveChecker;
        private readonly CascadeEnergyConfig _cascadeEnergyConfig;
        private readonly IGameStateService _gameStateService;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IMoveBarService _moveBarService;
        private readonly EventBus _eventBus;
        private readonly SpecialTileResolver _specialTileResolver;
        private readonly SwapComboResolver _swapComboResolver;
        private readonly IBombRadiusModifierService _bombRadiusModifierService;
        private readonly ILineRuneModifierService _lineRuneModifierService;
        private readonly DebugConfig _debugConfig;
        private bool _isProcessing;


        public BoardOrchestrator(EventBus eventBus, IGridState state, IGridView view, IGridOperations gridOps,
            IGravityHandler gravity, IMatchFinder matchFinder, ISwapInputHandler swapHandler,
            IMoveChecker moveChecker, CascadeEnergyConfig cascadeEnergyConfig,
            IGameStateService gameStateService, IBoardRuntimeService boardRuntimeService, IMoveBarService moveBarService,
            SpecialTileResolver specialTileResolver, SwapComboResolver swapComboResolver,
            IBombRadiusModifierService bombRadiusModifierService, ILineRuneModifierService lineRuneModifierService,
            DebugConfig debugConfig)
        {
            _eventBus = eventBus;
            _state = state;
            _view = view;
            _gridOps = gridOps;
            _gravity = gravity;
            _matchFinder = matchFinder;
            _swapHandler = swapHandler;
            _moveChecker = moveChecker;
            _cascadeEnergyConfig = cascadeEnergyConfig;
            _gameStateService = gameStateService;
            _boardRuntimeService = boardRuntimeService;
            _moveBarService = moveBarService;
            _specialTileResolver = specialTileResolver;
            _swapComboResolver = swapComboResolver;
            _bombRadiusModifierService = bombRadiusModifierService;
            _lineRuneModifierService = lineRuneModifierService;
            _debugConfig = debugConfig;
        }

        public UniTask InitAsync()
        {
            _swapHandler.OnSwapRequested += OnSwapRequested;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _swapHandler.OnSwapRequested -= OnSwapRequested;
        }

        public async UniTask StartGame()
        {
            await _gridOps.PopulateGrid();
        }


        private void OnSwapRequested(SwapRequest request)
        {
            if (_isProcessing)
                return;

            if (false == CanAcceptInput())
                return;

            if (false == _moveBarService.HasMoves)
            {
                _eventBus.Publish(new SwapRejectedEvent());
                _swapHandler.NotifyBoardReady();
                return;
            }

            _isProcessing = true;
            _boardRuntimeService.BeginResolution();
            HandleSwapAsync(request).Forget();
        }

        private async UniTask HandleSwapAsync(SwapRequest request)
        {
            try
            {
                var fromTile = _view.GetTile(request.From);
                var toTile = _view.GetTile(request.To);

                if (false == fromTile || false == toTile)
                    return;

                var runtimeVersion = _boardRuntimeService.CaptureVersion();

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                var fromKind = fromTile.Config.Kind;
                var toKind = toTile.Config.Kind;
                var fromIsSpecial = fromKind.IsSpecial();
                var toIsSpecial = toKind.IsSpecial();
                var energySourcePositions = new EnergySourcePositionCollector();

                await _gridOps.SwapTiles(request.From, request.To);

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                var waves = new List<List<MatchResult>>();
                var matchEnergyByKind = new Dictionary<TileKind, float>();
                var specialActivationEnergyByKind = new Dictionary<TileKind, float>();
                var energySettings = _cascadeEnergyConfig.ToSettings();
                var moveUsed = false;

                if (fromIsSpecial && toIsSpecial)
                {
                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    _moveBarService.TryConsume();

                    var stateBefore = _state.GetGridState();
                    await ExecuteSwapCombo(fromKind, toKind, request.To, request.From, fromTile, toTile, runtimeVersion);

                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    var stateAfter = _state.GetGridState();

                    _eventBus.Publish(new BombActivatedEvent());
                    var comboMultiplier = energySettings.GetSpecialTileMultiplier(fromKind)
                                         * energySettings.GetSpecialTileMultiplier(toKind);
                    MatchEnergyRules.AccumulateGridDiffEnergy(stateBefore, stateAfter, specialActivationEnergyByKind,
                        comboMultiplier);
                    energySourcePositions.CollectFromGridDiff(stateBefore, stateAfter, _view);

                    await RunPostActivationFlow(waves, request.PivotPosition, runtimeVersion);

                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    moveUsed = true;
                }
                else if (fromIsSpecial || toIsSpecial)
                {
                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    _moveBarService.TryConsume();

                    Tile specialTile, partnerTile;
                    GridPoint specialFinalPos;

                    if (fromIsSpecial)
                    {
                        specialTile = fromTile;
                        specialFinalPos = request.To;
                        partnerTile = toTile;
                    }
                    else
                    {
                        specialTile = toTile;
                        specialFinalPos = request.From;
                        partnerTile = fromTile;
                    }

                    var stateBefore = _state.GetGridState();
                    await ActivateSpecialWithPartner(specialTile, partnerTile, specialFinalPos, runtimeVersion);

                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    var stateAfter = _state.GetGridState();

                    _eventBus.Publish(new BombActivatedEvent());
                    var specialMultiplier = energySettings.GetSpecialTileMultiplier(specialTile.Config.Kind);
                    MatchEnergyRules.AccumulateGridDiffEnergy(stateBefore, stateAfter, specialActivationEnergyByKind,
                        specialMultiplier);
                    energySourcePositions.CollectFromGridDiff(stateBefore, stateAfter, _view);

                    await RunPostActivationFlow(waves, request.PivotPosition, runtimeVersion);

                    if (false == CanContinueFlow(runtimeVersion))
                        return;

                    moveUsed = true;
                }
                else
                {
                    var matches = _matchFinder.FindMatches(_state.GetGridState());

                    if (matches.Count == 0)
                    {
                        if (CanContinueFlow(runtimeVersion))
                            await _gridOps.SwapTiles(request.To, request.From);

                        if (false == CanContinueFlow(runtimeVersion))
                            return;
                    }
                    else
                    {
                        if (false == CanContinueFlow(runtimeVersion))
                            return;

                        _moveBarService.TryConsume();
                        await ProcessMatchChain(matches, waves, request.PivotPosition, true, runtimeVersion, true);

                        if (false == CanContinueFlow(runtimeVersion))
                            return;

                        moveUsed = true;
                    }
                }

                MatchEnergyRules.AccumulateMatchEnergy(waves, energySettings, matchEnergyByKind);
                energySourcePositions.CollectFromMatches(waves, _view);

                var energyBreakdown = new EnergyGainBreakdown(matchEnergyByKind, specialActivationEnergyByKind);
                if (false == energyBreakdown.IsEmpty && CanContinueFlow(runtimeVersion))
                {
                    _eventBus.Publish(new EnergyGeneratedEvent(BattleSide.Player, energyBreakdown));
                    _eventBus.Publish(new EnergyGeneratedVisualEvent(energySourcePositions.Build()));
                    if (_debugConfig.LogCascades)
                        Debug.Log(BuildDetailedCascadeLog(waves, energySettings, energyBreakdown.TotalEnergyByKind));
                }

                if (moveUsed && CanContinueFlow(runtimeVersion))
                    _eventBus.Publish(new MoveUsedEvent());
            }
            finally
            {
                EnsureBoardStableAfterResolution();
                _boardRuntimeService.EndResolution();
                _isProcessing = false;
                _swapHandler.NotifyBoardReady();
            }
        }

        private async UniTask ActivateSpecialWithPartner(Tile specialTile, Tile partnerTile, GridPoint specialFinalPos, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            if (specialTile.Config.Kind == TileKind.Storm)
                specialTile.SetPayloadKind(partnerTile.Kind);

            await _gridOps.ActivateBySwap(specialFinalPos);
        }

        private async UniTask ExecuteSwapCombo(TileKind kindA, TileKind kindB,
            GridPoint posA, GridPoint posB, Tile tileA, Tile tileB, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            var comboType = _swapComboResolver.Resolve(kindA, kindB);

            switch (comboType)
            {
                case SwapComboType.StormStorm:
                    await ExecuteStormStormCombo(posA, posB, runtimeVersion);
                    break;

                case SwapComboType.StormBomb:
                {
                    var stormPos = kindA == TileKind.Storm ? posA : posB;
                    await ExecuteStormBombCombo(stormPos, runtimeVersion);
                    break;
                }

                case SwapComboType.StormLine:
                {
                    var stormPos = kindA == TileKind.Storm ? posA : posB;
                    await ExecuteStormLineCombo(stormPos, runtimeVersion);
                    break;
                }

                case SwapComboType.BombBomb:
                {
                    var doubleRadius = GetBombDoubleRadius(tileA, tileB, BattleSide.Player);
                    await ExecuteBombBombCombo(posA, posB, doubleRadius, runtimeVersion);
                    break;
                }

                case SwapComboType.BombLine:
                {
                    var bombPos = kindA == TileKind.Bomb ? posA : posB;
                    var bombTile = kindA == TileKind.Bomb ? tileA : tileB;
                    var radius = GetBombRadius(bombTile, BattleSide.Player);
                    var lineThicknessBonus = GetLineRuneThicknessBonus(BattleSide.Player);
                    await ExecuteBombLineCombo(posA, posB, bombPos, radius, lineThicknessBonus, runtimeVersion);
                    break;
                }

                case SwapComboType.LineLine:
                    await ExecuteLineLineCombo(posA, posB, GetLineRuneThicknessBonus(BattleSide.Player), runtimeVersion);
                    break;
            }
        }

        private async UniTask ExecuteStormStormCombo(GridPoint posA, GridPoint posB, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await UniTask.WhenAll(_gridOps.ConsumeTile(posA), _gridOps.ConsumeTile(posB));

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var allPositions = _state.GetAllOccupied();
            await _gridOps.ActivateTiles(allPositions);
        }

        private async UniTask ExecuteStormBombCombo(GridPoint stormPos, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await _gridOps.ConsumeTile(stormPos);

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var bombPositions = _state.GetAllOfKind(TileKind.Bomb);
            if (bombPositions.Count == 0)
                return;

            await _gridOps.ActivateTiles(bombPositions);
        }

        private async UniTask ExecuteStormLineCombo(GridPoint stormPos, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await _gridOps.ConsumeTile(stormPos);

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var linePositions = _state.GetAllOfKind(TileKind.LineRuneH);
            linePositions.AddRange(_state.GetAllOfKind(TileKind.LineRuneV));
            if (linePositions.Count == 0)
                return;

            await _gridOps.ActivateTiles(linePositions);
        }

        private async UniTask ExecuteBombBombCombo(GridPoint posA, GridPoint posB, int doubleRadius, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await UniTask.WhenAll(_gridOps.ConsumeTile(posA), _gridOps.ConsumeTile(posB));

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var explosion = new HashSet<GridPoint>(_state.GetNeighboursInRadius(posA, doubleRadius));
            var ps = _state.GetNeighboursInRadius(posB, doubleRadius);
            for (var i = 0; i < ps.Count; i++)
                explosion.Add(ps[i]);

            await _gridOps.ActivateTiles(new List<GridPoint>(explosion));
        }

        private async UniTask ExecuteBombLineCombo(GridPoint posA, GridPoint posB,
            GridPoint bombPos, int bombRadius, int lineThicknessBonus, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await UniTask.WhenAll(_gridOps.ConsumeTile(posA), _gridOps.ConsumeTile(posB));

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var area = new HashSet<GridPoint>(_state.GetNeighboursInRadius(bombPos, bombRadius));
            AddLineArea(area, bombPos, LineClearOrientation.Horizontal, lineThicknessBonus);
            AddLineArea(area, bombPos, LineClearOrientation.Vertical, lineThicknessBonus);

            await _gridOps.ActivateTiles(new List<GridPoint>(area));
        }

        private async UniTask ExecuteLineLineCombo(GridPoint posA, GridPoint posB, int lineThicknessBonus,
            int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await UniTask.WhenAll(_gridOps.ConsumeTile(posA), _gridOps.ConsumeTile(posB));

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var cross = new HashSet<GridPoint>();
            AddLineArea(cross, posA, LineClearOrientation.Horizontal, lineThicknessBonus);
            AddLineArea(cross, posA, LineClearOrientation.Vertical, lineThicknessBonus);

            await _gridOps.ActivateTiles(new List<GridPoint>(cross));
        }

        private void AddLineArea(HashSet<GridPoint> target, GridPoint origin, LineClearOrientation orientation,
            int lineThicknessBonus)
        {
            var positions = LineClearRules.GetAffectedPositions(_state, origin, orientation, lineThicknessBonus);
            for (var i = 0; i < positions.Count; i++)
                target.Add(positions[i]);
        }

        private async UniTask RunPostActivationFlow(List<List<MatchResult>> waves, GridPoint pivotPosition, int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            await _gravity.ApplyGravity();

            if (false == CanContinueFlow(runtimeVersion))
                return;

            await _gravity.SpawnNewTiles();

            if (false == CanContinueFlow(runtimeVersion))
                return;

            var chainMatches = _matchFinder.FindMatches(_state.GetGridState());
            if (chainMatches.Count > 0 && CanContinueFlow(runtimeVersion))
                await ProcessMatchChain(chainMatches, waves, pivotPosition, false, runtimeVersion, true);

            if (CanContinueFlow(runtimeVersion))
                await EnsureMovesAvailable(runtimeVersion);
        }

        private async UniTask ProcessMatchChain(List<MatchResult> matches, List<List<MatchResult>> waves, GridPoint pivotPosition, bool spawnSpecials,
            int runtimeVersion, bool publishMatchesCollected)
        {
            while (matches.Count > 0)
            {
                if (false == CanContinueFlow(runtimeVersion))
                    return;

                var cascadeLevel = waves.Count + 1;
                _eventBus.Publish(new MatchPlayedEvent(cascadeLevel));
                if (publishMatchesCollected)
                    PublishMatchesCollected(matches);

                waves.Add(new List<MatchResult>(matches));

                var specialPlacements = spawnSpecials && cascadeLevel == 1
                    ? _specialTileResolver.Resolve(matches, pivotPosition) : null;
                await _gridOps.RemoveMatches(matches, specialPlacements);

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                await _gravity.ApplyGravity();

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                await _gravity.SpawnNewTiles();

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                matches = _matchFinder.FindMatches(_state.GetGridState());
            }

            if (CanContinueFlow(runtimeVersion))
                await EnsureMovesAvailable(runtimeVersion);
        }

        private void PublishMatchesCollected(List<MatchResult> matches)
        {
            var countsByKind = new Dictionary<TileKind, int>();
            for (var i = 0; i < matches.Count; i++)
            {
                var kind = matches[i].TileKind;
                if (kind == TileKind.None)
                    continue;

                countsByKind.TryGetValue(kind, out var current);
                countsByKind[kind] = current + 1;
            }

            foreach (var pair in countsByKind)
                _eventBus.Publish(new BattleSideMatchesCollectedEvent(BattleSide.Player, pair.Key, pair.Value));
        }

        private async UniTask EnsureMovesAvailable(int runtimeVersion)
        {
            if (false == CanContinueFlow(runtimeVersion))
                return;

            if (_moveChecker.HasPossibleMoves())
                return;

            for (var i = 0; i < ShuffleMaxAttempts; i++)
            {
                await _gridOps.ShuffleGrid();

                if (false == CanContinueFlow(runtimeVersion))
                    return;

                if (_moveChecker.HasPossibleMoves())
                    return;
            }

            if (false == CanContinueFlow(runtimeVersion))
                return;

            _gridOps.ForceInjectMove();

            var immediateMatches = _matchFinder.FindMatches(_state.GetGridState());
            if (immediateMatches.Count > 0 && CanContinueFlow(runtimeVersion))
                await ProcessMatchChain(immediateMatches, new List<List<MatchResult>>(), GridPoint.Zero, false, runtimeVersion, false);
        }

        private bool CanAcceptInput()
        {
            return _gameStateService.IsPlaying && _boardRuntimeService.CanAcceptInput;
        }

        private bool CanContinueFlow(int runtimeVersion)
        {
            if (false == _gameStateService.IsPlaying)
                return false;

            if (false == _boardRuntimeService.CanContinueResolution)
                return false;

            return _boardRuntimeService.IsCurrent(runtimeVersion);
        }

        private int GetBombRadius(Tile tile, BattleSide side)
        {
            var baseRadius = (tile.Config.Behaviour as BombTileBehaviour)?.Radius ?? 1;
            return BombRadiusRules.GetEffectiveRadius(baseRadius, _bombRadiusModifierService.GetBombRadiusBonus(side));
        }

        private int GetBombDoubleRadius(Tile tileA, Tile tileB, BattleSide side)
        {
            var bomb = tileA.Config.Behaviour as BombTileBehaviour
                    ?? tileB.Config.Behaviour as BombTileBehaviour;

            var baseRadius = bomb?.DoubleRadius ?? 2;
            
            return BombRadiusRules.GetEffectiveRadius(baseRadius, _bombRadiusModifierService.GetBombRadiusBonus(side));
        }

        private int GetLineRuneThicknessBonus(BattleSide side)
        {
            return _lineRuneModifierService.GetLineRuneThicknessBonus(side);
        }

        private void EnsureBoardStableAfterResolution()
        {
            if (_boardRuntimeService.IsStoppingForBurndown || _boardRuntimeService.IsFrozen)
                return;

            var issues = CollectBoardStabilityIssues();
            if (issues.Count == 0)
                return;

            var repaired = _gridOps.RepairBoardState();
            var remainingIssues = CollectBoardStabilityIssues();
            if (remainingIssues.Count == 0)
            {
                Debug.LogWarning($"Board state was inconsistent after resolution and required repair. Issues: {string.Join(" | ", issues)}");
                return;
            }

            var repairSuffix = repaired ? " Repair attempt did not fully resolve the state." : string.Empty;
            Debug.LogError($"Board state is inconsistent after resolution.{repairSuffix} Issues: {string.Join(" | ", remainingIssues)}");
        }

        private List<string> CollectBoardStabilityIssues()
        {
            var issues = new List<string>();

            if (_state is GridState gridState && gridState.ScheduledRemovals.Count > 0)
                issues.Add($"Pending scheduled removals: {gridState.ScheduledRemovals.Count}");

            const float positionToleranceSqr = 0.0001f;
            for (var x = 0; x < _state.Width; x++)
                for (var y = 0; y < _state.Height; y++)
                {
                    var pos = new GridPoint(x, y);
                    var tile = _view.GetTile(pos);
                    var kind = _state.GetKind(pos);

                    if (false == tile && kind != TileKind.None)
                    {
                        issues.Add($"Missing tile at {pos} for kind {kind}");
                        continue;
                    }

                    if (tile && kind == TileKind.None)
                    {
                        issues.Add($"State hole at {pos} while tile {tile.Kind} exists");
                        continue;
                    }

                    if (false == tile)
                        continue;

                    if (tile.Kind != kind)
                        issues.Add($"Kind mismatch at {pos}: state={kind}, tile={tile.Kind}");

                    if (tile.GridPosition != pos)
                        issues.Add($"GridPosition mismatch at {pos}: tile reports {tile.GridPosition}");

                    var expectedWorldPos = _view.GridToWorld(pos);
                    if ((tile.transform.position - expectedWorldPos).sqrMagnitude > positionToleranceSqr)
                        issues.Add($"World position mismatch at {pos}");
                }

            var immediateMatches = _matchFinder.FindMatches(_state.GetGridState());
            if (immediateMatches.Count > 0)
                issues.Add($"Immediate matches remain after resolution: {immediateMatches.Count}");

            return issues;
        }

        private static string BuildDetailedCascadeLog(List<List<MatchResult>> waves, CascadeEnergySettings settings,
            IReadOnlyDictionary<TileKind, float> energyByKind)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Energy] === Cascade report ===");

            for (var i = 0; i < waves.Count; i++)
            {
                var matches = waves[i];
                var cascadeMult = MatchEnergyRules.GetCascadeMultiplier(i, settings);
                var multiMatchMult = MatchEnergyRules.GetMultiMatchMultiplier(matches.Count, settings);

                sb.AppendLine($"  Wave {i + 1}  cascade×{cascadeMult:F2}  multiMatch×{multiMatchMult:F2}  ({matches.Count} match(es))");

                for (var j = 0; j < matches.Count; j++)
                {
                    var match = matches[j];
                    if (false == match.TileKind.IsColor())
                        continue;

                    var shapeMult = MatchEnergyRules.GetShapeMultiplier(match.Shape, settings);
                    var raw = MatchEnergyRules.CalculateMatchEnergy(match, i, matches.Count, settings);

                    sb.AppendLine($"    [{match.TileKind}] {match.Positions.Count} tiles  shape={match.Shape}×{shapeMult:F2}  → +{raw:F2}");
                }
            }

            sb.AppendLine("  == Per-kind totals ==");
            var total = 0f;
            foreach (var pair in energyByKind)
            {
                if (pair.Value <= 0f)
                    continue;

                sb.AppendLine($"  {pair.Key}: +{pair.Value:F2}");
                total += pair.Value;
            }

            sb.Append($"  Total energy generated: +{total:F2}");
            
            return sb.ToString();
        }
    }
}