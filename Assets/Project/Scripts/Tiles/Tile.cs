using Project.Scripts.Configs.Board;
using Project.Scripts.Shared.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Tiles
{
    public class Tile : MonoBehaviour
    {
        [Tooltip("SpriteRenderer основного изображения тайла")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("Компонент анимаций тайла: появление, уничтожение и пульсация подсказки")]
        [SerializeField] private TileAnimator _animator;

        [Tooltip("SpriteRenderer подсветки, отображаемый за тайлом для активных пассивных бонусов")]
        [SerializeField] private SpriteRenderer _glowRenderer;


        public TileKind Kind { get; private set; }
        public GridPoint GridPosition { get; set; }
        public TileConfig Config { get; private set; }
        public TileAnimator Animator => _animator;
        public TileKind PayloadKind { get; private set; }


        public void Init(TileConfig config, GridPoint gridPos, TileKind payloadKind = TileKind.None)
        {
            Config = config;
            Kind = config.Kind;
            GridPosition = gridPos;
            PayloadKind = payloadKind;
            _spriteRenderer.sprite = config.Sprite;
            SetGlowActive(false, Color.white);
        }

        public void SetPayloadKind(TileKind kind)
        {
            PayloadKind = kind;
        }

        public void SetGlowActive(bool active, Color color)
        {
            if (!_glowRenderer)
                return;
            
            _glowRenderer.color = color;
            _glowRenderer.gameObject.SetActive(active);
        }
    }
}