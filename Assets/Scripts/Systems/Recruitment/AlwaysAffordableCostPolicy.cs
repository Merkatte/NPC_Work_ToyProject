namespace Recruitment
{
    // Temporary stand-in until a real gold/wallet system exists.
    // Both CanAfford and TryPay always succeed; no state is mutated.
    // TODO: Replace with a WalletCostPolicy once IWallet is implemented.
    public sealed class AlwaysAffordableCostPolicy : IRecruitmentCostPolicy
    {
        public bool CanAfford(int cost) => true;

        public bool TryPay(int cost) => true;
    }
}
