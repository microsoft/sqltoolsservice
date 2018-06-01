using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// Summary description for IJobStepPropertiesControl.
	/// </summary>
	internal interface IJobStepPropertiesControl
	{
      void Load(JobStepData data);
      void Save(JobStepData data, bool isSwitching);
    }
}








