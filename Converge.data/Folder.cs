using Converge.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Converge.Data
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public Folder Parent { get; set; }
        public ICollection<Folder> Children { get; set; } = new List<Folder>();
    }
}
