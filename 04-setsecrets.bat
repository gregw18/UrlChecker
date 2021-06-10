REM This file contains the real secrets. It should be above the repo, and never
REM added to it.

@ECHO off
SETLOCAL
CALL urlchecker\readcfg.bat urlchecker\batch.cfg

call az keyvault secret set --name secret1 --vault-name %keyVault% --description "Test secret 1" ^
	--value "ACTUALSECRETVALUE"

call az keyvault secret set --name awsAccessKeyId --vault-name %keyVault% ^
	--value "accessKeyIdGoesHere"

call az keyvault secret set --name awsSecretAccessKey --vault-name %keyVault% ^
	--value "secretAccessKeyGoesHere"
