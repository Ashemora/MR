#if DEV
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI.Dev
{
    public class DevDeckListItemView : MonoBehaviour
    {
        [Tooltip("Текст с отображаемым именем деки")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Подложка, активна когда деку выбрана")]
        [SerializeField] private GameObject _selectedBackground;

        [Tooltip("Кнопка выбора этой деки")]
        [SerializeField] private Button _button;


        private int _index;
        private Action<int> _onClick;


        public void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(HandleClick);
        }


        public void Bind(int index, string displayName, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;
            if (_label)
                _label.text = displayName;
            if (_button)
            {
                _button.onClick.RemoveListener(HandleClick);
                _button.onClick.AddListener(HandleClick);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectedBackground)
                _selectedBackground.SetActive(isSelected);
        }


        private void HandleClick()
        {
            _onClick?.Invoke(_index);
        }
    }
}
#endif