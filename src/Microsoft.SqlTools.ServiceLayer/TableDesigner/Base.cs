//------------------------------------------------------------------------------
// <copyright file="Base.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.ComponentModel;
using Microsoft.Data.Relational.Design.Table;
using Microsoft.Data.Tools.Design.Core.Context;
using Microsoft.Data.Tools.Schema.Sql.DesignServices;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel;

namespace Microsoft.Data.Relational.Design.VM
{
    internal abstract class Base : INotifyPropertyChanged
    {
        internal Base(ISqlModelElement sqlModelElement)
        {
            this.SqlModelElement = sqlModelElement;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ISqlModelElement SqlModelElement { get; protected set; }

        public SqlSchemaModel SqlSchemaModel
        {
            get { return this.SqlModelElement.Model as SqlSchemaModel; }
        }

        public abstract EditingContext GetEditingContext();

        protected ISqlModelUpdatingService GetPersistDesignerChangeService()
        {
            EditingContext context = GetEditingContext();
            if (context != null)
            {
                return context.Items.GetValue<ContextItem<ISqlModelUpdatingService>>().Object;
            }
            return null;
        }

        protected void OnPropertyChanged(string propName)
        {
            if (this.SqlModelElement != null && this.SqlModelElement.IsDeleted())
            {
                // view model will be re-attached to model element
                return;
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        protected bool PerformEdit(VMUtils.ModelUpdateOperation operation, bool refreshItem = true, bool refreshDesignerState = true)
        {
            // TODO
            // bool succeeded = (VMUtils.PerformEdit(this.GetEditingContext(), operation, refreshDesignerState) == PerformEditResult.Success);
            bool succeeded = true;

            if (refreshItem || !succeeded)
            {
                // refresh item after edit (even if edit operation failed,
                // in which case we need to revert to original value)
                OnPropertyChanged(null);
            }
            return succeeded;
        }
    }
}
