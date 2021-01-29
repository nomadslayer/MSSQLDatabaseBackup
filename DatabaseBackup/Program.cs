using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace DatabaseBackup
{
    class Program
    {
        static void Main(string[] args)
        {
            AppSettings.InitializeAppSettings();
            Mailer mailer = new Mailer();
            StringBuilder emailTextBuilder = new StringBuilder();
            Logger logger = new Logger();
            FileZipper fileZipper = new FileZipper();
            S3Worker s3Worker = new S3Worker();

            bool backupSuccess;

            string emailSubjectStartTime = DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss");
            string to = AppSettings.GetConfiguration<string>("emailNotificationSendTo");
            string from = AppSettings.GetConfiguration<string>("emailNotificationSendFrom");
            string successSubject = string.Format("Nightly database backup - {0} [Completed]", emailSubjectStartTime);
            string errorSubject = string.Format("Nightly database backup - {0} [Error]", emailSubjectStartTime);

            string folderName = AppSettings.GetConfiguration<string>("backupLocation");
            string fileName = string.Format("backup_{0}.bak", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            string databaseServer = AppSettings.GetConfiguration<string>("databaseServer");
            string databaseName = AppSettings.GetConfiguration<string>("databaseName");
            string databaseUserID = AppSettings.GetConfiguration<string>("databaseUserID");
            string databasePassword = AppSettings.GetConfiguration<string>("databasePassword");

            string fullFileName = folderName + "\\" + fileName;
            DateTime startTime = DateTime.Now;
            DateTime completionTime;
            

            using (SqlConnection con = new SqlConnection(
                string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3}", 
                databaseServer, databaseName, databaseUserID, databasePassword)))
            {
                con.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(string.Format("BACKUP DATABASE [{0}] TO  DISK = N'{1}' WITH FORMAT, INIT,  NAME = N'DB-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10", databaseName, fullFileName), con))
                    {
                        startTime = DateTime.Now;

                        command.CommandTimeout = 0;
                        command.ExecuteNonQuery();

                        backupSuccess = true;
                        completionTime = DateTime.Now;
                    }

                    Console.WriteLine("Database backup successfully.");
                    Console.WriteLine(string.Format("Backup File name: {0}", fullFileName));
                    Console.WriteLine(string.Format("Time Started DB Backup: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine(string.Format("Time Completed DB Backup: {0}", completionTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine("");

                    emailTextBuilder.AppendLine("Database backup successfully.");
                    emailTextBuilder.AppendLine(string.Format("Backup File name: {0}", fullFileName));
                    emailTextBuilder.AppendLine(string.Format("Time Started DB Backup: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine(string.Format("Time Completed DB Backup: {0}", completionTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine("");
                }
                catch (Exception e)
                {
                    backupSuccess = false;

                    Console.WriteLine("Database backup with error.");
                    Console.WriteLine(string.Format("Time Started DB Backup: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine(string.Format("Error : {0}", e.ToString()));
                    Console.WriteLine("");

                    emailTextBuilder.AppendLine("Database backup with error.");
                    emailTextBuilder.AppendLine(string.Format("Time Started DB Backup: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine(string.Format("Error : {0}", e.ToString()));
                    emailTextBuilder.AppendLine("");
                }
            }

            if (backupSuccess)
            {
                startTime = DateTime.Now;

                try
                {
                    fileZipper.Zip(fullFileName);
                    s3Worker.Upload(string.Format("{0}.gz", fullFileName), string.Format("{0}.gz", fileName));
                    completionTime = DateTime.Now;
                    backupSuccess = true;

                    Console.WriteLine("S3 Uploaded successfully.");
                    Console.WriteLine(string.Format("Full file name: {0}", fullFileName));
                    Console.WriteLine(string.Format("Time Started: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine(string.Format("Time Completed: {0}", completionTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine("");

                    emailTextBuilder.AppendLine("S3 Uploaded successfully.");
                    emailTextBuilder.AppendLine(string.Format("Full file name: {0}", fullFileName));
                    emailTextBuilder.AppendLine(string.Format("Time Started: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine(string.Format("Time Completed: {0}", completionTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine("");
                }
                catch (Exception e)
                {
                    backupSuccess = false;

                    Console.WriteLine("S3 Upload Error.");
                    Console.WriteLine(string.Format("Full file name: {0}", fullFileName));
                    Console.WriteLine(string.Format("Time Started: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    Console.WriteLine(string.Format("Error: {0}", e.ToString()));

                    emailTextBuilder.AppendLine("S3 Upload Error.");
                    emailTextBuilder.AppendLine(string.Format("Full file name: {0}", fullFileName));
                    emailTextBuilder.AppendLine(string.Format("Time Started: {0}", startTime.ToString("yyyy-MMM-dd HH:mm:ss")));
                    emailTextBuilder.AppendLine(string.Format("Error: {0}", e.ToString()));

                    if (s3Worker.uploadStatusBuilder.Length > 0)
                    {
                        emailTextBuilder.AppendLine(s3Worker.uploadStatusBuilder.ToString());
                    }
                    emailTextBuilder.AppendLine("");
                }
            }

            //Clean up old files on S3
            if (backupSuccess)
            {
                try
                {
                    s3Worker.CleanUp();

                    Console.WriteLine("S3 Clean Up Completed Successfully.");

                    emailTextBuilder.AppendLine("S3 Clean Up Completed Successfully.");
                }
                catch (Exception e)
                {
                    backupSuccess = false;

                    Console.WriteLine("S3 Clean Up Error.");
                    Console.WriteLine(string.Format("Error: {0}", e.ToString()));

                    emailTextBuilder.AppendLine("S3 Clean Up Error.");
                    emailTextBuilder.AppendLine(string.Format("Error: {0}", e.ToString()));

                    if (s3Worker.listObjectStatusBuilder.Length > 0)
                    {
                        Console.WriteLine(s3Worker.listObjectStatusBuilder.ToString());

                        emailTextBuilder.AppendLine(s3Worker.listObjectStatusBuilder.ToString());
                    }

                    if (s3Worker.cleanUpStatusBuilder.Length > 0)
                    {
                        Console.WriteLine(s3Worker.cleanUpStatusBuilder.ToString());

                        emailTextBuilder.AppendLine(s3Worker.cleanUpStatusBuilder.ToString());
                    }
                    Console.WriteLine("");

                    emailTextBuilder.AppendLine("");
                }
            }

            if (backupSuccess)
            {
                //perform the old files clean up
                string[] files = Directory.GetFiles(folderName, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    FileInfo fileInformation = new FileInfo(file);
                    fileInformation.Delete();
                }

                Console.WriteLine("Local folder clean up completed.");
                emailTextBuilder.AppendLine("Local folder clean up completed.");

                logger.LogToFile(emailTextBuilder.ToString());
                mailer.SendEmail(from, to, successSubject, emailTextBuilder.ToString());
                Console.WriteLine("Email sent.");
            }
            else
            {
                logger.LogToFile(emailTextBuilder.ToString());
                mailer.SendEmail(from, to, errorSubject, emailTextBuilder.ToString());
                Console.WriteLine("Email sent.");
            }
        }
    }
}
