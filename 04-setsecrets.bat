REM This file contains the real secrets. It should be above the repo, and never
REM added to it.

call az keyvault secret set --name secret1 --vault-name UrlCheckerKVus --description "Test secret 1" ^
	--value "ACTUALSECRETVALUE"

call az keyvault secret set --name awsAccessKeyId --vault-name UrlCheckerKVus ^
	--value "accessKeyIdGoesHere"

call az keyvault secret set --name awsSecretAccessKey --vault-name UrlCheckerKVus ^
	--value "secretAccessKeyGoesHere"
