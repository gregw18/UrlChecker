aws sns subscribe ^
	--topic-arn arn:aws:sns:ca-central-1:yourawsAccount:VaccinationURLChangeWatcher ^
	--protocol email ^
	--notification-endpoint your_address@email.com