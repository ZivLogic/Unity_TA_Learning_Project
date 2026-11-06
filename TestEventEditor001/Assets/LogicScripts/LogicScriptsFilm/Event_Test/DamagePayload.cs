using EventSystemV3;
using System;

[Serializable]
public class DamagePayload : EntityEventPayload
{
    public string SourceUuid;
    public int Damage;
    public string DamageType;
}