// See https://aka.ms/new-console-template for more information

using SMTPNET;

SMTPServer SS = new(IPEndPoint.Parse("mail.domain.tld:25"));

SS.StartAcceptMail();



