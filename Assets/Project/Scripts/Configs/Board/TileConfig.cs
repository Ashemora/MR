using Project.Scripts.Tiles.Behaviours;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Utils.Attributes;
using UnityEngine;

namespace Project.Scripts.Configs.Board
{
    [CreateAssetMenu(fileName = "TileConfig", menuName = "Configs/Tile Config")]
    public class TileConfig : ScriptableObject
    {
        [Tooltip("Идентификатор тайла - определяет как цвет совпадения (Red/Blue и т.д.), так и специальный тип (Bomb/Storm и т.д.)")]
        [SerializeField] private TileKind _kind;

        [Tooltip("Визуальный спрайт тайла")]
        [SpritePreview]
        [SerializeField] private Sprite _sprite;

        [Tooltip("Специальное поведение при уничтожении этого тайла")]
        [SerializeField] private TileBehaviour _behaviour;


        public TileKind Kind => _kind;
        public Sprite Sprite => _sprite;
        public TileBehaviour Behaviour => _behaviour;
    }
}