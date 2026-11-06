using EventSystemV3;
using System;

[Serializable]
public class AttackPayload : EntityEventPayload
{
    public string SourceUuid;
    public int Damage;
    public float Range;
}