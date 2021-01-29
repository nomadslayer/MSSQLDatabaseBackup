# DatabaseBackup
DatabaseBackup is a simple console program that built for the purpose to perform database backup periodically.

The tool can be configured to 

 - Perform backup on specific database to specifc local folder
 - Upload to specific aws S3 bucket and folder
 - Clean up old backup files on S3 bucket after specific expiry days
 - Send email notification to specific email with the status after the execution is completed

# Get Started

 1. Open the DatabaseBackup.sln file with Visual Studio
 2. Configure the appsettings.json (as explained below)
 3. Build the project
 4. Publish the project
 5. Run the program with dotnet cli eg: `dotnet "D:\Projects\DatabaseBackup\publish\DatabaseBackup.dll"`

## Pre-requisites

 - dotnet core SDK
 - Visual Studio (not mandatory, dotnet cli can still do the job)

## Configurations (appsettings.json)

Database Credentials

- databaseServer
- databaseName
- databaseUserID
- databasePassword

S3 Credentials

 - awsS3AccessKeyID
 - awsS3AccessKeySecret
 - awsS3Region
 - awsS3BucketName
 - awsS3BucketFolder (Bucket Folder that stores all the backup files)

Email Credentials

- emailSmtpServer
- emailSmtpPort
- emailSmtpLoginID
- emailSmtpLoginPassword
- emailSmtpEnableSsl

Email Notification

- emailNotificationSendFrom
- emailNotificationSendTo

Backup Configuration

- backupLocation
- backupFileCleanupAfterDays


## Deploy

 1. Create a windows task scheduler
 2. Executes the dotnet core cli "C:\Program Files\dotnet\dotnet.exe"
 3. Add the path to the DatabaseBackup.dll as argument, for example: "D:\Scheduled Task\DatabaseBackupTool\DatabaseBackup.dll"
 4. Set schedule for the task to be carried out automatically.

