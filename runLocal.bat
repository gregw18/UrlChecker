REM @echo off
REM if %1%==t (SET proxy = "-x 127.0.0.1:8888") else (SET proxy = "")
REM if %1%==t (dir) else (dir *.bat)

if [%1] == [] goto noproxy
if %1==t (goto proxy) else (goto noproxy)

:proxy
echo proxy
curl --header "Content-Type: application/json" --request POST -x 127.0.0.1:8888 --data {input:\"test\"} http://127.0.0.1:7071/admin/functions/TimerTriggerCSharp1
goto eof

:noproxy
curl --header "Content-Type: application/json" --request POST %proxy% --data {input:\"test\"} http://127.0.0.1:7071/admin/functions/TimerTriggerCSharp1

:eof
