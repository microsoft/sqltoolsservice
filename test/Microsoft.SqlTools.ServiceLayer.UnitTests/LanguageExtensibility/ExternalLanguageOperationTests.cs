using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageExtensibility
{
    public class ExternalLanguageOperationTests
    {
        [Fact]
        public void VerifyDeleteLanguageWithInvalidName()
        { 
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage language = new ExternalLanguage();
            Verify(language, (connection, lang, commandMock) =>
            {
                Assert.Throws<LanguageExtensibilityException>(() => operations.DeleteLanguage(connection, language.Name));
                return true;
            });
        }

        [Fact]
        public void VerifyDeleteLanguage()
        {
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage language = new ExternalLanguage()
            {
                Name = "name"
            };
            Verify(language, (connection, lang, commandMock) =>
            {
                operations.DeleteLanguage(connection, language.Name);
                commandMock.VerifySet(x => x.CommandText = It.Is<string>(s => s.Contains(ExternalLanguageOperations.DropScript)));
                return true;
            });
        }

        [Fact]
        public void VerifyCreateLanguage()
        {
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage newLanguage = new ExternalLanguage()
            {
                Name = "newLang",
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath"
                    }
                }
            };
            Verify(null, (connection, lang, commandMock) =>
            {
                operations.UpdateLanguage(connection, newLanguage);
                commandMock.VerifySet(x => x.CommandText = It.Is<string>(s => s.Contains(ExternalLanguageOperations.CreateScript)));
                return true;
            });
        }

        [Fact]
        public void VerifyCreateLanguageWithLocalFile()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                ExternalLanguageOperations operations = new ExternalLanguageOperations();
                ExternalLanguage newLanguage = new ExternalLanguage()
                {
                    Name = "newLang",
                    Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = true,
                        PathToExtension = queryTempFile.FilePath
                    }
                }
                };
                Verify(null, (connection, lang, commandMock) =>
                {
                    operations.UpdateLanguage(connection, newLanguage);
                    commandMock.VerifySet(x => x.CommandText = It.Is<string>(s => s.Contains(ExternalLanguageOperations.CreateScript)));
                    return true;
                });
            }
        }

        [Fact]
        public void VerifyUpdateLanguage()
        {
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage language = new ExternalLanguage()
            {
                Name = "name",
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath",
                        Platform = "WINDOWS"
                    }
                }
            };
            ExternalLanguage newLanguage = new ExternalLanguage()
            {
                Name = language.Name,
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath",
                        Platform = "LINUX"
                    }
                }
            };
            Verify(language, (connection, lang, commandMock) =>
            {
                operations.UpdateLanguage(connection, newLanguage);
                commandMock.VerifySet(x => x.CommandText = It.Is<string>(
                    s => s.Contains(ExternalLanguageOperations.AlterScript) 
                 && s.Contains(ExternalLanguageOperations.AddContentScript)));
                return true;
            });
        }

        [Fact]
        public void VerifyUpdateContentLanguage()
        {
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage language = new ExternalLanguage()
            {
                Name = "name",
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath"
                    }
                }
            };
            ExternalLanguage newLanguage = new ExternalLanguage()
            {
                Name = language.Name,
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath"
                    }
                }
            };
            Verify(language, (connection, lang, commandMock) =>
            {
                operations.UpdateLanguage(connection, newLanguage);
                commandMock.VerifySet(x => x.CommandText = It.Is<string>(
                    s => s.Contains(ExternalLanguageOperations.AlterScript)
                 && s.Contains(ExternalLanguageOperations.SetContentScript)));
                return true;
            });
        }

        [Fact]
        public void VerifyRemoveContentLanguage()
        {
            ExternalLanguageOperations operations = new ExternalLanguageOperations();
            ExternalLanguage language = new ExternalLanguage()
            {
                Name = "name",
                Contents = new List<ExternalLanguageContent>()
                {
                    new ExternalLanguageContent
                    {
                        IsLocalFile = false,
                        ExtensionFileName = "filepath"
                    }
                }
            };
            ExternalLanguage newLanguage = new ExternalLanguage()
            {
                Name = language.Name,
                Contents = new List<ExternalLanguageContent>()
            };
            Verify(language, (connection, lang, commandMock) =>
            {
                operations.UpdateLanguage(connection, newLanguage);
                commandMock.VerifySet(x => x.CommandText = It.Is<string>(
                    s => s.Contains(ExternalLanguageOperations.AlterScript)
                 && s.Contains(ExternalLanguageOperations.RemoveContentScript)));
                return true;
            });
        }

        private IDbConnection Verify(ExternalLanguage language, Func<IDbConnection, ExternalLanguage, Mock<IDbCommand>, bool> func)
        {
            Mock<IDbConnection> connectionMock = new Mock<IDbConnection>();
            Mock<IDbCommand> commandMock = new Mock<IDbCommand>();
            Mock<IDbDataParameter> dbDataParamMock = new Mock<IDbDataParameter>();
            Mock<IDataParameterCollection> dbDataParametersMock = new Mock<IDataParameterCollection>();
            Mock<IDataReader> dataReaderMock = new Mock<IDataReader>();
            bool dataReaderHasValues = language != null;
            dataReaderMock.Setup(x => x.Read()).Returns(() => dataReaderHasValues).Callback(() =>
            {
                dataReaderHasValues = false;
            }
            );
            if (language != null)
            {
                ExternalLanguageContent content = language.Contents == null || language.Contents.Count == 0 ? null : language.Contents[0];

                dataReaderMock.Setup(x => x.GetInt32(0)).Returns(1);
                dataReaderMock.Setup(x => x.IsDBNull(1)).Returns(language.Name == null);
                dataReaderMock.Setup(x => x.GetString(1)).Returns(language.Name);

                dataReaderMock.Setup(x => x.IsDBNull(2)).Returns(language.CreatedDate == null);
                if (language.CreatedDate != null)
                {
                    dataReaderMock.Setup(x => x.GetDateTime(2)).Returns(DateTime.Parse(language.CreatedDate));
                }
                dataReaderMock.Setup(x => x.IsDBNull(3)).Returns(language.Owner == null);
                if (language.Owner != null)
                {
                    dataReaderMock.Setup(x => x.GetString(3)).Returns(language.Owner);
                }
                dataReaderMock.Setup(x => x.IsDBNull(5)).Returns(content == null || content.ExtensionFileName == null);
                if (content != null && content.ExtensionFileName != null)
                {
                    dataReaderMock.Setup(x => x.GetString(5)).Returns(content.ExtensionFileName);
                }
                dataReaderMock.Setup(x => x.IsDBNull(6)).Returns(content == null || content.Platform == null);
                if (content != null && content.Platform != null)
                {
                    dataReaderMock.Setup(x => x.GetString(6)).Returns(content.Platform);
                }
                dataReaderMock.Setup(x => x.IsDBNull(7)).Returns(content == null || content.Parameters == null);
                if (content != null && content.Parameters != null)
                {
                    dataReaderMock.Setup(x => x.GetString(7)).Returns(content.Parameters);
                }
                dataReaderMock.Setup(x => x.IsDBNull(8)).Returns(content == null || content.EnvironmentVariables == null);
                if (content != null && content.EnvironmentVariables != null)
                {
                    dataReaderMock.Setup(x => x.GetString(8)).Returns(content.EnvironmentVariables);
                }
            }
            dbDataParametersMock.Setup(x => x.Add(It.IsAny<object>()));
            dbDataParamMock.Setup(x => x.ParameterName);
            dbDataParamMock.Setup(x => x.Value);
            commandMock.Setup(x => x.CreateParameter()).Returns(dbDataParamMock.Object);
            commandMock.Setup(x => x.Parameters).Returns(dbDataParametersMock.Object);
            commandMock.SetupSet(x => x.CommandText = It.IsAny<string>());
            commandMock.Setup(x => x.ExecuteNonQuery());
            commandMock.Setup(x => x.ExecuteReader()).Returns(dataReaderMock.Object);
            connectionMock.Setup(x => x.CreateCommand()).Returns(commandMock.Object);
            func(connectionMock.Object, language, commandMock);

            return connectionMock.Object;
        }
    }
}
