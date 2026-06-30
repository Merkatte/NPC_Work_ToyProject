using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Recruitment
{
    // Display skeleton for one resident candidate.
    // Serialized UI fields are placeholders to connect in the prefab Inspector.
    // Recruit request is delegated via callback — this slot does not call WorkerAIManager or worker internals.
    public class RecruitmentCandidateSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _kindText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Button _recruitButton;

        // Placeholder parent for stat preview lines (RecruitmentManager.Candidates -> StatPreview.Lines).
        // Wire child TMP_Text elements here when implementing stat display.
        [SerializeField] private Transform _statLineParent;

        private IResidentCandidateView _candidate;
        private Action<IResidentCandidateView> _onRecruitRequested;

        // Binds a candidate view and sets up the recruit button callback.
        // Called by RecruitmentPopup after Instantiate.
        public void Bind(IResidentCandidateView candidate, Action<IResidentCandidateView> onRecruitRequested)
        {
            _candidate = candidate;
            _onRecruitRequested = onRecruitRequested;

            if (_nameText)
                _nameText.text = candidate != null ? candidate.DisplayName : string.Empty;

            if (_kindText)
                _kindText.text = candidate != null ? candidate.Kind.ToString() : string.Empty;

            if (_costText)
                _costText.text = candidate != null ? candidate.RecruitCost.ToString() : string.Empty;

            if (_portraitImage)
            {
                // Portrait is a UnityEngine.Object; truthiness check guards against fake-null.
                Sprite portrait = candidate != null ? candidate.Portrait : null;
                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait;
            }

            if (_recruitButton)
            {
                _recruitButton.onClick.RemoveAllListeners();
                _recruitButton.onClick.AddListener(OnRecruitClicked);
            }
        }

        private void OnRecruitClicked()
        {
            _onRecruitRequested?.Invoke(_candidate);
        }

        private void OnDestroy()
        {
            if (_recruitButton)
                _recruitButton.onClick.RemoveAllListeners();
        }
    }
}
