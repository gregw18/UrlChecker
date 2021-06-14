REM Create resource group and key vault, configure the vault to allow func to access.
REM Remember that have to be logged in before run this.

@ECHO off
SETLOCAL
CALL readcfg.bat batch.cfg

call az group create --name %resrcGroup% --location %azLocation%

call az keyvault create --resource-group %resrcGroup% --name %keyVault% ^
--enabled-for-deployment -l %azLocation%

call az functionapp create --name %functionApp% --storage-account %storageAccount% ^
  --consumption-plan-location %azLocation% ^
  --resource-group %resrcGroup% --functions-version 3