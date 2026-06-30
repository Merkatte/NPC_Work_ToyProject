using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    // Scene-level UI router and popup stack manager.
    // Responsibilities: open/close popups through IPopup, maintain LIFO active stack, handle ESC.
    // Does not own recruitment logic, worker logic, or any domain behavior.
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private List<UIPopupEntry> _popupEntries = new List<UIPopupEntry>();

        private readonly Stack<IPopup> _openPopups = new Stack<IPopup>();

        private void Update()
        {
            // ESC closes only the top popup. Requires Input System package (activeInputHandler: 1).
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseTopPopup();
        }

        // Opens the popup registered for the given type.
        // No-ops with a warning if the popup is already open or the entry is missing/misconfigured.
        public void OpenPopup(UIPopupType popupType)
        {
            IPopup popup = FindPopup(popupType);
            if (popup == null)
                return;

            if (popup.IsOpen)
            {
                Debug.LogWarning($"[UIManager] Popup '{popupType}' is already open.", this);
                return;
            }

            _openPopups.Push(popup);
            popup.Open();
        }

        // Closes the topmost open popup (called by ESC).
        public void CloseTopPopup()
        {
            if (_openPopups.Count == 0)
                return;

            IPopup top = _openPopups.Pop();
            top.Close();
        }

        // Closes the given popup and synchronizes the active stack regardless of stack position.
        // Use this as the single close path so any close route keeps the stack consistent.
        // Closing a non-top popup is abnormal; a warning is logged but the close proceeds.
        public void ClosePopup(IPopup popup)
        {
            if (popup == null)
                return;

            if (_openPopups.Count == 0)
            {
                Debug.LogWarning("[UIManager] ClosePopup called but no popups are open.", this);
                return;
            }

            if (_openPopups.Peek() == popup)
            {
                _openPopups.Pop();
                popup.Close();
                return;
            }

            // Non-top close: check membership, rebuild stack without the target.
            IPopup[] snapshot = _openPopups.ToArray(); // ToArray() returns top-first order.
            bool found = false;
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] == popup)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning("[UIManager] ClosePopup: popup is not in the active stack.", this);
                return;
            }

            // Rebuild from bottom to top, skipping the target.
            _openPopups.Clear();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                if (snapshot[i] != popup)
                    _openPopups.Push(snapshot[i]);
            }

            Debug.LogWarning("[UIManager] Non-top popup was closed. Stack rebuilt.", this);
            popup.Close();
        }

        private IPopup FindPopup(UIPopupType popupType)
        {
            for (int i = 0; i < _popupEntries.Count; i++)
            {
                UIPopupEntry entry = _popupEntries[i];
                if (entry.PopupType != popupType)
                    continue;

                MonoBehaviour source = entry.Popup;
                if (!source)
                {
                    Debug.LogWarning($"[UIManager] Popup entry for '{popupType}' has no MonoBehaviour assigned.", this);
                    return null;
                }

                IPopup popup = source as IPopup;
                if (popup == null)
                {
                    Debug.LogWarning($"[UIManager] MonoBehaviour for '{popupType}' does not implement IPopup.", this);
                    return null;
                }

                return popup;
            }

            Debug.LogWarning($"[UIManager] No entry found for popup type '{popupType}'.", this);
            return null;
        }
    }

    // Inspector entry that maps a UIPopupType to the MonoBehaviour implementing IPopup.
    // Pattern mirrors WorkerSelectorEntry in WorkerAIManager.
    [Serializable]
    public class UIPopupEntry
    {
        [SerializeField] private UIPopupType _popupType;
        [SerializeField] private MonoBehaviour _popup;

        public UIPopupType PopupType => _popupType;
        public MonoBehaviour Popup => _popup;
    }
}
