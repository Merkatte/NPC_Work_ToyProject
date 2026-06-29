using UnityEngine;

namespace Recruitment
{
    [CreateAssetMenu(
        fileName = "ResidentCandidateDefinition",
        menuName = "Settlement/Resident Candidate")]
    public class ResidentCandidateDefinition : ScriptableObject, IResidentCandidateView
    {
        [SerializeField] private string _displayName;
        [SerializeField] private ResidentCandidateKind _kind;
        [SerializeField] private CandidateStatPreview _statPreview;
        [SerializeField] private int _recruitCost;
        [SerializeField] private Sprite _portrait;

        public string DisplayName => _displayName;
        public ResidentCandidateKind Kind => _kind;
        public CandidateStatPreview StatPreview => _statPreview;
        public int RecruitCost => _recruitCost;
        public Sprite Portrait => _portrait;
    }
}
