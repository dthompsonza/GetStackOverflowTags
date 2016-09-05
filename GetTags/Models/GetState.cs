using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetTags.Models
{
    public class GetState
    {
        public int currentPage { get; set; } = 0;
        public int pageSize { get; set; } = 100;
        public bool has_more { get; set; } = false;
        public int quota_max { get; set; } = 0;
        public int quota_remaining { get; set; } = 0;
    }
}
