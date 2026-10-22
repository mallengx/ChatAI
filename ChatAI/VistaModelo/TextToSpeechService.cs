using System;
using System.Speech.Synthesis;

namespace ChatAI.VistaModelo
{
    public class TextToSpeechService
    {
        private readonly SpeechSynthesizer _synthesizer;

        public TextToSpeechService()
        {
            _synthesizer = new SpeechSynthesizer();

            // Verificar si hay voces disponibles
            if (_synthesizer.GetInstalledVoices().Count == 0)
            {
                throw new InvalidOperationException("No hay voces instaladas en el sistema.");
            }

            // Seleccionar la primera voz disponible
            _synthesizer.SelectVoice(_synthesizer.GetInstalledVoices()[0].VoiceInfo.Name);
        }

        /// <summary>
        /// Reproduce el texto proporcionado.
        /// </summary>
        /// <param name="text">El texto a reproducir.</param>
        public void Speak(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    Console.WriteLine("Texto a leer: " + text); // Para depuración
                    _synthesizer.SpeakAsync(text); // Reproduce el texto de forma asíncrona
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en síntesis de voz: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Pausa la reproducción actual.
        /// </summary>
        public void Pause()
        {
            if (_synthesizer.State == SynthesizerState.Speaking)
            {
                _synthesizer.Pause(); // Pausa la reproducción
                Console.WriteLine("Reproducción pausada.");
            }
        }

        /// <summary>
        /// Reanuda la reproducción si está pausada.
        /// </summary>
        public void Resume()
        {
            if (_synthesizer.State == SynthesizerState.Paused)
            {
                _synthesizer.Resume(); // Reanuda la reproducción
                Console.WriteLine("Reproducción reanudada.");
            }
        }

        /// <summary>
        /// Detiene la reproducción actual.
        /// </summary>
        public void Stop()
        {
            if (_synthesizer.State == SynthesizerState.Speaking || _synthesizer.State == SynthesizerState.Paused)
            {
                _synthesizer.SpeakAsyncCancelAll(); // Detiene la reproducción
                Console.WriteLine("Reproducción detenida.");
            }
        }

        /// <summary>
        /// Obtiene el estado actual de la síntesis de voz.
        /// </summary>
        /// <returns>El estado actual (Speaking, Paused, Ready).</returns>
        public SynthesizerState GetState()
        {
            return _synthesizer.State;
        }
    }
}