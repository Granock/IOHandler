using Boßelwagen.Addons.Boardunit.Lib;
using Boßelwagen.Addons.Configuration.Configuration;
using Boßelwagen.Addons.Configuration.Model;
using Boßelwagen.Addons.Core.Switcher;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Gpio.Monitor.Implementation;

public class BoardUnitMonitor : GpioMonitorBase {
    private readonly ILogger<BoardUnitMonitor> _logger;
    private readonly BoardunitGpio _boardunitGpio;

    public BoardUnitMonitor(ILogger<BoardUnitMonitor> logger, 
                            IConfigurationService configurationService,
                            ISwitcher switcher,
                            BoardunitGpio boardunitGpio) 
        : base(logger, configurationService, switcher) {
        
        _logger = logger;
        _boardunitGpio = boardunitGpio;
    }

    public override Task StartAsync(CancellationToken cancellationToken) {
        if (_boardunitGpio.Initialize()) {
            _logger.LogInformation("BoardUnitGpio intialized");
            _boardunitGpio.PortChanged += (x) => OnPinEvent(x.port, x.rising);
        } else {
            _logger.LogCritical("BoardUnitGpio intializing failed");
        }
        return base.StartAsync(cancellationToken);
    }

    protected override void InitHandlerForOperationCore(GpioConfiguration operation) { }
    protected override void CleanupHandlerForOperationCore(GpioConfiguration operation) { }

}