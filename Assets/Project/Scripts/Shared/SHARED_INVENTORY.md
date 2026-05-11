# Shared Inventory

This document captures the current `Shared` structure before folder cleanup.
It is a migration aid: keep behavior changes out of structural moves and update this file as modules are relocated.

## Current Folder Counts

- `Abilities`: 7 files
- `ActivationConditions`: 7 files
- `BattleFlow`: 10 files
- `BattleSetup`: 2 files
- `Bot`: 2 files
- `Buffs`: 8 files
- `Energy`: 7 files
- `Grid`: 14 files
- `GroupDefense`: 3 files
- `Heroes`: 6 files
- `MoveBar`: 4 files
- `Passives`: 3 files
- `Targeting`: 9 files
- `Tiles`: 3 files
- `Units`: 5 files
- root `Shared`: 1 file

Total: 91 C# files.

## Root Types

The root namespace `Project.Scripts.Shared` currently contains one portable vector type:

- `SharedVector3`

Migration note: `SharedVector3` should either stay as a truly global primitive or move to a dedicated primitives folder.

## Folder Inventory

### Abilities

Namespace: `Project.Scripts.Shared.Abilities`

- `ActiveAbilityDefinition`
- `AbilityAdditionalTargetRules`
- `PassiveAbilityDefinition`
- `AbilityEffectEntryDefinition`
- `AbilityTargetCandidate`
- `AbilityTargetRules`
- `CooldownRules`
- `DirectActionDefinition`
- `BuffApplicationDefinition`
- `DirectActionKind`
- `AbilityDefinitionCopy`

Notes:
- Contains ability data definitions.
- Also contains ability targeting and cooldown rules that were previously in `Rules`.
- Depends on passive/buff targeting definitions.
- Can later be split into `Abilities/Definitions`, `Abilities/Targeting`, and `Abilities/Cooldowns` if the folder grows further.

### BattleFlow

Namespace: `Project.Scripts.Shared.BattleFlow`

- `BattleActionBlockReason`
- `BattleActionGateResult`
- `BattleActionGateRules`
- `BattleActionKind`
- `BattleActionPhase`
- `BattleFlowEngine`
- `BattleFlowSettings`
- `BattleFlowSnapshot`
- `BattlePhaseKind`
- `EnergyCarryoverMode`

Notes:
- Contains round/phase progression and battle action gating.
- Keep as a unit unless a broader `Battle/Flow` hierarchy is introduced.

### ActivationConditions

Namespace: `Project.Scripts.Shared.ActivationConditions`

- `ActivationConditionDefinition`
- `ActivationConditionEvent`
- `ActivationConditionGroupDefinition`
- `ActivationConditionGroupOperator`
- `ActivationConditionKind`
- `ActivationConditionRules`
- `ActivationConditionSubject`

Notes:
- Contains passive/trigger activation conditions and matching rules.
- Split from the former overloaded `Passives` folder.

### BattleSetup

Namespace: `Project.Scripts.Shared.BattleSetup`

- `BattleSetup`
- `BattleUnitSetup`

Notes:
- Small but coherent setup DTO module.
- Can become `Battle/Setup` in a broader battle hierarchy.

### Bot

Namespace: `Project.Scripts.Shared.Bot`

- `BotDecisionEngine`
- `BotSettings`

Notes:
- Coherent portable bot decision module.
- Uses `System.Random` internally and depends on hero state.

### Buffs

Namespace: `Project.Scripts.Shared.Buffs`

- `BuffDefinition`
- `BuffEngine`
- `BuffKind`
- `BuffLifetimeKind`
- `BuffModifierOperation`
- `BuffRules`
- `BuffRuntimeState`
- `BuffStackingMode`

Notes:
- Contains buff definitions, runtime state, rules, and engine.
- Split from the former overloaded `Passives` folder.

### Energy

Namespace: `Project.Scripts.Shared.Energy`

- `CascadeEnergySettings`
- `EnergyGainBreakdown`
- `EnergyGainRules`
- `MatchEnergyRules`
- `RoundEnergyCapSchedule`
- `SideEnergyPoolEngine`
- `SideEnergyPoolSnapshot`

Notes:
- Mostly cohesive economy/energy logic.
- `MatchEnergyRules` depends on grid match results and tile kinds.

### Grid

Namespace: `Project.Scripts.Shared.Grid`

- `BombRadiusRules`
- `IMatchFinder`
- `GridPoint`
- `GridState`
- `IGridState`
- `LineClearOrientation`
- `LineClearRules`
- `MatchFinder`
- `MatchResult`
- `MatchRules`
- `MatchShape`
- `SwapRequest`
- `SwapComboResolver`
- `SwapComboType`

