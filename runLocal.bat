REM Run the UrlChecker azure function locally.
REM If receive t as an option, use a proxy, so that fiddler can snoop.

SETLOCAL

set testUrl=http://127.0.0.1:7071/admin/functions/UrlChecker

if [%1] == [] goto noproxy
if %1==t (goto proxy) else (goto noproxy)

:proxy
curl --header "Content-Type: application/json" --request POST ^
    -x 127.0.0.1:8888 ^
    --data {input:\"test\"} %testUrl%
goto eof

:noproxy
curl --header "Content-Type: application/json" --request POST ^
    --data {input:\"test\"} %testUrl%

:eof
