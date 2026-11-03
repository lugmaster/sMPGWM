

namespace sMPGWM.Scripts.Items.Equipment;

public interface IItemTransfers
{
    TransferResult Validate(TransferRequest request);
    TransferResult Transfer(TransferRequest request);
}
