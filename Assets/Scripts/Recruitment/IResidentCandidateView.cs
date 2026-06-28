using UnityEngine;

namespace Recruitment
{
    // Read-only view contract for a resident candidate.
    // UI and presentation layers depend only on this interface; they never mutate candidate state.
    public interface IResidentCandidateView
    {
        string DisplayName { get; }
        ResidentCandidateKind Kind { get; }
        CandidateStatPreview StatPreview { get; }
        int RecruitCost { get; }
        Sprite Portrait { get; }
    }
}
