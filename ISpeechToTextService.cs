using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Aquatir
{
    public interface ISpeechToTextService
    {
        Task<string> ListenAsync(CultureInfo culture, CancellationToken cancellationToken);
        Task<bool> RequestPermissions();
    }
}