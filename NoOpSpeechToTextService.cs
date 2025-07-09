using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Aquatir // <--- УБЕДИТЕСЬ, ЧТО ЭТО ПРОСТРАНСТВО ИМЕН Aquatir
{
    public class NoOpSpeechToTextService : ISpeechToTextService
    {
        public Task<string> ListenAsync(CultureInfo culture, CancellationToken cancellationToken)
        {
            Console.WriteLine("Speech-to-text is not supported on this platform.");
            return Task.FromResult(string.Empty);
        }

        public Task<bool> RequestPermissions()
        {
            return Task.FromResult(true);
        }
    }
}