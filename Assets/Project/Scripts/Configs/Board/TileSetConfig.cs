using UnityEngine;

namespace Project.Scripts.Configs.Board
{
    [CreateAssetMenu(fileName = "TileSetConfig", menuName = "Configs/TileSet Config")]
    public class TileSetConfig : ScriptableObject
    {
        [Tooltip("Обычные тайлы, доступные на доске для любых матчей")]
        [SerializeField] private TileConfig[] _regularTiles;

        [Tooltip("Специальные тайлы, доступные на доске - резолвятся по TileKind при срабатывании правил SpecialTileConfig")]
        [SerializeField] private TileConfig[] _specialTiles;


        public TileConfig[] RegularTiles => _regularTiles;
        public TileConfig[] SpecialTiles => _specialTiles;
    }
}