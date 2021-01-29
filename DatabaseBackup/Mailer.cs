using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace DatabaseBackup
{
    public class Mailer
    {
        private string _smtpServer;
        private string _emailID;
        private string _emailPassword;
        private int _smtpServerPort;
        private bool _smtpServerEnableSsl;

        public Mailer() {
            _smtpServer = AppSettings.GetConfiguration<string>("emailSmtpServer");
            _emailID = AppSettings.GetConfiguration<string>("emailSmtpLoginID");
            _emailPassword = AppSettings.GetConfiguration<string>("emailSmtpLoginPassword");
            _smtpServerPort = AppSettings.GetConfiguration<int>("emailSmtpPort");
            _smtpServerEnableSsl = AppSettings.GetConfiguration<bool>("emailSmtpEnableSsl");
        }

        public void SendEmail(string from, string to, string subject, string body)
        {
            SmtpClient SmtpServer = new SmtpClient(_smtpServer);
            SmtpServer.Port = _smtpServerPort;
            SmtpServer.Credentials =
            new System.Net.NetworkCredential(_emailID, _emailPassword);
            SmtpServer.EnableSsl = _smtpServerEnableSsl;
            SmtpServer.Send(from, to, subject, body);
        }
    }
}
