REM run the UrlChecker azure function, on azure.
REM If receive t as an option, use a proxy, so that fiddler can snoop.

@ECHO off
SETLOCAL
CALL readcfg.bat batch.cfg
@ECHO on

set liveUrl=https://%function%.azurewebsites.net/admin/functions/UrlChecker
set masterKey=<your _master App key goes here>

if [%1] == [] goto noproxy
if %1==t (goto proxy) else (goto noproxy)

:proxy
curl --header "Content-Type: application/json" ^
	--header "x-functions-key: %masterKey%" ^
	--request POST ^
	-x 127.0.0.1:8888 ^
	--data {input:\"test\"} ^
	%liveUrl%
goto eof

:noproxy
curl --header "Content-Type: application/json" ^
	--header "x-functions-key: %masterKey%" ^
	--request POST ^
	--data {input:\"test\"} ^
	%liveUrl%

:eof
