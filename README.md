# URL Checker
This is a project that I put together to monitor Canada's vaccine delivery web page (https://www.canada.ca/en/public-health/services/diseases/2019-novel-coronavirus-infection/prevention-risks/covid-19-vaccine-treatment/vaccine-rollout.html) for changes so that I could track whether suppliers were delivering on time, and whether delivery dates were being met or constantly adjusted. (Summary - Pfizer/BioNTech are amazing, Moderna is really struggling.) I've tried to generalize it so that it can monitor any static web page for changes, and also to monitor multiple pages. When a change is detected, an email notification is sent.
It monitors a given page by watching a specific, configured element for a change. I.e. for my original target, there is a "dateModified" element near the bottom of the page that is updated whenever the page is changed. The program retrieves the full text of the page, finds the last instance of the hardcoded text "dateModified", then looks for the changed value starting two characters after the end of the hardcoded text, for the next ten characters. It compares this value to the saved previous value, to see if it changed. 
The url to retrieve, the target text that is searched for, the offset from the end of that text to the start of the changing value and the length of the changing value are all configurable, for as many urls as desired.
This program only works with static web pages - the text that you want to monitor has to be populated when the page first loads. I've included proof of concept code for parsing dynamic pages but it uses Selenium and headless Chrome, which I didn't want to try installing under Azure Functions.
This C#/.NET Core code, intended to run on a timer as a Microsoft Azure Function, allows you configure a list of urls to check, and send an email (via AWS SNS) when one or more have changed. For each url you configure the target text, eg. a control name, that is before the text that you want to actually monitor, along with the number of characters from the end of the targeted text and the beginning of the text to monitor, and then length of the text to monitor. It uses an Azure Key Vault to store the SNS credentials and an Azure Files share to store the previous values.
This was a project intended to give me a bit of experience with C# and Microsoft Azure, while also accessing AWS - since Azure doesn't have any built-in email functionality. Other than storing the AWS credentials securely, accessing the AWS service from Azure was surprisingly easy (and the first bills don't indicate any "hey - what are you using those other guys for?" type charges.) When running twice a day, monitoring two urls, this function appears to be sneaking under the billable thresholds for both providers.

