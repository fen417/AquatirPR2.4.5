// Aquatir/Platforms/Android/SpeechToTextService.Android.cs
using Android.Content;
using Android.Runtime;
using Android.Speech;
using Android.OS;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Debug = System.Diagnostics.Debug;

namespace Aquatir
{
    public class SpeechToTextService : Java.Lang.Object, ISpeechToTextService, IRecognitionListener
    {
        private SpeechRecognizer _speechRecognizer;
        private TaskCompletionSource<string> _recognitionCompletionSource;
        private CancellationTokenSource _cancellationTokenSource;

        public SpeechToTextService()
        {
            // Check if speech recognition is available
            if (SpeechRecognizer.IsRecognitionAvailable(Platform.CurrentActivity))
            {
                _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Platform.CurrentActivity);
                _speechRecognizer.SetRecognitionListener(this);
            }
            else
            {
                Debug.WriteLine("Speech recognition is not available on this device.");
                // Optionally throw an exception or set a flag to indicate unavailability
            }
        }

        public async Task<bool> RequestPermissions()
        {
            // Request RECORD_AUDIO permission if not already granted
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            return status == PermissionStatus.Granted;
        }

        public Task<string> ListenAsync(CultureInfo culture, CancellationToken cancellationToken)
        {
            if (_speechRecognizer == null)
            {
                _recognitionCompletionSource?.TrySetException(new InvalidOperationException("Speech recognition is not available."));
                return Task.FromResult(string.Empty);
            }

            // Cancel any previous recognition attempt
            _recognitionCompletionSource?.TrySetCanceled();
            _cancellationTokenSource?.Cancel();

            _recognitionCompletionSource = new TaskCompletionSource<string>();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationTokenSource.Token.Register(() => _recognitionCompletionSource.TrySetCanceled());

            var speechIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            speechIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            speechIntent.PutExtra(RecognizerIntent.ExtraLanguage, culture.Name);
            speechIntent.PutExtra(RecognizerIntent.ExtraPreferOffline, false); // Try online if offline not available
            speechIntent.PutExtra(RecognizerIntent.ExtraPartialResults, true); // Get partial results

            _speechRecognizer.StartListening(speechIntent);

            return _recognitionCompletionSource.Task;
        }

        // IRecognitionListener methods

        public void OnReadyForSpeech(Bundle @params) { Debug.WriteLine("Ready for speech"); }
        public void OnBeginningOfSpeech() { Debug.WriteLine("Beginning of speech"); }
        public void OnRmsChanged(float rmsdB) { /* Optional: Update UI with sound level */ }
        public void OnBufferReceived(byte[] buffer) { Debug.WriteLine("Buffer received"); }
        public void OnEndOfSpeech() { Debug.WriteLine("End of speech"); _speechRecognizer.StopListening(); } // Explicitly stop listening
        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            Debug.WriteLine($"Speech recognition error: {error}");
            _recognitionCompletionSource?.TrySetException(new Exception($"Speech recognition error: {error}"));
        }

        public void OnResults(Bundle results)
        {
            var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                _recognitionCompletionSource?.TrySetResult(matches[0]);
            }
            else
            {
                _recognitionCompletionSource?.TrySetResult(string.Empty);
            }
        }

        public void OnPartialResults(Bundle partialResults)
        {
            // Optional: Handle partial results if needed, e.g., for live display
            var matches = partialResults.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                Debug.WriteLine($"Partial: {matches[0]}");
                // If you want to use partial results, you might need to change ListenAsync to accept a Progress<string>
                // For now, it just logs them.
            }
        }

        public void OnEvent(int eventType, Bundle @params) { Debug.WriteLine($"Event: {eventType}"); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _speechRecognizer?.Destroy();
            }
            base.Dispose(disposing);
        }
    }
}