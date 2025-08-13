namespace Common;

[Flags]
public enum PgsqlGrant
{
    ALL = -1,
    SELECT = 0b0001,
    INSERT = 0b0010,
    UPDATE = 0b0100,
    DELETE = 0b1000,
}