namespace Recruitment
{
    // Creation seam between recruitment and the actual worker/NPC system.
    // TODO: Implement a WorkerAIManagerSpawnerAdapter that maps IResidentCandidateView
    //       to a WorkerAIManager.SpawnWorker call once the candidate→worker stat
    //       mapping is defined (e.g. which spawn point, how candidate stats map to WorkerInitialStats).
    public interface IResidentSpawner
    {
        bool TrySpawnResident(IResidentCandidateView candidate);
    }
}