Notes:
- Contains grid primitives, grid state, matching logic, swap request data, and board special-tile rules.
- Can later be split under a broader board domain if `Board/Grid`, `Board/Matching`, and `Board/Swaps` are introduced.

### GroupDefense

Namespace: `Project.Scripts.Shared.GroupDefense`

- `AvatarDefenseEvaluator`
- `AvatarDefenseSnapshot`
- `HeroGroupId`

Notes:
- Cohesive narrow module.
- Strongly related to heroes/groups and may move under combat units later.

### Heroes

Namespace: `Project.Scripts.Shared.Heroes`

- `BurndownDrainCursor`
- `HealthChangeResult`
- `HealthChangeRules`
- `HeroDeathResolutionResult`
- `HeroDeathResolutionRules`
- `HeroSlotState`

Notes:
- Contains hero slot state and hero health/death resolution rules that were previously in `Rules`.
- `BurndownDrainCursor` remains here because it drains hero slots before falling through to avatar drain.

### MoveBar

Namespace: `Project.Scripts.Shared.MoveBar`

- `IMoveBarEngine`
- `MoveBarEngine`
- `MoveBarSettings`
- `MoveBarSnapshot`

Notes:
- This is specifically move budget/refill bar logic.
- Renamed from `Moves` because the folder contains only move bar budget/refill logic.

### Passives

Namespace: `Project.Scripts.Shared.Passives`

- `HeroPassiveRuntimeState`
- `HeroPassiveSetup`
- `PassiveAbilityEngine`

Notes:
- Now contains only passive ability setup/runtime and the passive engine.
- Buffs, activation conditions, and targeting were split into focused folders.

### Targeting

Namespace: `Project.Scripts.Shared.Targeting`

- `BattleUnitKey`
- `UnitTargetCandidate`
- `UnitTargetFilter`
- `UnitTargetKind`
- `UnitTargetRelation`
- `UnitTargetScope`
- `UnitTargetSelectionMode`
- `UnitTargetingDefinition`
- `UnitTargetingRules`

Notes:
- Contains shared unit targeting definitions, candidates, and selection rules.
- `BattleUnitKey` lives here because it is used to compare targeting candidates.

### Tiles

Namespace: `Project.Scripts.Shared.Tiles`

- `TileDestructionContext`
- `TileKind`
- `TileKindExtensions`

Notes:
- Coherent tile primitives.
- Strongly related to board/grid and special tile rules.

### Units

Namespace: `Project.Scripts.Shared.Units`

- `BattleSide`
- `HeroActionType`
- `UnitActivationBlockReason`
- `UnitDescriptor`
- `UnitKind`

Notes:
- Contains combat unit identity shared by heroes and avatars.
- Split from `Heroes` because these types are used by avatar, hero, ability, buff, energy, and targeting systems.

## External Reference Hotspots

High-impact imports outside `Shared`:

- Root `Project.Scripts.Shared` is now only needed by `SharedVector3` users.
- `Project.Scripts.Shared.Grid` is used by tile behaviours, board/grid services, hinting, gravity, board orchestration, board special-tile rules, and swap input flow.
- `Project.Scripts.Shared.Abilities` is used by ability configs/services and now also contains ability targeting/cooldown rules.
- `Project.Scripts.Shared.BattleFlow` is used by battle flow services and now also contains battle action gating.
- `Project.Scripts.Shared.Buffs` is used by buff configs/services, events, and ability definitions.
- `Project.Scripts.Shared.ActivationConditions` is used by passive configs/services and passive runtime.
- `Project.Scripts.Shared.Targeting` is used by ability configs/rules and effect definitions.
- `Project.Scripts.Shared.Passives` is now limited to passive setup/runtime consumers.
- `Project.Scripts.Shared.Units` is used by systems that address both heroes and avatars.
- `Project.Scripts.Shared.Heroes` is now limited to hero slot state, hero health/death, and burndown cursor state.

Client-only layout code was moved out of `Shared`:

- `Project.Scripts.Gameplay.Layout` contains `GameplayScreenLayout`, `GameplayWorldLayout`, their calculators, and `ScreenLayoutRect`.

## Migration Rules

- Move files by domain first; rename public types only in a separate follow-up step.
- Keep namespace changes mechanical and compile after each stage.
- Do not mix behavior changes into folder cleanup.
- Prefer moving small/single-file folders before splitting `Passives`.
- Keep client-only layout/presentation code out of `Shared`; it now lives under `Gameplay/Layout`.
