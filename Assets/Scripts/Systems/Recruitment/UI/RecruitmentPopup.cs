using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Recruitment
{
    // Recruitment popup implementing IPopup.
    // Owns candidate slot lifecycle and delegates recruit requests to RecruitmentManager.
    // Closing must go through UIManager to keep the active stack synchronized.
    public class RecruitmentPopup : MonoBehaviour, IPopup
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RecruitmentManager _recruitmentManager;

        // Root to show/hide when opening and closing. Falls back to this GameObject if unset.
        [SerializeField] private GameObject _contentRoot;

        [SerializeField] private RecruitmentCandidateSlot _slotPrefab;
        [SerializeField] private Transform _slotParent;

        private readonly List<RecruitmentCandidateSlot> _activeSlots = new List<RecruitmentCandidateSlot>();

        public bool IsOpen { get; private set; }

        // Convenience accessor: uses _contentRoot if assigned, otherwise this GameObject.
        private GameObject ContentRoot => _contentRoot ? _contentRoot : gameObject;

        // Optional code injection path: use instead of or together with Inspector wiring.
        public void Init(RecruitmentManager recruitmentManager, UIManager uiManager)
        {
            _recruitmentManager = recruitmentManager;
            _uiManager = uiManager;
        }

        public void Open()
        {
            IsOpen = true;
            ContentRoot.SetActive(true);
            RefreshCandidates();
        }

        // Called by UIManager only. Does not call back into UIManager (prevents re-entry).
        public void Close()
        {
            IsOpen = false;
            ContentRoot.SetActive(false);
        }

        // Connect this to the close button's UnityEvent (Button.onClick).
        // Routes through UIManager so the active stack stays synchronized.
        public void OnCloseButtonClicked()
        {
            if (_uiManager)
            {
                _uiManager.ClosePopup(this);
            }
            else
            {
                Debug.LogWarning("[RecruitmentPopup] No UIManager assigned. Closing directly — active stack may become stale.", this);
                Close();
            }
        }

        // Destroys all active slots and rebuilds them from the current candidate roster.
        public void RefreshCandidates()
        {
            ClearSlots();

            if (_recruitmentManager == null)
            {
                Debug.LogWarning("[RecruitmentPopup] No RecruitmentManager assigned.", this);
                return;
            }

            IReadOnlyList<IResidentCandidateView> candidates = _recruitmentManager.Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!_slotPrefab || !_slotParent)
                    break;

                RecruitmentCandidateSlot slot = Instantiate(_slotPrefab, _slotParent);
                slot.Bind(candidates[i], RequestRecruit);
                _activeSlots.Add(slot);
            }
        }

        // Delegation target for slot recruit buttons.
        // Only uses the four domain API members specified in the design contract.
        public void RequestRecruit(IResidentCandidateView candidate)
        {
            if (_recruitmentManager == null)
                return;

            RecruitmentResult result = _recruitmentManager.TryRecruit(candidate);
            if (result == RecruitmentResult.Success)
                RefreshCandidates();
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i])
                    Destroy(_activeSlots[i].gameObject);
            }

            _activeSlots.Clear();
        }
    }
}
