using dai.api.Services.ServiceExtension;
using dai.core.DTO.DragAndDrop;
using dai.core.DTO.Task;
using dai.core.Models;
using dai.dataAccess.IRepositories;
using dai.dataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using System.Collections.Generic;
using dai.dataAccess.DbContext;

namespace dai.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TaskController : ControllerBase
{
    private readonly UserManager<UserModel> _userManager;
    private readonly ITaskRepository _taskRepository;
    private readonly IDragAndDropRepository _dragAndDropRepository;
    private readonly IConfiguration _configuration;
    private readonly AzureBlobService _storageService;
    private readonly AppDbContext _context;


    public TaskController(ITaskRepository taskRepository, IDragAndDropRepository dragAndDropRepository, IConfiguration configuration, AzureBlobService storageService, UserManager<UserModel> userManager, AppDbContext context)
    {
        _taskRepository = taskRepository;
        _dragAndDropRepository = dragAndDropRepository;
        _configuration = configuration;
        _storageService = storageService;
        _userManager = userManager;
        _context = context;
    }

    // GET: api/Task
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GET_Task>>> GetAllTasks()
    {
        var tasks = await _taskRepository.GetAllTasksAsync();

        var taskDtos = tasks.Select(t => new GET_Task
        {
            Id = t.Id,
            Title = t.Title,
            Create_At = t.Create_At,
            Update_At = t.Update_At,
            Finish_At = t.Finish_At,
            Description = t.Description,
            Status = t.Status,
            Position = t.Position,
            AvailableCheck = t.AvailableCheck,
            //UserEmail = t.User?.Email
        }).ToList();

        return Ok(taskDtos);
    }

