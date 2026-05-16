using R3;
using UnityEngine;

namespace Project.Scripts.Services.SafeArea
{
    public class SafeAreaService : MonoBehaviour, ISafeAreaService
    {
        private readonly ReactiveProperty<SafeAreaInfo> _current = new();

        public ReadOnlyReactiveProperty<SafeAreaInfo> Current { get; private set; }


        private void Awake()
        {
            _current.Value = Capture();
            Current = _current.ToReadOnlyReactiveProperty();
        }

        private void Update()
        {
            var next = Capture();
            if (false == next.Equivalent(_current.Value))
                _current.Value = next;
        }

        private void OnDestroy()
        {
            Current?.Dispose();
            _current.Dispose();
        }


        private static SafeAreaInfo Capture()
        {
            var screenSize = new Vector2(Screen.width, Screen.height);
            return new SafeAreaInfo(Screen.safeArea, screenSize, Screen.orientation);
        }
    }
}
