using System.Collections.Generic;
using UnityEngine;

namespace Recruitment
{
    public class RecruitmentManager : MonoBehaviour
    {
        [SerializeField] private List<ResidentCandidateDefinition> _candidateDefinitions = new List<ResidentCandidateDefinition>();

        private readonly List<ResidentCandidateDefinition> _candidates = new List<ResidentCandidateDefinition>();
        private IRecruitmentCostPolicy _costPolicy;
        private IResidentSpawner _residentSpawner;

        // Read seam for UI: exposes the current candidate roster without mutation access.
        // IReadOnlyList<out T> covariance allows returning List<ResidentCandidateDefinition>
        // as IReadOnlyList<IResidentCandidateView>.
        public IReadOnlyList<IResidentCandidateView> Candidates => _candidates;

        private void Awake()
        {
            _costPolicy = new AlwaysAffordableCostPolicy();

            foreach (ResidentCandidateDefinition def in _candidateDefinitions)
            {
                if (def)
                    _candidates.Add(def);
            }
        }

        // Optional explicit injection for composition root wiring or testing.
        // Call before any recruit operation (ideally before Start).
        public void Init(IRecruitmentCostPolicy costPolicy, IResidentSpawner residentSpawner)
        {
            _costPolicy = costPolicy ?? new AlwaysAffordableCostPolicy();
            _residentSpawner = residentSpawner;
        }

        public bool CanRecruit(IResidentCandidateView candidate)
        {
            if (candidate == null)
                return false;

            var definition = candidate as ResidentCandidateDefinition;
            if (definition == null || !_candidates.Contains(definition))
                return false;

            return _costPolicy.CanAfford(candidate.RecruitCost);
        }

        // Atomic recruit command: preflight → pay → spawn → remove from roster.
        // Candidate is removed only on full success. If TryPay succeeds but spawn fails,
        // the candidate is retained. NOTE: a future wallet-backed IRecruitmentCostPolicy
        // must expose a Refund path to restore deducted gold on spawn failure.
        // TODO: Add IRecruitmentCostPolicy.Refund(int cost) when IWallet is implemented.
        public RecruitmentResult TryRecruit(IResidentCandidateView candidate)
        {
            if (candidate == null)
                return RecruitmentResult.InvalidCandidate;

            var definition = candidate as ResidentCandidateDefinition;
            if (definition == null || !_candidates.Contains(definition))
                return RecruitmentResult.InvalidCandidate;

            if (!_costPolicy.CanAfford(candidate.RecruitCost))
                return RecruitmentResult.CannotAfford;

            if (!_costPolicy.TryPay(candidate.RecruitCost))
                return RecruitmentResult.CannotAfford;

            if (_residentSpawner == null)
            {
                // TODO: Assign a WorkerAIManagerSpawnerAdapter via Init() to wire
                //       recruitment to actual resident creation.
                Debug.LogWarning(
                    $"[RecruitmentManager] No IResidentSpawner assigned. " +
                    $"Candidate '{candidate.DisplayName}' cannot be spawned yet.", this);
                return RecruitmentResult.SpawnFailed;
            }

            if (!_residentSpawner.TrySpawnResident(candidate))
            {
                Debug.LogWarning(
                    $"[RecruitmentManager] IResidentSpawner failed for '{candidate.DisplayName}'.", this);
                return RecruitmentResult.SpawnFailed;
            }

            _candidates.Remove(definition);
            return RecruitmentResult.Success;
        }

        [ContextMenu("Log Candidates")]
        private void LogCandidates()
        {
            if (_candidates.Count == 0)
            {
                Debug.Log("[RecruitmentManager] No candidates.");
                return;
            }

            foreach (ResidentCandidateDefinition c in _candidates)
            {
                Debug.Log(
                    $"[RecruitmentManager] {c.DisplayName} | {c.Kind} " +
                    $"| Cost: {c.RecruitCost} | Portrait: {(c.Portrait ? c.Portrait.name : "none")}", this);
            }
        }
    }
}