    // POST: api/Task
    [HttpPost]
    public async Task<ActionResult<POST_Task>> PostTask([FromBody] POST_Task postTask, Guid listId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var task = new TaskModel
        {
            Id = Guid.NewGuid(),
            Title = postTask.Title,
            Create_At = postTask.Create_At,
            Update_At = DateTime.Now,
            Finish_At = postTask.Finish_At,
            Description = postTask.Description,
            Status = postTask.Status,
            //AssignTo = postTask.AssignTo,
            //AssignedToList = postTask.AssignedUsers.ToList(),
            AvailableCheck = postTask.AvailableCheck,
        };

        try
        {
            var createdTask = await _taskRepository.AddTaskAsync(task, listId);

            var taskDTO = new GET_Task
            {
                Id = createdTask.Id,
                Title = createdTask.Title,
                Create_At = createdTask.Create_At,
                Update_At = createdTask.Update_At,
                Finish_At = createdTask.Finish_At,
                //UserEmail = createdTask.User?.Email,
                Status = createdTask.Status
            };

            return CreatedAtAction(nameof(GetTaskById), new { id = taskDTO.Id }, taskDTO);
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, "An error occurred while saving the list. Please try again.");
        }
    }

    // GET: api/Task/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GET_Task>> GetTaskById(Guid id)
    {
        var task = await _taskRepository.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        Dictionary<Guid, string> assignedUsersEmails = new Dictionary<Guid, string>();
        if (task.AssignedToList?.Any() == true)
        {
            var assignedUsers = await _context.Users
                .Where(u => task.AssignedToList.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            assignedUsersEmails = assignedUsers.ToDictionary(u => u.Id, u => u.Email);
        }

        var taskDto = new GET_Task
        {
            Id = task.Id,
            Title = task.Title,
            Create_At = task.Create_At,
            Update_At = task.Update_At,
            Finish_At = task.Finish_At,
            Description = task.Description,
            Status = task.Status,
            Position = task.Position,
            AvailableCheck = task.AvailableCheck,
            //UserEmail = task.User?.Email, // Include the User's email
            //UserEmailId = task.User?.Id ?? Guid.Empty,
            AssignedUsers = task.AssignedToList,
            AssignedUsersEmails = assignedUsersEmails, 
            FileLink = task.FileName
        };
        return Ok(taskDto);
    }

    [HttpGet("details/{taskId}")]
    public async Task<IActionResult> GetTaskDetails(Guid taskId)
    {
        try
        {
            // Call the repository method to get task details
            var taskDetails = await _taskRepository.GetTaskDetailsAsync(taskId);

            if (taskDetails.WorkspaceName == null && taskDetails.BoardName == null && taskDetails.ListName == null)
            {
                return NotFound(new { Message = "Task details not found or incomplete" });
            }

            // Return the task details as a response
            return Ok(new
            {
                WorkspaceName = taskDetails.WorkspaceName,
                BoardName = taskDetails.BoardName,
                ListName = taskDetails.ListName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching task details", Error = ex.Message });
        }
    }


    // PUT: api/Task/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TaskModel>> UpdateTask(Guid id, [FromForm] PUT_Task updatedTask)
    {
        var existingTask = await _taskRepository.GetTaskByIdAsync(id);
        if (existingTask == null)
        {
            return NotFound();
        }

        // Check if assigned user has changed
        //var previousAssigneeId = existingTask.AssignTo;
        var previousAssignedUsers = existingTask.AssignedToList ?? new List<Guid>();

        // Update task properties
        existingTask.Title = updatedTask.Title;
        existingTask.Description = updatedTask.Description;
        existingTask.Status = updatedTask.Status;
        existingTask.Create_At = updatedTask.Create_At; 
        existingTask.Finish_At = updatedTask.Finish_At;
        existingTask.AvailableCheck = updatedTask.AvailableCheck;
        //existingTask.AssignTo = updatedTask.AssignTo;

        // Handle file upload logic
        string newTaskFileUrl = null;
        if (updatedTask.File != null)
        {
            try
            {
                // Delete old file if it exists
                if (!string.IsNullOrEmpty(existingTask.FileName))
                {
                    var deleteResult = await _storageService.DeleteTaskFileAsync(existingTask.FileName);
                    if (!deleteResult)
                        return BadRequest(new { Message = "Failed to delete old task file" });
                }

                // Upload new file
                var fileName = Path.GetFileName(updatedTask.File.FileName);
                var folderName = "task-files";
                var containerName = _configuration["AzureBlobStorage:ContainerName"];
                var taskFileUrl = await _storageService.UploadTaskFileAsync(updatedTask.File.OpenReadStream(), containerName, folderName, fileName);

                if (!string.IsNullOrEmpty(taskFileUrl))
                {
                    existingTask.FileName = taskFileUrl;
                    newTaskFileUrl = taskFileUrl;
                }
                else
                {
                    return BadRequest(new { Message = "Failed to upload task file" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred while uploading task file", Error = ex.Message });
            }
        }

        existingTask.AssignedToList = updatedTask.AssignedUsers.ToList();

        await _taskRepository.UpdateTaskAsync(existingTask);

        // Send email notifications to newly assigned users
        var newlyAssignedUsers = updatedTask.AssignedUsers.Except(previousAssignedUsers).ToList();
        foreach (var userId in newlyAssignedUsers)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                var taskDetails = await _taskRepository.GetTaskDetailsAsync(existingTask.Id);

                string emailMessage = $@"
                                                <html>
                                                <head>
                                                    <style>
                                                        body {{
                                                            font-family: Arial, sans-serif;
                                                            background-color: #f4f4f4;
                                                            color: #333;
                                                            margin: 0;
                                                            padding: 0;
                                                        }}
                                                        .email-container {{
                                                            max-width: 600px;
                                                            margin: 20px auto;
                                                            background: #ffffff;
                                                            border-radius: 8px;
                                                            overflow: hidden;
                                                            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                                                        }}
                                                        .header {{
                                                            background: #323338;
                                                            color: white;
                                                            text-align: center;
                                                            padding: 20px;
                                                            font-size: 24px;
                                                            font-weight: bold;
                                                        }}
                                                        .content {{
                                                            padding: 20px;
                                                        }}
                                                        .content p {{
                                                            font-size: 16px;
                                                            line-height: 1.5;
                                                            margin-bottom: 20px;
                                                        }}
                                                        .task-details {{
                                                            list-style: none;
                                                            padding: 0;
                                                            margin: 0;
                                                        }}
                                                        .task-details li {{
                                                            margin: 10px 0;
                                                            font-size: 14px;
                                                            line-height: 1.5;
                                                        }}
                                                        .task-details strong {{
                                                            color: #4CAF50;
                                                        }}
                                                        .button {{
                                                            display: inline-block;
                                                            background-color: #4CAF50;
                                                            color: white;
                                                            text-decoration: none;
                                                            padding: 10px 20px;
                                                            margin: 20px 0;
                                                            border-radius: 5px;
                                                            font-size: 16px;
                                                        }}
                                                        .button:hover {{
                                                            background-color: #45a049;
                                                        }}
                                                        .footer {{
                                                            text-align: center;
                                                            background: #f4f4f4;
                                                            padding: 10px;
                                                            font-size: 12px;
                                                            color: #666;
                                                        }}
                                                        .footer a {{
                                                            color: #4CAF50;
                                                            text-decoration: none;
                                                        }}
                                                    </style>
                                                </head>
                                                <body>
                                                    <div class='email-container'>
                                                        <div class='header'>
                                                            Task Assignment Notification
                                                        </div>
                                                        <div class='content'>
                                                            <p>Hello <strong>{user.FullName}</strong>,</p>
                                                            <p>You have been assigned a new task:</p>
                                                            <ul class='task-details'>
                                                                <li><strong>Task:</strong> {existingTask.Title}</li>
                                                                <li><strong>Description:</strong> {existingTask.Description}</li>
                                                                <li><strong>Workspace:</strong> {taskDetails.WorkspaceName}</li>
                                                                <li><strong>Board:</strong> {taskDetails.BoardName}</li>
                                                                <li><strong>List:</strong> {taskDetails.ListName}</li>
                                                                <li><strong>Deadline:</strong> {existingTask.Finish_At.ToString("f")}</li>
                                                            </ul>
                                                            <p>Please check your task dashboard for more details.</p>
                                                            <a href='https://dainote.netlify.app/board-list/{taskDetails.BoardId}' class='button'>View Task Board</a>
                                                            <p>Best regards,<br><strong>DAI Team</strong></p>
                                                        </div>
                                                        <div class='footer'>
                                                            <p>This is an automated email. Do not reply to this message.</p>
                                                            <p>For support, visit our <a href='#'>Help Center</a>.</p>
                                                        </div>
                                                    </div>
                                                </body>
                                                </html>";

                try
                {
                    SendEmail(user.Email, emailMessage);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Message = "Failed to send notification email", Error = ex.Message });
                }
            }
        }

        ////Send email notification if assignee has changed
        //if (updatedTask.AssignTo.HasValue 
        //    //&& updatedTask.AssignTo != previousAssigneeId
        //    )
        //{
        //    var newAssignee = await _userManager.FindByIdAsync(updatedTask.AssignTo.ToString());
        //    if (newAssignee != null && !string.IsNullOrEmpty(newAssignee.Email))
        //    {
        //        // Fetch additional task details
        //        var taskDetails = await _taskRepository.GetTaskDetailsAsync(existingTask.Id);

        //        // Construct email message
        //        string emailMessage = $@"
        //                                <html>
        //                                <head>
        //                                    <style>
        //                                        body {{
        //                                            font-family: Arial, sans-serif;
        //                                            background-color: #f4f4f4;
        //                                            color: #333;
        //                                            margin: 0;
        //                                            padding: 0;
        //                                        }}
        //                                        .email-container {{
        //                                            max-width: 600px;
        //                                            margin: 20px auto;
        //                                            background: #ffffff;
        //                                            border-radius: 8px;
        //                                            overflow: hidden;
        //                                            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        //                                        }}
        //                                        .header {{
        //                                            background: #323338;
        //                                            color: white;
        //                                            text-align: center;
        //                                            padding: 20px;
        //                                            font-size: 24px;
        //                                            font-weight: bold;
        //                                        }}
        //                                        .content {{
        //                                            padding: 20px;
        //                                        }}
        //                                        .content p {{
        //                                            font-size: 16px;
        //                                            line-height: 1.5;
        //                                            margin-bottom: 20px;
        //                                        }}
        //                                        .task-details {{
        //                                            list-style: none;
        //                                            padding: 0;
        //                                            margin: 0;
        //                                        }}
        //                                        .task-details li {{
        //                                            margin: 10px 0;
        //                                            font-size: 14px;
        //                                            line-height: 1.5;
        //                                        }}
        //                                        .task-details strong {{
        //                                            color: #4CAF50;
        //                                        }}
        //                                        .button {{
        //                                            display: inline-block;
        //                                            background-color: #4CAF50;
        //                                            color: white;
        //                                            text-decoration: none;
        //                                            padding: 10px 20px;
        //                                            margin: 20px 0;
        //                                            border-radius: 5px;
        //                                            font-size: 16px;
        //                                        }}
        //                                        .button:hover {{
        //                                            background-color: #45a049;
        //                                        }}
        //                                        .footer {{
        //                                            text-align: center;
        //                                            background: #f4f4f4;
        //                                            padding: 10px;
        //                                            font-size: 12px;
        //                                            color: #666;
        //                                        }}
        //                                        .footer a {{
        //                                            color: #4CAF50;
        //                                            text-decoration: none;
        //                                        }}
        //                                    </style>
        //                                </head>
        //                                <body>
        //                                    <div class='email-container'>
        //                                        <div class='header'>
        //                                            Task Assignment Notification
        //                                        </div>
        //                                        <div class='content'>
        //                                            <p>Hello <strong>{newAssignee.FullName}</strong>,</p>
        //                                            <p>You have been assigned a new task:</p>
        //                                            <ul class='task-details'>
        //                                                <li><strong>Task:</strong> {existingTask.Title}</li>
        //                                                <li><strong>Description:</strong> {existingTask.Description}</li>
        //                                                <li><strong>Workspace:</strong> {taskDetails.WorkspaceName}</li>
        //                                                <li><strong>Board:</strong> {taskDetails.BoardName}</li>
        //                                                <li><strong>List:</strong> {taskDetails.ListName}</li>
        //                                                <li><strong>Deadline:</strong> {existingTask.Finish_At.ToString("f")}</li>
        //                                            </ul>
        //                                            <p>Please check your task dashboard for more details.</p>
        //                                            <a href='http://localhost:8080/board-list/{taskDetails.BoardId}' class='button'>View Task Board</a>
        //                                            <p>Best regards,<br><strong>DAI Team</strong></p>
        //                                        </div>
        //                                        <div class='footer'>
        //                                            <p>This is an automated email. Do not reply to this message.</p>
        //                                            <p>For support, visit our <a href='#'>Help Center</a>.</p>
        //                                        </div>
        //                                    </div>
        //                                </body>
        //                                </html>";


        //        try
        //        {
        //            SendEmail(newAssignee.Email, emailMessage);
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(new { Message = "Failed to send notification email", Error = ex.Message });
        //        }
        //    }
        //}


        return NoContent();
    }

    // DELETE: api/Task/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTask(Guid id)
    {
        var task = await _taskRepository.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        await _taskRepository.DeleteTaskAsync(id);
        return NoContent();
    }

    // GET: api/Task/user/{userId}
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<TaskModel>>> GetUserTasks(Guid userId)
    {
        var tasks = await _taskRepository.GetUserTasksAsync(userId);
        if (tasks == null)
        {
            return NotFound();
        }
        return Ok(tasks);
    }


    [HttpPut("moveTask")]
    public async Task<IActionResult> MoveTask([FromBody] MoveTaskRequest request)
    {
        if (request == null || request.DraggedTaskId == Guid.Empty || request.TargetTaskId == Guid.Empty)
        {
            return BadRequest("Invalid request");
        }

        var taskToMove = await _dragAndDropRepository.GetTaskByIdAsync(request.DraggedTaskId);
        var targetTask = await _dragAndDropRepository.GetTaskByIdAsync(request.TargetTaskId);

        if (taskToMove == null || targetTask == null)
        {
            return NotFound("Task not found");
        }

        await _dragAndDropRepository.UpdateTaskOrder(taskToMove, targetTask);
        Console.WriteLine("change task order inside a List");

        return Ok();
    }

    [HttpPut("moveTaskToListAtLastPosition")]
    public async Task<IActionResult> MoveTaskToList([FromBody] MoveTaskToListRequest request)
    {
        try
        {
            var taskToMove = await _dragAndDropRepository.GetTaskByIdAsync(request.TaskId);
            var targetList = await _dragAndDropRepository.GetListByIdAsync(request.TargetListId);

            if (taskToMove == null || targetList == null)
            {
                return NotFound("Task or target list not found.");
            }

            await _dragAndDropRepository.MoveTaskToList(taskToMove, targetList);

            return Ok(new { Message = "Task moved successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }


    [HttpPut("moveTaskToListWithTaskId")]
    public async Task<IActionResult> MoveTaskToAnotherList([FromBody] MoveTaskToAnotherListRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var taskToMove = request.DraggedTaskId;
            var targetMove = request.TargetTaskId;

            await _dragAndDropRepository.MoveTaskToAnotherListAsync(taskToMove, targetMove);

            return Ok(new { Message = "Task moved successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MoveTaskToAnotherList: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            return BadRequest(new { Message = ex.Message });
        }
    }


    private void SendEmail(string email, string messageBody)
    {
        string smtpEmail = _configuration["Smtp:Email"];
        string smtpPassword = _configuration["Smtp:Password"];
        string smtpHost = _configuration["Smtp:Host"];
        int smtpPort = int.Parse(_configuration["Smtp:Port"]);

        var emailMessage = new MimeMessage();
        emailMessage.To.Add(MailboxAddress.Parse(email));
        emailMessage.From.Add(MailboxAddress.Parse(smtpEmail));
        emailMessage.Subject = "Task Assignment Notification";
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = messageBody };

        using var smtp = new SmtpClient();
        smtp.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        smtp.Authenticate(smtpEmail, smtpPassword);
        smtp.Send(emailMessage);
        smtp.Disconnect(true);
    }




}