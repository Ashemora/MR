using System.Collections.Generic;
using Project.Scripts.Configs.Board;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Tiles;

namespace Project.Scripts.Services.Board
{
    public class SpecialTileResolver
    {
        private readonly SpecialTileConfig _config;
        private readonly TileSetConfig _tileSetConfig;


        public SpecialTileResolver(SpecialTileConfig config, TileSetConfig tileSetConfig)
        {
            _config = config;
            _tileSetConfig = tileSetConfig;
        }


        public Dictionary<GridPoint, SpecialTileSpawnData> Resolve(List<MatchResult> matches, GridPoint pivotPosition)
        {
            var result = new Dictionary<GridPoint, SpecialTileSpawnData>();
            var rules = _config.Rules;

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var entry = FindEntry(match, rules);

                if (null == entry)
                    continue;

                var tileConfig = FindTileConfig(entry.TileKind);
                if (!tileConfig)
                    continue;

                var spawnPos = entry.SpawnPosition == SpecialTileSpawnPosition.MatchCenter
                    ? match.Center
                    : pivotPosition;

                if (result.ContainsKey(spawnPos))
                    continue;

                var payloadKind = entry.TileKind == TileKind.Storm
                    ? TileKind.None
                    : match.TileKind;

                result[spawnPos] = new SpecialTileSpawnData(tileConfig, payloadKind);
            }

            return result;
        }


        private TileConfig FindTileConfig(TileKind kind)
        {
            if (null == _tileSetConfig.SpecialTiles)
                return null;

            for (var i = 0; i < _tileSetConfig.SpecialTiles.Length; i++)
                if (_tileSetConfig.SpecialTiles[i].Kind == kind)
                    return _tileSetConfig.SpecialTiles[i];

            return null;
        }

        private static SpecialTileEntry FindEntry(MatchResult match, SpecialTileEntry[] rules)
        {
            for (var i = 0; i < rules.Length; i++)
            {
                if (MatchesCondition(rules[i].Condition, match))
                    return rules[i];
            }

            return null;
        }

        private static bool MatchesCondition(SpecialTileCondition condition, MatchResult match)
        {
            return condition switch
            {
                SpecialTileCondition.LShape => match.Shape == MatchShape.LShape,
                SpecialTileCondition.TShape => match.Shape == MatchShape.TShape,
                SpecialTileCondition.Match4 => match.MaxLineLength == 4 && match.Shape is MatchShape.Horizontal or MatchShape.Vertical,
                SpecialTileCondition.Match4Horizontal => match.MaxLineLength == 4 && match.Shape == MatchShape.Horizontal,
                SpecialTileCondition.Match4Vertical => match.MaxLineLength == 4 && match.Shape == MatchShape.Vertical,
                SpecialTileCondition.Match5 => match.MaxLineLength >= 5 && match.Shape is MatchShape.Horizontal or MatchShape.Vertical,
                _ => false
            };
        }
    }
}