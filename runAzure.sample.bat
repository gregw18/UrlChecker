REM run the UrlChecker azure function, on azure.
REM If receive t as an option, use a proxy, so that fiddler can snoop.

if [%1] == [] goto noproxy
if %1==t (goto proxy) else (goto noproxy)

:proxy
curl --header "Content-Type: application/json" ^
	--header "x-functions-key: <your _master App key here>" ^
	--request POST ^
	-x 127.0.0.1:8888 ^
	--data {input:\"test\"} ^
	https://UrlCheckerGAW.azurewebsites.net/admin/functions/UrlChecker
goto eof

:noproxy
curl --header "Content-Type: application/json" ^
	--header "x-functions-key: <your _master App key here>" ^
	--request POST ^
	--data {input:\"test\"} ^
	https://UrlCheckerGAW.azurewebsites.net/admin/functions/UrlChecker

:eof
