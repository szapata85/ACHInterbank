namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseStrategyFactory
{
    IClearingHouseStrategy GetStrategy(int clearingHouseId);
}
