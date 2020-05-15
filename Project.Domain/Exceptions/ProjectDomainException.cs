using System;
using System.Collections.Generic;
using System.Text;

namespace Project.Domain.Exceptions
{
    /// <summary>
    /// 在Domian层产生异常时，可以抛出
    /// </summary>
    public class ProjectDomainException:Exception
    {
        public ProjectDomainException() { }

        public ProjectDomainException(string message):base(message) { }

        public ProjectDomainException(string message,Exception innerException) : base(message, innerException) { }
    }
}
