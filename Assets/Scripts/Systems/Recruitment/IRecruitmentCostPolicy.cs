namespace Recruitment
{
    // Gold/wallet seam for recruitment cost validation and deduction.
    // Replace AlwaysAffordableCostPolicy with a wallet-backed implementation
    // once the economy system (IWallet) is ready.
    public interface IRecruitmentCostPolicy
    {
        bool CanAfford(int cost);
        bool TryPay(int cost);
    }
}
