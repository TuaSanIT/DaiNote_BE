using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dai.core.Models
{
    public class RevokedToken
    {
        public int Id { get; set; }  
        public string Token { get; set; }  
        public string UserId { get; set; }  
        public DateTime RevokedAt { get; set; }  
        public DateTime Expiration { get; set; }

        public bool IsActive { get; set; }
    }

}
