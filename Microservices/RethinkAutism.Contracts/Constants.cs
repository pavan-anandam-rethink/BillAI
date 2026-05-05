namespace RethinkAutism.Contracts
{
    public class Constants
    {
        public const string UserNamePreventCharactersPattern = @"\s+";
        public const int UserNameMinLength = 6;
        public const string EmailPattern = @"^(([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})*)$";
        public const int MaxQueryParametersNumber = 2000;
        public const string SessionNoteFilePrefix = "Session_note_";
    }
}

