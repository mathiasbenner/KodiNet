using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface ITransferQueueService
{
    IReadOnlyList<TransferOperationDto>     Operations { get; }
    event Action?                           StateChanged;
    void                                    EnqueueMassTransfer(MassTransferRequest request);
    void                                    ClearCompleted();
    Task                                    CancelAsync(Guid operationId);
}
