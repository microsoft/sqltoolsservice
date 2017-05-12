using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    //TODO: Should URL be added to Carbon?
    public partial class UrlControl
	{
		/// <summary>
		/// Server
		/// </summary>
		public Microsoft.SqlServer.Management.Smo.Server SqlServer;
        
		/// <summary>
		/// list of Backup Urls
		/// </summary>
		private ArrayList listBakDestUrls;

		public UrlControl()
		{			
		}

		/// <summary>
		/// List of backup urls
		/// </summary>
		public ArrayList ListBakDestUrls
		{
			get
			{
				return this.listBakDestUrls;
			}
		}
        
	}
}