## Getting Started
As a cloud-based project, using both Azure and AWS, getting this running requires a lot of setup and configuration. I assume that you already have Azure and AWS accounts, with a default storage account for Azure, and the CLIs for both installed. Having the Azure Functions tools installed is also recommended. There are batch files for creating some of the required resources while others are created programmatically, but some may need to be created manually. There is also quite a bit of configuration required - sample files are provided.
1. If you haven't already logged in to Azure, use 00-login.bat to do so. Modify 01-keyvault.bat to uncomment the az group create command and modify the desired values for the name of the group (default UrlChecker), the region (default eastus) and the name of the vault (default UrlCheckerKVUS.) Save 01-keyvault.bat and run it, to create a group for the function and the corresponding key vault.
2. Copy the sample local.settings.sample.json in the func directory to local.settings.json and update the vaultName in func\local.settings.json to your chosen value.
3. Configure the access key for your storage account. Pick the Azure storage account that you want to use with this function - either the default one automatically created by Azure with the account, or one that you have manually created. Go to the portal for that account and select "Access keys" under the "Security + networking" section, copy one of the connection strings (they seem to start with "DefaultEndpointsProtocol") and copy that into the "AzureWebJobsStorage" entry of local.settings.json. Then, set the path to the file that will be used to store the previous values - the shareName, dirName and lastChangedFileName. Note that the share, directory and file will all automatically be created if they don't already exist.
4. While in that file, you may as well also set your aws region in the awsRegionName setting, and the name of the SNS topic to publish to, in snsTopic.
5. Configure the web site(s) that you want to monitor, again in local.settings.json. For the first site, set the webSiteUrl0, targetText0, targetTextOffset0 and targetTextLength0 settings local.settings.json file. The targetText is the fixed (i.e. unchanging) text used to locate the text that changes, targetTextOffset is the number of characters from the end of the target text to the beginning of the text that changes and the targetTextLength is the number of characters to monitor from that position. The corresponding settings for the second url end with 1 rather than 0, and you can add as many more sets as you like, with increasing indexes. (The parser stops reading when it hits the first webSiteUrl? entry that doesn't exist.) 
getHtml.ps1 is a PowerShell script that can be used to retrieve the static results from a web page, to look for targets near text that changes. Set the URL in the file and change the name of the file to save the results in (the default is html.txt) then run the script. By default Windows will no longer let you run a PowerShell script from a normal command line, but you can enable it - see (https://superuser.com/questions/106360/how-to-enable-execution-of-powershell-scripts).
6. Publish the function to Azure. First, open 02-publish.bat and give your function a name (default value UrlCheckerGAW.) Save and run the batch file.
7. You then need to create an identity for the group/function and give that identity access to the key vault. 03-keyvaultid.bat may let you do this (first update the group and function names to match those set in the previous steps), but, in April, 2021 when I tried it, the az keyvault role command to do this wasn't functioning (it gave "unable to establish connection" errors), so I had to do it through the Azure console. Use the steps in https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references?toc=/azure/azure-functions/toc.json, bearing in mind that you have already created the key vault and a system-assigned identity, so you only need to create the access policy, step 3 under "Granting your app access to Key Vault".
8. Store the secrets in the key vault - the aws AccessKeyId and aws SecretAccessKey. I recommend creating a new aws user for this program and only giving it the required access - AmazonSNSFullAccess. You can either do this manually through the portal or using the provided 04-setsecrets.bat batch file. Since you should never store secrets in GitHub, and so also shouldn't include them in the project (unless you have a way of guaranteeing that you will never accidentally add them to GitHub), you should move the included batch file out of your project structure, modify that file and execute it. First, set the path to readcfg.bat and batch.cfg so that they can be found. Then change the vault-name to your value and change the awsAccessKeyId and awsSecretAccessKeyId to values for an aws user with access to SNS but hopefully almost nothing else. If setting the secrets through the portal, see the batch file for the secret names - note that "secret1" is used for testing.
9. Even though the AWS SNS topic that you configured will automatically be created if it doesn't already exist, no-one will know whether anything is published to it unless they subscribe to it. This can be done in various ways, but I've included snsSubscribe.sample.bat that you can modify to include your topic name, aws region and the email address that you want notifications sent to. Rename it to snsSubscribe.bat, put in your desired values and then run it. Again, I recommend against including the modified file in GitHub, due to the private information that it contains.
10. The last piece of configuration is to adjust the schedule that the function runs on. This is set by a cron expression, in UrlChecker.Run, in UrlChecker.cs. Note that the time is UTC and the function by defaults run at 10am and 4pm UTC. You can change it to anything that your cron skills can conjure - bearing in mind that if the function runs too frequently it can escape the free tier.
11. The project should be ready to run. If you have all the expected tools locally installed, you should be able to run the function locally. If you changed the name of the directory that this project is in from urlchecker to something else, modify runlocal.bat to match your directory name (just the last element, the rest of the path shouldn't need to be changed.) Start the project in your development environment and, if it doesn't automatically run (it usually will run automatically the first time it starts up on a given day, as it seems to recognize that it hasn't run yet today, but it should have - depending on when it is scheduled to run), use runLocal.bat to trigger it. It doesn't return any values, but you should be able to see the logging output in your development environment.
12. Once you can get it to run locally, and have published the function again if you had to make any changes, you can try running it on Azure. To trigger it you could modify the times that it is scheduled to run, or you can manually trigger it using runAzure.bat. Copy the provided runAzure.Sample.bat to runAzure.bat, change the function app name from UrlCheckerGAW to your value, and paste your function's _master app key (which can be found in the portal for the Function App under Functions\App keys\_master) in as well. Then, you should be able to trigger the function by running the batch file. If you ran it locally, the previous values file likely contains the current values, so you are unlikely to receive a notification. You could manually edit that file in the Azure portal to ensure that there is a discrepancy, or you could view the logs for the function to confirm that it did run. Again, I recommend against adding the modified runAzure.bat file to GitHub, as it now contains secret information that should not be published.

## Development Environment
I used VS Code under Windows 10 as my development environment, with the following extensions:
	.Net Core Test Explorer
	AWS Toolkit
	Azure Account
	Azure Functions
	Azure Resources
	C#
	NuGet Package Manager


Prerequisites
.NET Core 5.0
C# 8
Azure cli
AWS cli
Azure subscription
AWS account

Authors
Greg Walker - Initial work - (https://github.com/gregw18)
License
MIT