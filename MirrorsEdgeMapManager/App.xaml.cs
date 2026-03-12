using System.Text;
using System.Windows;

namespace MirrorsEdgeMapManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        base.OnStartup(e);
    }
}

