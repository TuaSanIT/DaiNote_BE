using AutoMapper;
using dai.core.DTO.Note;
using dai.core.DTO.Label;
using dai.core.DTO.NoteLabel;
using dai.core.Models;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using dai.api.Services.ServiceExtension;

namespace dai.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteController : ControllerBase
    {
        private readonly INoteRepository _noteRepository;
        private readonly INoteLabelRepository _noteLabelRepository;
        private readonly IMapper _mapper;

        public NoteController(INoteRepository noteRepository, IMapper mapper, INoteLabelRepository noteLabelRepository)
        {
            _noteRepository = noteRepository;
            _mapper = mapper;
            _noteLabelRepository = noteLabelRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNotes()
        {
            var notes = await _noteRepository.GetNotesAsync();
            var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
            return Ok(noteDtos);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAllNotes(Guid userId)
        {
            var notes = await _noteRepository.GetNotesWithUserIdAsync(userId);
            var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
            return Ok(noteDtos);
        }

        [HttpGet("ArhciveNote/{userId}")]
        public async Task<IActionResult> GetArchiveNotes(Guid userId)
        {
            var notes = await _noteRepository.GetArchivedNotesAsync(userId);
            var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
            return Ok(noteDtos);
        }

        [HttpGet("TrashNote/{userId}")]
        public async Task<IActionResult> GetTrashNotes(Guid userId)
        {
            var notes = await _noteRepository.GetTrashedNotesAsync(userId);
            var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
            return Ok(noteDtos);
        }

        [HttpPut("{id}/bookmarked")]
        public async Task<IActionResult> ToggleBookmark(Guid id)
        {
            try
            {
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found");

                // Handle null bookmark by treating it as false
                note.Bookmark = note.Bookmark ?? false;
                note.Bookmark = !note.Bookmark;

                var updatedNote = await _noteRepository.UpdateNoteAsync(note);
                var updatedNoteDto = _mapper.Map<GetNoteDTO>(updatedNote);
                return Ok(updatedNoteDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/archived")]
        public async Task<IActionResult> ToggleArchive(Guid id)
        {
            try
            {
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found");

                // Handle null bookmark by treating it as false
                note.Archive = note.Archive ?? false;
                note.Archive = !note.Archive;


                var updatedNote = await _noteRepository.UpdateNoteAsync(note);
                var updatedNoteDto = _mapper.Map<GetNoteDTO>(updatedNote);
                return Ok(updatedNoteDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/trashed")]
        public async Task<IActionResult> ToggleTrash(Guid id, [FromQuery] string? timeZoneId)
        {
            try
            {
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found");

                // Handle toggling trash status
                note.Trash = !(note.Trash ?? false);
                note.TrashIsNotified = false;
                note.Archive = false; // Ensure it's not archived when trashed

                // Handle time zone for TrashDate
                TimeZoneInfo userTimeZone;
                if (!string.IsNullOrEmpty(timeZoneId))
                {
                    try
                    {
                        userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        return BadRequest("Invalid time zone specified.");
                    }
                }
                else
                {
                    userTimeZone = TimeZoneInfo.Utc; // Default to UTC if no time zone provided
                }

                // Set or reset TrashDate
                if (note.Trash == true)
                {
                    note.TrashDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, userTimeZone);
                }
                else
                {
                    note.TrashDate = DateTime.MinValue; // Clear TrashDate if untrashed
                }

                var updatedNote = await _noteRepository.UpdateNoteAsync(note);
                var updatedNoteDto = _mapper.Map<GetNoteDTO>(updatedNote);
                return Ok(updatedNoteDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("{userId}")]
        public async Task<IActionResult> AddNote(Guid userId, [FromForm] NoteDTO noteDto, [FromServices] AzureBlobService azureBlobService)
        {
            try
            {
                var note = new NoteModel
                {
                    Title = noteDto.Title,
                    Description = noteDto.Description,
                    Color = noteDto.Color,
                    Bookmark = noteDto.Bookmark ?? false, // Default to false if null
                    Archive = noteDto.Archive ?? false,
                    Reminder = noteDto.Reminder,
                    Created = DateTime.UtcNow,
                    Edited = DateTime.UtcNow,
                    UserId = userId,
                    Images = new List<string>()
                };

                if (noteDto.Images != null && noteDto.Images.Any())
                {
                    foreach (var file in noteDto.Images)
                    {
                        if (file.Length > 0)
                        {
                            using (var fileStream = file.OpenReadStream())
                            {
                                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                                var imageUrl = await azureBlobService.UploadNoteImageAsync(fileStream, "dainotecontainer", "note-images", fileName);
                                note.Images.Add(imageUrl);
                            }
                        }
                    }
                }

                await _noteRepository.AddNoteAsync(userId, note);
                return Ok(note);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(Guid id, [FromForm] NoteDTO noteDto, [FromServices] AzureBlobService azureBlobService)
        {
            try
            {
                // Retrieve the existing note
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found.");

                // Update basic properties
                note.Title = noteDto.Title;
                note.Description = string.IsNullOrWhiteSpace(noteDto.Description) ? "" : noteDto.Description; // Default to empty string
                note.Color = noteDto.Color;
                note.Reminder = noteDto.Reminder;
                note.Edited = DateTime.UtcNow;

                // Retain existing images
                var updatedImages = new List<string>(note.Images ?? new List<string>());

                // Remove specified images
                if (noteDto.DeletedImages != null && noteDto.DeletedImages.Any())
                {
                    foreach (var imageUrl in noteDto.DeletedImages)
                    {
                        if (updatedImages.Contains(imageUrl))
                        {
                            await azureBlobService.DeleteNoteImageAsync(imageUrl);
                            updatedImages.Remove(imageUrl);
                        }
                    }
                }

                // Add new images
                if (noteDto.Images != null && noteDto.Images.Any())
                {
                    foreach (var file in noteDto.Images)
                    {
                        if (file.Length > 0)
                        {
                            using (var fileStream = file.OpenReadStream())
                            {
                                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                                var imageUrl = await azureBlobService.UploadNoteImageAsync(fileStream, "dainotecontainer", "note-images", fileName);
                                updatedImages.Add(imageUrl);
                            }
                        }
                    }
                }

                // Update the note's image list
                note.Images = updatedImages;

                // Save changes to the database
                await _noteRepository.UpdateNoteAsync(note);
                return Ok(note);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id, [FromServices] AzureBlobService azureBlobService)
        {
            try
            {
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found.");

                if (note.Images != null && note.Images.Any())
                {
                    foreach (var imageUrl in note.Images)
                    {
                        await azureBlobService.DeleteNoteImageAsync(imageUrl);
                    }
                }

                await _noteRepository.DeleteNoteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Fetch labels for a note
        [HttpGet("{noteId}/labels")]
        public async Task<IActionResult> GetLabelsForNote(Guid noteId)
        {
            var labels = await _noteLabelRepository.GetLabelsByNoteIdAsync(noteId);
            var labelDtos = _mapper.Map<IEnumerable<GetLabelDTO>>(labels);
            return Ok(labelDtos);
        }

        [HttpPost("{noteId}/labels")]
        public async Task<IActionResult> AddLabelsToNote(Guid noteId, [FromBody] List<Guid> labelIds)
        {
            if (labelIds == null || !labelIds.Any())
                return BadRequest("Label IDs cannot be null or empty.");

            await _noteLabelRepository.AddNoteLabelsAsync(noteId, labelIds);
            return Ok(new { message = "Labels added successfully." });
        }

        [HttpPut("{noteId}/labels")]
        public async Task<IActionResult> UpdateLabelsForNote(Guid noteId, [FromBody] List<Guid> labelIds)
        {
            if (labelIds == null)
                return BadRequest("Label IDs cannot be null.");

            await _noteLabelRepository.UpdateNoteLabelsAsync(noteId, labelIds);
            return Ok(new { message = "Labels updated successfully." });
        }

        // Fetch notes by label
        [HttpGet("notesByLabel/{labelId}")]
        public async Task<IActionResult> GetNotesByLabel(Guid labelId)
        {
            try
            {
                var notes = await _noteLabelRepository.GetNotesByLabelIdAsync(labelId);

                if (!notes.Any())
                    return NotFound(new { Message = "No notes found for the given label." });

                var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
                return Ok(noteDtos);
            }
            catch (Exception ex)
            {
                // Log the error (consider using a logging library)
                return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
            }
        }

    }
}