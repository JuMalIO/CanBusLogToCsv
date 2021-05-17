using System.Collections.Generic;

namespace CanBusLogToCsv.Models
{
    public class Description
    {
        public int Id { get; set; }
        public string IdHex { get; set; }
        public string Name { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public List<DescriptionExpression> Expressions { get; set; } = new List<DescriptionExpression>();
    }
}
