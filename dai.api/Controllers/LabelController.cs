using AutoMapper;
using dai.core.Models;
using dai.dataAccess.IRepositories;
using Microsoft.AspNetCore.Mvc;
using dai.core.Mapping;
using dai.core.DTO.Note;
using dai.core.DTO.Label;
using dai.core.DTO.NoteLabel;
using Org.BouncyCastle.Crypto;

namespace dai.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class LabelController : ControllerBase
    {
        private readonly ILabelRepository _labelRepository;
        private readonly IMapper _mapper;

        public LabelController(ILabelRepository labelRepository, IMapper mapper)
        {
            _labelRepository = labelRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLabels()
        {
            var labels = await _labelRepository.GetLabelsAsync();
            var labelDtos = _mapper.Map<IEnumerable<GetLabelDTO>>(labels);
            return Ok(labelDtos);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetLabelsByUserId(Guid userId)
        {
            var labels = await _labelRepository.GetLabelsByUserIdAsync(userId);
            var labelDtos = _mapper.Map<IEnumerable<GetLabelDTO>>(labels);
            return Ok(labelDtos);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> AddLabel(Guid userId, [FromBody] LabelDTO labelDto)
        {
            var label = new LabelModel
            {
                Name = labelDto.Name,
                Created = DateTime.UtcNow,
                Edited = DateTime.UtcNow
            };

            await _labelRepository.AddLabelAsync(userId, label);
            return Ok(label);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLabel(Guid id, [FromBody] LabelDTO labelDto)
        {
            var label = await _labelRepository.GetLabelByIdAsync(id);
            if (label == null)
                return NotFound();

            label.Name = labelDto.Name;
            label.Edited = DateTime.UtcNow;

            await _labelRepository.UpdateLabelAsync(label);
            return Ok(label);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLabel(Guid id)
        {
            await _labelRepository.DeleteLabelAsync(id);
            return NoContent();
        }
    }
}