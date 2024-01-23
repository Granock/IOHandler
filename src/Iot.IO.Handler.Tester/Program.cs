
using System.Net.Sockets;

const string API_KEY_AUTH_REQUEST = "API-KEY-AUTH:REQUEST:";
const string API_KEY_AUTH_RESPONSE_FAILED = "API-KEY-AUTH:RESPONSE:FAILED";
const string API_KEY_AUTH_RESPONSE_SUCCESS = "API-KEY-AUTH:RESPONSE:SUCCESS";
const string OPCODE_MESSAGE_REQUEST = "OPCODE:REQUEST:{0}:{1}";
const string OPCODE_MESSAGE_RESPONSE = "OPCODE:RESPONSE:{0}:{1}";

const int PORT = 9999;
const string IPADDRESS = "192.168.1.122";
const string API_KEY = "0000";


using Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
await socket.ConnectAsync(IPADDRESS, PORT);
await using NetworkStream stream = new(socket);
using StreamReader reader = new(stream);
using StreamWriter writer = new(stream);

await writer.WriteLineAsync(string.Concat(API_KEY_AUTH_REQUEST, API_KEY));
await writer.FlushAsync();

string? line = await reader.ReadLineAsync();

if (!string.Equals(line, API_KEY_AUTH_RESPONSE_SUCCESS)) {
    Console.WriteLine(line);
    return;
}

while (true) {
    line = await reader.ReadLineAsync();
    
    if (string.IsNullOrWhiteSpace(line)) continue;
    
    string[] splits = line.Split(":");
    int opcode = int.Parse(splits[2]);
    DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(splits[3]));

    Console.WriteLine($"Received Opcode: {opcode}, Timestamp: {offset}");
    await writer.WriteLineAsync(string.Format(OPCODE_MESSAGE_RESPONSE, opcode, offset.ToUnixTimeMilliseconds()));
    await writer.FlushAsync();
}