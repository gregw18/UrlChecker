REM Publish the function app to Azure

@ECHO off
SETLOCAL
CALL readcfg.bat batch.cfg

cd func
func azure functionapp publish %function% --publish-local-settings
