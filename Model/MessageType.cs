namespace Model
{
    public enum MessageType
    {
        FileContent=1,
        RequestFile,
        Close,
        RequestFileSystem,
        ResponseFileSystem,
        String,
        DownloadFinish,
        RequestSection,
        ResponseSection
    }
}
