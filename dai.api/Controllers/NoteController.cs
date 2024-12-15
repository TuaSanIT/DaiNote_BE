using AutoMapper;
using dai.core.DTO.Note;
using dai.core.DTO.Label;
using dai.core.DTO.NoteLabel;
using dai.core.Models;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPut("{id}/bookmarked")]
        public async Task<IActionResult> AddBookmark(Guid id, [FromBody] NoteDTO noteDto)
        {
            try
            {

                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound("Note not found");


                note.Bookmark = noteDto.Bookmark ?? false;


                var updatedNote = await _noteRepository.UpdateNoteAsync(note);  // Ensure you update, not add


                var updatedNoteDto = _mapper.Map<GetNoteDTO>(updatedNote);
                return Ok(updatedNoteDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> AddNote(Guid userId, [FromBody] NoteDTO noteDto)
        {
            try
            {
                var note = new NoteModel
                {
                    Title = noteDto.Title,
                    Description = noteDto.Description,
                    Color = noteDto.Color,
                    Bookmark = noteDto.Bookmark,
                    Created = DateTime.UtcNow,
                    Edited = DateTime.UtcNow,
                    UserId = userId
                };


                if (noteDto.Images != null && noteDto.Images.Any())
                {
                    note.Images = new List<byte[]>();
                    foreach (var image in noteDto.Images)
                    {
                        if (string.IsNullOrWhiteSpace(image))
                        {
                            return BadRequest("Image data cannot be empty or null.");
                        }

                        try
                        {
                            var imageBytes = Convert.FromBase64String(image);
                            note.Images.Add(imageBytes);
                        }
                        catch (FormatException)
                        {
                            return BadRequest("Invalid image format: Ensure the input is a valid Base-64 string.");
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
        public async Task<IActionResult> UpdateNote(Guid id, [FromBody] NoteDTO noteDto)
        {
            try
            {
                var note = await _noteRepository.GetNoteByIdAsync(id);
                if (note == null)
                    return NotFound();

                note.Title = noteDto.Title;
                note.Description = noteDto.Description;
                note.Color = noteDto.Color;
                note.Bookmark = noteDto.Bookmark;
                note.Edited = DateTime.UtcNow;


                if (noteDto.Images != null)
                {
                    if (noteDto.Images.Any())
                    {
                        note.Images = new List<byte[]>();
                        foreach (var image in noteDto.Images)
                        {
                            if (string.IsNullOrWhiteSpace(image))
                            {
                                return BadRequest("Image data cannot be empty or null.");
                            }

                            try
                            {
                                var imageBytes = Convert.FromBase64String(image);
                                note.Images.Add(imageBytes);
                            }
                            catch (FormatException)
                            {
                                return BadRequest("Invalid image format: Ensure the input is a valid Base-64 string.");
                            }
                        }
                    }
                    else
                    {

                        note.Images = new List<byte[]>();
                    }
                }

                await _noteRepository.UpdateNoteAsync(note);
                return Ok(note);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            await _noteRepository.DeleteNoteAsync(id);
            return NoContent();
        }


        [HttpGet("{noteId}/labels")]
        public async Task<IActionResult> GetLabelsByNoteId(Guid noteId)
        {
            var labels = await _noteLabelRepository.GetLabelsByNoteIdAsync(noteId);
            var labelDtos = _mapper.Map<IEnumerable<LabelDTO>>(labels ?? Enumerable.Empty<LabelModel>());
            return Ok(labelDtos);
        }


        [HttpPost("{noteId}/labels/batch")]
        public async Task<IActionResult> AddLabelsToNote(Guid noteId, [FromBody] List<Guid> labelIds)
        {
            if (labelIds == null || !labelIds.Any())
                return BadRequest(new { error = "labelIds cannot be null or empty." });

            var existingLabels = await _noteLabelRepository.GetLabelsByNoteIdAsync(noteId);
            var existingLabelIds = existingLabels.Select(label => label.Id).ToHashSet();

            var newNoteLabels = labelIds
                .Where(labelId => !existingLabelIds.Contains(labelId))
                .Select(labelId => new NoteLabelModel { NoteId = noteId, LabelId = labelId })
                .ToList();

            if (newNoteLabels.Any())
                await _noteLabelRepository.AddNoteLabelsAsync(newNoteLabels);

            return Ok();
        }


        [HttpDelete("{noteId}/labels/batch")]
        public async Task<IActionResult> RemoveLabelsFromNote(Guid noteId, [FromBody] List<Guid> labelIds)
        {
            if (labelIds == null || !labelIds.Any())
                return BadRequest(new { error = "labelIds cannot be null or empty." });

            var noteLabels = await Task.WhenAll(labelIds.Select(labelId =>
                _noteLabelRepository.GetNoteLabelAsync(noteId, labelId)
            ));

            var validNoteLabels = noteLabels.Where(nl => nl != null).ToArray();
            if (!validNoteLabels.Any())
                return NotFound(new { error = "No matching labels found to remove." });

            await _noteLabelRepository.DeleteNoteLabelsAsync(validNoteLabels);
            return NoContent();
        }


        [HttpGet("notesByLabel/{labelId}")]
        public async Task<IActionResult> GetNotesByLabelId(Guid labelId)
        {
            var notes = await _noteLabelRepository.GetNotesByLabelIdAsync(labelId);
            var noteDtos = _mapper.Map<IEnumerable<GetNoteDTO>>(notes);
            return Ok(noteDtos);
        }
    }
}