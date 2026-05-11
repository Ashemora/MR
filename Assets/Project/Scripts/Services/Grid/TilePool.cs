using Project.Scripts.Configs.Board;
using Project.Scripts.Tiles;
using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace Project.Scripts.Services.Grid
{
    public class TilePool
    {
        private float _tileVisualSize;
        private readonly ObjectPool<Tile> _pool;
#if UNITY_EDITOR
        private readonly HashSet<Tile> _activeTiles = new();
#endif


        public TilePool(Tile prefab, Transform parent, BoardAnimationConfig animConfig, float cellSize, float tileFillPercent)
        {
            _tileVisualSize = CalculateTileVisualSize(cellSize, tileFillPercent);
            _pool = new ObjectPool<Tile>(
                createFunc: () =>
                {
                    var t = Object.Instantiate(prefab, parent);
                    t.Animator.Init(animConfig);
                    return t;
                },
                actionOnGet: t =>
                {
                    t.transform.localScale = Vector3.one * _tileVisualSize;
                    t.Animator.SetTargetScale(_tileVisualSize);
                    t.gameObject.SetActive(true);
#if UNITY_EDITOR
                    _activeTiles.Add(t);
#endif
                },
                actionOnRelease: t =>
                {
                    DG.Tweening.DOTween.Kill(t.transform);
                    t.gameObject.SetActive(false);
#if UNITY_EDITOR
                    _activeTiles.Remove(t);
#endif
                },
                actionOnDestroy: t =>
                {
#if UNITY_EDITOR
                    _activeTiles.Remove(t);
#endif
                    Object.Destroy(t.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 36,
                maxSize: 100
            );
        }

        public Tile Get() => _pool.Get();

        public void Release(Tile tile) => _pool.Release(tile);

#if UNITY_EDITOR
        public void UpdateScale(float cellSize, float tileFillPercent)
        {
            _tileVisualSize = CalculateTileVisualSize(cellSize, tileFillPercent);
            foreach (var tile in _activeTiles)
            {
                tile.transform.localScale = Vector3.one * _tileVisualSize;
                tile.Animator.SetTargetScale(_tileVisualSize);
            }
        }
#endif

        private static float CalculateTileVisualSize(float cellSize, float tileFillPercent)
        {
            return cellSize * Mathf.Max(0f, tileFillPercent);
        }
    }
}