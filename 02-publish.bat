REM Publish the function app to Azure
REM Remember to run from the func directory.

@ECHO off
SETLOCAL
CALL ..\readcfg.bat ..\batch.cfg

func azure functionapp publish %function% --publish-local-settings
