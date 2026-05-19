using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZLinq;
#if DEV
using Project.Scripts.Dev;
#endif

namespace Project.Scripts.Services.UISystem
{
    public class UIService : MonoBehaviour
    {
        [Tooltip("Background layer canvas (Sort Order 0). For background screens.")]
        [SerializeField] private Canvas _backgroundCanvas;
        
        [Tooltip("Main static layer canvas (Sort Order 100). For static HUD elements: buttons, labels.")]
        [SerializeField] private Canvas _mainCanvas;
        
        [Tooltip("Main dynamic layer canvas (Sort Order 150). For animated elements: avatars, heroes, HP bars, effects.")]
        [SerializeField] private Canvas _mainDynamicCanvas;
        
        [Tooltip("Popup layer canvas (Sort Order 200). For popups and overlays.")]
        [SerializeField] private Canvas _popupCanvas;
        
        [Tooltip("System layer canvas (Sort Order 300). For system UI: loading screens, alerts.")]
        [SerializeField] private Canvas _systemCanvas;

        [Tooltip("Safe area root under Main canvas. Views on Main spawn here by default when assigned.")]
        [SerializeField] private RectTransform _mainSafeRoot;

        [Tooltip("Safe area root under MainDynamic canvas. Views on MainDynamic spawn here by default when assigned.")]
        [SerializeField] private RectTransform _mainDynamicSafeRoot;

        [Tooltip("Safe area root under Popup canvas. Views on Popup spawn here by default when assigned.")]
        [SerializeField] private RectTransform _popupSafeRoot;

        
        private readonly Dictionary<Type, GameObject> _registeredViews = new();
        private readonly Dictionary<Type, IView> _activeViews = new();
        private readonly Dictionary<Type, UILayer> _viewLayers = new();
        private DebugConfig _debugConfig;
        private IObjectResolver _resolver;


        private void Awake()
        {
            SetupCanvasLayers();
        }


        [Inject]
        public void Construct(DebugConfig debugConfig, IObjectResolver resolver)
        {
            _debugConfig = debugConfig;
            _resolver = resolver;
        }

        public void RegisterView<TView>(GameObject prefab, UILayer layer) where TView : MonoBehaviour, IView
        {
            var type = typeof(TView);
            _registeredViews[type] = prefab;
            _viewLayers[type] = layer;
            if (_debugConfig.LogUIEvents)
                Debug.Log($"View {type.Name} registered on layer {layer}");
        }

        public async UniTask<TView> Show<TView, TViewModel>(TViewModel viewModel)
            where TView : BaseView<TViewModel>
            where TViewModel : BaseViewModel
        {
            var viewType = typeof(TView);

            if (_activeViews.TryGetValue(viewType, out var existingView))
            {
                var typedView = existingView as TView;
                if (!typedView)
                {
                    Debug.LogError($"Active view is not of type {viewType.Name}, removing corrupted entry");
                    _activeViews.Remove(viewType);
                }
                else
                {
                    await typedView.ShowAsync();
                    return typedView;
                }
            }

            var view = CreateView<TView, TViewModel>();
            if (!view)
                return null;

            await view.InitializeAsync(viewModel);
            await view.ShowAsync();

            _activeViews[viewType] = view;

            if (_debugConfig.LogUIEvents)
                Debug.Log($"View {viewType.Name} shown");
            return view;
        }

        public async UniTask<TView> Show<TView, TViewModel>()
            where TView : BaseView<TViewModel>
            where TViewModel : BaseViewModel, new()
        {
            var viewType = typeof(TView);

            if (_activeViews.TryGetValue(viewType, out var existingView))
            {
                var typedView = existingView as TView;
                if (!typedView)
                {
                    Debug.LogError($"Active view is not of type {viewType.Name}, removing corrupted entry");
                    _activeViews.Remove(viewType);
                }
                else
                {
                    await typedView.ShowAsync();
                    return typedView;
                }
            }

            var view = CreateView<TView, TViewModel>();
            if (!view)
                return null;

            var viewModel = new TViewModel();
            await view.InitializeAsync(viewModel);
            await view.ShowAsync();

            _activeViews[viewType] = view;

            if (_debugConfig.LogUIEvents)
                Debug.Log($"View {viewType.Name} shown");
            
            return view;
        }

        public async UniTask Hide<TView>() where TView : MonoBehaviour, IView
        {
            var viewType = typeof(TView);

            if (false == _activeViews.TryGetValue(viewType, out var view))
            {
                if (_debugConfig.LogUIEvents)
                    Debug.LogWarning($"View {viewType.Name} is not active");
                
                return;
            }

            await view.HideAsync();
        }

