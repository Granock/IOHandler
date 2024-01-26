namespace Boßelwagen.Addons.Lib.Operation;

public record OpCodeMessage(int OpCode, DateTimeOffset Timestamp) {
    public override string ToString() {
        return $"OPCODE-MESSAGE:{OpCode}:{Timestamp.ToUnixTimeMilliseconds()}";
    }

    public static OpCodeMessage FromString(string @string) {
        string[] splits = @string.Split(separator: ":");
        if (splits[1] != "OPCODE-MESSAGE") {
            throw new ArgumentException("String-Start invalide", nameof(@string));
        }
        int opCode = int.Parse(splits[1]);
        long unixTime = long.Parse(splits[2]);
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTime);
        return new OpCodeMessage(opCode, timestamp);
    }

    public static OpCodeMessage FromOpCode(int opcode) => new(opcode, DateTimeOffset.UtcNow);
}