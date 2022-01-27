USE master
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'NotificationService')
BEGIN
  CREATE DATABASE [NotificationService];
END;
GO
