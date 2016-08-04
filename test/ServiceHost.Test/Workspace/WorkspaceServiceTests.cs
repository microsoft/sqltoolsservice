// //
// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
// //

// using Microsoft.SqlTools.ServiceLayer.LanguageServices;
// using Microsoft.SqlTools.ServiceLayer.WorkspaceServices.Contracts;
// using Microsoft.SqlTools.Test.Utility;
// using Xunit;

// namespace Microsoft.SqlTools.ServiceLayer.Test.Workspace
// {
//     /// <summary>
//     /// Tests for the ServiceHost Language Service tests
//     /// </summary>
//     public class WorkspaceServiceTests
//     {
        
//         [Fact]
//         public async Task ServiceLoadsProfilesOnDemand()
//         {
//             // Given an event detailing 

//             // when 
//             // Send the configuration change to cause profiles to be loaded
//             await this.languageServiceClient.SendEvent(
//                 DidChangeConfigurationNotification<LanguageServerSettingsWrapper>.Type,
//                 new DidChangeConfigurationParams<LanguageServerSettingsWrapper>
//                 {
//                     Settings = new LanguageServerSettingsWrapper
//                     {
//                         Powershell = new LanguageServerSettings
//                         {
//                             EnableProfileLoading = true,
//                             ScriptAnalysis = new ScriptAnalysisSettings
//                             {
//                                 Enable = false
//                             }
//                         }
//                     }
//                 });

//             OutputReader outputReader = new OutputReader(this.protocolClient);

//             Task<EvaluateResponseBody> evaluateTask =
//                 this.SendRequest(
//                     EvaluateRequest.Type,
//                     new EvaluateRequestArguments
//                     {
//                         Expression = "\"PROFILE: $(Assert-ProfileLoaded)\"",
//                         Context = "repl"
//                     });

//             // Try reading up to 10 lines to find the expected output line
//             string outputString = null;
//             for (int i = 0; i < 10; i++)
//             {
//                 outputString = await outputReader.ReadLine();

//                 if (outputString.StartsWith("PROFILE"))
//                 {
//                     break;
//                 }
//             }

//             // Delete the test profile before any assert failures
//             // cause the function to exit
//             File.Delete(currentUserCurrentHostPath);

//             // Wait for the selection to appear as output
//             await evaluateTask;
//             Assert.Equal("PROFILE: True", outputString);
//         }


//     }
// }

