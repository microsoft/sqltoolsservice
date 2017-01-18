echo [{ > %USERPROFILE%\testServerNames.json
echo		 "serverName": "<Server Name>", >> %USERPROFILE%\testServerNames.json
echo		 "profileName": "<Profile Name>", >> %USERPROFILE%\testServerNames.json
echo		 "serverType": "<server type>"  >> %USERPROFILE%\testServerNames.json
echo }] >> %USERPROFILE%\testServerNames.json

SET TestServerNamesFile=%USERPROFILE%\testServerNames.json
REM  The server name setting template is created here: "%USERPROFILE%\testServerNames.json". Make sure to add the server names before running the tests