using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.DTO.Chat
{
    public class FileUploadModel
    {
        [Required]
        public IFormFile File { get; set; }

        public string Message { get; set; }

        [Required]
        public Guid ChatRoomId { get; set; }
    }

}
