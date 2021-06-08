aws sns subscribe ^
	--topic-arn arn:aws:sns:ca-central-1:awsAccount:VaccinationURLChangeWatcher ^
	--protocol email ^
	--notification-endpoint your_address@email.com