using ClosedXML.Excel;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;

namespace DealTrack.Infrastructure.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ExcelExportService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<byte[]> ExportClientsAsync(CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;
            var clients = await _uow.Read<Client>().ListAsync(c => c.TenantId == tenantId, ct);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Clients");

            // Header row
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Phone";
            sheet.Cell(1, 3).Value = "Notes";
            sheet.Cell(1, 4).Value = "Created At";

            var headerRow = sheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRow.Style.Font.FontColor = XLColor.White;

            // Data rows
            var row = 2;
            foreach (var client in clients.OrderBy(c => c.Name))
            {
                sheet.Cell(row, 1).Value = client.Name;
                sheet.Cell(row, 2).Value = client.Phone;
                sheet.Cell(row, 3).Value = client.Notes ?? "";
                sheet.Cell(row, 4).Value = client.CreatedAt.ToString("yyyy-MM-dd");
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