        public void Close<TView>() where TView : MonoBehaviour, IView
        {
            var viewType = typeof(TView);

            if (false == _activeViews.Remove(viewType, out var view))
                return;

            if (view is MonoBehaviour mono && mono)
                view.Close();
        }

        public TView GetCurrent<TView>() where TView : MonoBehaviour, IView
        {
            var viewType = typeof(TView);

            if (_activeViews.TryGetValue(viewType, out var view))
                return view as TView;

            return null;
        }

        public void CloseAll()
        {
            var types = _activeViews.Keys.AsValueEnumerable().ToList();

            for (var i = 0; i < types.Count; i++)
            {
                var view = _activeViews[types[i]];
                if (view is MonoBehaviour mono && mono)
                    view.Close();
            }

            _activeViews.Clear();
        }

#if DEV
        public void CleanupDevGameplayButtons()
        {
            if (!_mainCanvas)
                return;

            var root = _mainCanvas.transform;
            DevGameplayButtonCleanup.DestroyNamedButtons(root, "DevAbortBattleButton");
            DevGameplayButtonCleanup.DestroyNamedButtons(root, "DevMatchPhaseSkipButton");
        }
#endif


        private void SetupCanvasLayers()
        {
            SetupCanvas(_backgroundCanvas, UILayer.Background);
            SetupCanvas(_mainCanvas, UILayer.Main);
            SetupCanvas(_mainDynamicCanvas, UILayer.MainDynamic);
            SetupCanvas(_popupCanvas, UILayer.Popup);
            SetupCanvas(_systemCanvas, UILayer.System);
        }

        private void SetupCanvas(Canvas canvas, UILayer layer)
        {
            if (!canvas)
            {
                Debug.LogError($"Canvas for layer {layer} is not assigned!");
                return;
            }

            canvas.sortingOrder = (int)layer;
        }

        private TView CreateView<TView, TViewModel>()
            where TView : BaseView<TViewModel>
            where TViewModel : BaseViewModel
        {
            var viewType = typeof(TView);

            if (false == _registeredViews.TryGetValue(viewType, out var prefab))
            {
                Debug.LogError($"View {viewType.Name} not registered!");
                return null;
            }

            if (false == _viewLayers.TryGetValue(viewType, out var layer))
            {
                Debug.LogError($"View {viewType.Name} has no registered UI layer!");
                return null;
            }

            var prefabView = prefab.GetComponent<TView>();
            if (!prefabView)
            {
                Debug.LogError($"Prefab doesn't have {viewType.Name} component!");
                return null;
            }

            var viewObject = Instantiate(prefab, GetParentForLayer(layer, prefabView.SafeAreaMode));
            _resolver?.InjectGameObject(viewObject);

            return viewObject.GetComponent<TView>();
        }

        private Transform GetParentForLayer(UILayer layer, SafeAreaMode mode)
        {
            var canvas = GetCanvasForLayer(layer);
            if (mode == SafeAreaMode.ForceIgnore)
                return canvas.transform;

            var safeRoot = GetSafeRootForLayer(layer);
            if (mode == SafeAreaMode.ForceApply)
                return safeRoot ? safeRoot : canvas.transform;

            return LayerHasSafeAreaByDefault(layer) && safeRoot
                ? safeRoot
                : canvas.transform;
        }

        public Transform GetLayerRoot(UILayer layer)
        {
            return GetCanvasForLayer(layer).transform;
        }

        public Transform GetLayerRoot(UILayer layer, SafeAreaMode mode)
        {
            return GetParentForLayer(layer, mode);
        }

        private RectTransform GetSafeRootForLayer(UILayer layer)
        {
            return layer switch
            {
                UILayer.Main => _mainSafeRoot,
                UILayer.MainDynamic => _mainDynamicSafeRoot,
                UILayer.Popup => _popupSafeRoot,
                _ => null
            };
        }

        private static bool LayerHasSafeAreaByDefault(UILayer layer)
        {
            return layer == UILayer.Main
                   || layer == UILayer.MainDynamic
                   || layer == UILayer.Popup;
        }

        private Canvas GetCanvasForLayer(UILayer layer)
        {
            return layer switch
            {
                UILayer.Background => _backgroundCanvas,
                UILayer.Main => _mainCanvas,
                UILayer.MainDynamic => _mainDynamicCanvas,
                UILayer.Popup => _popupCanvas,
                UILayer.System => _systemCanvas,
                _ => _mainCanvas
            };
        }
    }
}