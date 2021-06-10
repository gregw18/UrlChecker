REM Create system-assigned role for the function, then
REM add that id to the keyvault.

@ECHO off
SETLOCAL
CALL readcfg.bat batch.cfg

call az functionapp identity assign -g %resrcGroup% -n %function%
