using dai.dataAccess.IRepositories;
using iTextSharp.text.pdf;
using iTextSharp.text;
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
            var pdfDoc = new Document(PageSize.A4);
            PdfWriter.GetInstance(pdfDoc, stream);
            pdfDoc.Open();

            var titleFont = FontFactory.GetFont("Arial", 16, Font.BOLD);
            var sectionFont = FontFactory.GetFont("Arial", 12, Font.BOLD);
            var bodyFont = FontFactory.GetFont("Arial", 10);

            pdfDoc.Add(new Paragraph("Monthly Task Report", titleFont));
            pdfDoc.Add(new Chunk("\n"));

            // Tasks Created This Month
            pdfDoc.Add(new Paragraph("Tasks Created This Month", sectionFont));
            pdfDoc.Add(new Chunk("\n")); // Add space

            var createdTable = new PdfPTable(3) { WidthPercentage = 100 };
            createdTable.SetWidths(new float[] { 1f, 1f, 1f });
            createdTable.AddCell(new PdfPCell(new Phrase("Task Title", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            createdTable.AddCell(new PdfPCell(new Phrase("Created At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            createdTable.AddCell(new PdfPCell(new Phrase("Finished At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

            if (!tasksCreatedThisMonth.Any())
            {
                pdfDoc.Add(new Paragraph("No tasks created this month.", bodyFont));
            }
            else
            {
                foreach (var task in tasksCreatedThisMonth)
                {
                    createdTable.AddCell(new PdfPCell(new Phrase(task.Title, bodyFont)));
                    createdTable.AddCell(new PdfPCell(new Phrase(task.Create_At.ToString("g"), bodyFont)));
                    createdTable.AddCell(new PdfPCell(new Phrase(task.Finish_At.ToString("g") ?? "N/A", bodyFont)));
                }
                pdfDoc.Add(createdTable);
            }

            pdfDoc.Add(new Chunk("\n")); // Add space

            // Tasks Done This Month
            pdfDoc.Add(new Paragraph("Tasks Done This Month", sectionFont));
            pdfDoc.Add(new Chunk("\n")); // Add space

            var doneTable = new PdfPTable(3) { WidthPercentage = 100 };
            doneTable.SetWidths(new float[] { 1f, 1f, 1f });
            doneTable.AddCell(new PdfPCell(new Phrase("Task Title", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            doneTable.AddCell(new PdfPCell(new Phrase("Created At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            doneTable.AddCell(new PdfPCell(new Phrase("Finished At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

            if (!tasksDoneThisMonth.Any())
            {
                pdfDoc.Add(new Paragraph("No tasks done this month.", bodyFont));
            }
            else
            {
                foreach (var task in tasksDoneThisMonth)
                {
                    doneTable.AddCell(new PdfPCell(new Phrase(task.Title, bodyFont)));
                    doneTable.AddCell(new PdfPCell(new Phrase(task.Create_At.ToString("g"), bodyFont)));
                    doneTable.AddCell(new PdfPCell(new Phrase(task.Finish_At.ToString("g"), bodyFont)));
                }
                pdfDoc.Add(doneTable);
            }

            pdfDoc.Add(new Chunk("\n")); // Add space

            // Tasks Over This Month
            pdfDoc.Add(new Paragraph("Tasks Over This Month", sectionFont));
            pdfDoc.Add(new Chunk("\n")); // Add space

            var overTable = new PdfPTable(3) { WidthPercentage = 100 };
            overTable.SetWidths(new float[] { 1f, 1f, 1f });
            overTable.AddCell(new PdfPCell(new Phrase("Task Title", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            overTable.AddCell(new PdfPCell(new Phrase("Created At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            overTable.AddCell(new PdfPCell(new Phrase("Finished At", sectionFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

            if (!tasksOverThisMonth.Any())
            {
                pdfDoc.Add(new Paragraph("No tasks over this month.", bodyFont));
            }
            else
            {
                foreach (var task in tasksOverThisMonth)
                {
                    overTable.AddCell(new PdfPCell(new Phrase(task.Title, bodyFont)));
                    overTable.AddCell(new PdfPCell(new Phrase(task.Create_At.ToString("g"), bodyFont)));
                    overTable.AddCell(new PdfPCell(new Phrase(task.Finish_At.ToString("g"), bodyFont)));
                }
                pdfDoc.Add(overTable);
            }

            pdfDoc.Close();
            var pdfBytes = stream.ToArray();

            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "MonthlyTaskReport.pdf");
            await System.IO.File.WriteAllBytesAsync(downloadsPath, pdfBytes);

            return Ok(new { message = "PDF generated successfully!", path = downloadsPath });
        }
    }

}


