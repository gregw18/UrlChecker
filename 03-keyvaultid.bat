REM Create system-assigned role for the function. Would like to then
REM add that id to the keyvault, but commands weren't working when I tested.

@ECHO off
SETLOCAL
CALL readcfg.bat batch.cfg

call az functionapp identity assign -g %resrcGroup% -n %functionApp%
