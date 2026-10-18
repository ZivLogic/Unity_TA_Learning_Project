using System;
using System.Collections.Generic;

[Serializable]
public class InputConfigArchive
{
    public string ArchiveId;
    public string CreateTime;
    public bool IsDefault;
    public InputConfigRoot Root;

    public DateTime GetCreateTime()
    {
        if (DateTime.TryParse(CreateTime, out var t))return t;
        return DateTime.MinValue;
    }

    public void SetCreateTime(DateTime t)
    {
        CreateTime = t.ToString("O");
    }
}

[Serializable]
public class InputArchiveIndex
{
    public List<InputArchiveIndexItem> Items = new List<InputArchiveIndexItem>();
}

[Serializable]
public class InputArchiveIndexItem
{
    public string ArchiveId;
    public string CreateTime;
    public string FileName;
}