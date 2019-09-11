using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTesting
{
    class Domain
    {
        public Domain(string domainName, DateTime createdDate, DateTime experationDate)
        {
            DomainName = domainName;
            CreationDate = createdDate;
            ExperationDate = experationDate;
            OwnedTimespan = new TimeSpan((DateTime.Now - CreationDate).Ticks);
        }

        public string DomainName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExperationDate { get; set; }
        public TimeSpan OwnedTimespan { get; }
        public string Response { get; set; }

        public override string ToString()
        {
            return $"{DomainName}, {CreationDate.ToString()}, {ExperationDate.ToString()}, {OwnedTimespan.Days}, {Response}";
        }
    }
}
