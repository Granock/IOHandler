using Microsoft.Extensions.Hosting;

namespace Boßelwagen.Addons.Lib.Operation;

public interface IOpCodeExecutor : IHostedService {
    void ExecuteOpCodeMessage(OpCodeMessage message);
}