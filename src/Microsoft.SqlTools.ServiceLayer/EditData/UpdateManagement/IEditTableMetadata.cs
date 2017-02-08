using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public interface IEditTableMetadata
    {

        IEnumerable<IEditColumnWrapper> Columns { get; }

        string EscapedMultipartName { get; }

        bool IsHekaton { get; }

        IEnumerable<IEditColumnWrapper> KeyColumns { get; }
    }
}
