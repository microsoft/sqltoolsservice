echo "mssql.connections": [ { > %USERPROFILE%\settings.json
echo		 "server": "<Server Name>", >> %USERPROFILE%\settings.json
echo		 "authenticationType": "SqlLogin", >> %USERPROFILE%\settings.json
echo		 "user": "<User ID>", >> %USERPROFILE%\settings.json
echo		 "password": "<Password>", >> %USERPROFILE%\settings.json
echo		 "serverType": "OnPrem" },{ >> %USERPROFILE%\settings.json
echo		 "server": "<Azure Server Name>", >> %USERPROFILE%\settings.json
echo		 "authenticationType": "SqlLogin", >> %USERPROFILE%\settings.json
echo		 "user": "<User ID>", >> %USERPROFILE%\settings.json
echo		 "password": "<Password>", >> %USERPROFILE%\settings.json
echo		 "serverType": "Azure" } >> %USERPROFILE%\settings.json
echo  ] >> %USERPROFILE%\settings.json

SET SettingsFileName=%USERPROFILE%\settings.json
REM  The connection setting template is created here: "%USERPROFILE%\settings.json". Make sure to add the connection info before running the tests