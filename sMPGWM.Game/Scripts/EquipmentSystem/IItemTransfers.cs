

namespace sMPGWM.Scripts.EquipmentSystem;

public interface IItemTransfers
{
    TransferResult Validate(TransferRequest request);
    TransferResult Transfer(TransferRequest request);
}
