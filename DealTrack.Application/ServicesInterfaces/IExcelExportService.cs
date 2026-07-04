namespace DealTrack.Application.ServicesInterfaces
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportClientsAsync(CancellationToken ct = default);
    }
}
