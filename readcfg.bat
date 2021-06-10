REM Reads in config values from provided file name (first parameter.)
REM Assumes every line is of the form "key:value".
REM For each line, creates an environment variable named key
REM and assigns it value. i.e. "set key=value". 
REM Caller can then use %key% to get the value.
REM Most command line special characters cannot be used in the file -
REM &, %, ), <, > and |. Most of these
REM will cause this batch file to crash, while some may simply disappear.
REM The values in my use case shouldn't contain any of these, so
REM I didn't put much effort into trying to fix this.

FOR /f "delims=" %%i IN (%1) DO (
 CALL :parseline "%%i"
)
EXIT /b

:parseline
SET linetosplit=%1
FOR /f "delims=:" %%a IN ("%linetosplit%") DO SET name=%%a
 SET %name%=%linetosplit:*:=%
EXIT /b
