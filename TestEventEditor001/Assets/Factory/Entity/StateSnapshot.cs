// StateSnapshot.cs (或者直接写在基类里)
using System;

[Serializable]
public class EntityStateSnapshot
{
    public string UUID;
    public string Lifecycle;
    public string Action;
    public string[] Tags;
}