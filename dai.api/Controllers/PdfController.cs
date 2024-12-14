using dai.dataAccess.IRepositories;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;

    public PdfController(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    [HttpGet("generate-report")]
    public async Task<IActionResult> GenerateReport(Guid boardId)
    {
        var tasksCreatedThisMonth = await _taskRepository.GetTasksCreatedInMonthByBoardAsync(boardId);
        var tasksDoneThisMonth = await _taskRepository.GetTasksByStatusInMonthByBoardAsync("done", boardId);
        var tasksOverThisMonth = await _taskRepository.GetTasksByStatusInMonthByBoardAsync("over", boardId);

        using (var stream = new MemoryStream())
        {
            var writer = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(writer);
            var document = new Document(pdfDoc);

            // Title
            document.Add(new Paragraph("Monthly Task Report")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20));

            document.Add(new Paragraph("\n")); // Add space

            // Tasks Created This Month
            document.Add(new Paragraph("Tasks Created This Month").SetFontSize(14));
            document.Add(new Paragraph("\n")); // Add space

            if (!tasksCreatedThisMonth.Any())
            {
                document.Add(new Paragraph("No tasks created this month."));
            }
            else
            {
                var table = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Task Title")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Created At")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Finished At")));

                foreach (var task in tasksCreatedThisMonth)
                {
                    table.AddCell(new Paragraph(task.Title));
                    table.AddCell(new Paragraph(task.Create_At.ToString("g")));
                    table.AddCell(new Paragraph(task.Finish_At.ToString("g") ?? "N/A"));
                }
                document.Add(table);
            }

            document.Add(new Paragraph("\n")); // Add space

            // Tasks Done This Month
            document.Add(new Paragraph("Tasks Done This Month").SetFontSize(14));
            document.Add(new Paragraph("\n")); // Add space

            if (!tasksDoneThisMonth.Any())
            {
                document.Add(new Paragraph("No tasks done this month."));
            }
            else
            {
                var table = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Task Title")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Created At")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Finished At")));

                foreach (var task in tasksDoneThisMonth)
                {
                    table.AddCell(new Paragraph(task.Title));
                    table.AddCell(new Paragraph(task.Create_At.ToString("g")));
                    table.AddCell(new Paragraph(task.Finish_At.ToString("g")));
                }
                document.Add(table);
            }

            document.Add(new Paragraph("\n")); // Add space

            // Tasks Over This Month
            document.Add(new Paragraph("Tasks Over This Month").SetFontSize(14));
            document.Add(new Paragraph("\n")); // Add space

            if (!tasksOverThisMonth.Any())
            {
                document.Add(new Paragraph("No tasks over this month."));
            }
            else
            {
                var table = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Task Title")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Created At")));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Finished At")));

                foreach (var task in tasksOverThisMonth)
                {
                    table.AddCell(new Paragraph(task.Title));
                    table.AddCell(new Paragraph(task.Create_At.ToString("g")));
                    table.AddCell(new Paragraph(task.Finish_At.ToString("g")));
                }
                document.Add(table);
            }

            // Close document
            document.Close();

            // Save to Downloads folder
            var pdfBytes = stream.ToArray();
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "MonthlyTaskReport.pdf");
            await System.IO.File.WriteAllBytesAsync(downloadsPath, pdfBytes);

            return Ok(new { message = "PDF generated successfully!", path = downloadsPath });
        }
    }
}
