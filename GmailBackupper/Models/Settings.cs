﻿namespace GmailBackupper.Models
{
    class GlobalSettings
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string SettingsPath { get; set; }
    }

    class AccountSettings
    {
        public string RefreshToken { get; set; }

        public string BackupPath { get; set; }

        public int Limit { get; set; }

        public DeleteRule[] DeleteRules { get; set; }
    }

    class DeleteRule
    {
        public string Id { get; set; }

        public int Limit { get; set; }
    }
}
