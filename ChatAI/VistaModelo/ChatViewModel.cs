using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatAI.Commands;
using ChatAI.Modelo;
using Vosk;
using System.Media;
using NAudio.Wave;
using System.Threading;
using System.Speech.Synthesis;

namespace ChatAI.VistaModelo
{
    public class ChatViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        private readonly TextToSpeechService _textToSpeechService = new();
        private readonly VoskRecognizer _voskRecognizer;
        private readonly Model _voskModel;
        private bool _grabando;
        private string _texto;
        private bool _puedeEnviar;
        private string _gifSource;
        private bool _isSpeaking;
        private Mensaje _currentMessage;

        public string GifSource
        {
            get => _gifSource;
            set
            {
                _gifSource = value;
                OnPropertyChanged();
            }
        }

        private bool _iaPensando;
        public bool IaPensando
        {
            get => _iaPensando;
            set
            {
                if (_iaPensando != value)
                {
                    _iaPensando = value;
                    OnPropertyChanged();
                    GifSource = _iaPensando
                        ? "pack://application:,,,/Resource/Images/krzystof-knifes.gif"
                        : "pack://application:,,,/Resource/Images/katana-sword.gif";
                }
            }
        }

        public ICommand NuevaConversacionCommand { get; }

        public ObservableCollection<Mensaje> Mensajes { get; } = new();

        private List<object> messages = new()
        {
            new { content = Settings1.Default.mensajeIA, role = "system" }
        };

        public string Texto
        {
            get => _texto;
            set
            {
                if (_texto != value)
                {
                    _texto = value;
                    OnPropertyChanged();
                    PuedeEnviar = !string.IsNullOrWhiteSpace(_texto);
                    ((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool PuedeEnviar
        {
            get => _puedeEnviar;
            set
            {
                if (_puedeEnviar != value)
                {
                    _puedeEnviar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Grabando
        {
            get => _grabando;
            private set
            {
                if (_grabando != value)
                {
                    _grabando = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand EnviarMensajeCommand { get; }
        public ICommand LeerMensajeCommand { get; }
        public ICommand MicrofonoCommand { get; }

        private void LeerMensaje(object parametro)
        {
            if (parametro is Mensaje mensaje && !string.IsNullOrWhiteSpace(mensaje.Contenido))
            {
                if (_textToSpeechService.GetState() == SynthesizerState.Speaking && _currentMessage == mensaje)
                {
                    // Pausar si se hace clic en el mismo mensaje
                    _textToSpeechService.Pause();
                }
                else if (_textToSpeechService.GetState() == SynthesizerState.Paused && _currentMessage == mensaje)
                {
                    // Reanudar si se hace clic en el mismo mensaje pausado
                    _textToSpeechService.Resume();
                }
                else
                {
                    // Detener la reproducción actual y comenzar la nueva
                    _textToSpeechService.Stop();
                    _textToSpeechService.Speak(mensaje.Contenido);
                    _currentMessage = mensaje;
                }
            }
        }

        public ChatViewModel()
        {
            EnviarMensajeCommand = new RelayCommand(async () => await EnviarMensaje(), () => PuedeEnviar);
            LeerMensajeCommand = new RelayCommand<object>(LeerMensaje);
            MicrofonoCommand = new RelayCommand(ToggleMicrofono);
            NuevaConversacionCommand = new RelayCommand(NuevaConversacion);

            GifSource = "pack://application:,,,/Resource/Images/katana-sword.gif";

            // Configurar el reconocimiento de voz con Vosk
            string modelPath = Path.Combine(Environment.CurrentDirectory, "Resource", "vosk-model-small-es-0.42");
            _voskModel = new Model(modelPath);
            _voskRecognizer = new VoskRecognizer(_voskModel, 16000.0f);
        }

        private void NuevaConversacion()
        {
            if (Mensajes.Count > 0)
            {
                var result = MessageBox.Show("¿Estás seguro de que deseas borrar el chat actual?", "Nueva Conversación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Mensajes.Clear();
                    messages.Clear();
                }
            }
        }

        private void ToggleMicrofono()
        {
            if (Grabando)
            {
                // Detener la grabación
                Grabando = false;
            }
            else
            {
                // Iniciar la grabación
                Grabando = true;
                Task.Run(() => RecognizeSpeech());
            }
        }

        private void RecognizeSpeech()
        {
            try
            {
                var waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1) // 16 kHz, mono
                };

                waveIn.DataAvailable += (sender, e) =>
                {
                    _voskRecognizer.AcceptWaveform(e.Buffer, e.BytesRecorded);
                };

                waveIn.RecordingStopped += (sender, e) =>
                {
                    var result = _voskRecognizer.FinalResult();

                    // Parsear el JSON para extraer solo el texto
                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
                        {
                            string recognizedText = textElement.GetString();
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Texto += recognizedText + " ";
                            });
                        }
                    }
                };

                waveIn.StartRecording();

                while (Grabando)
                {
                    Thread.Sleep(100);
                }

                waveIn.StopRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en el reconocimiento de voz: " + ex.Message);
            }
            finally
            {
                Grabando = false;
            }
        }

        private async Task EnviarMensaje()
        {
            var mensajeUsuario = new Mensaje { Contenido = Texto, EsUsuario = true };
            Mensajes.Add(mensajeUsuario);
            Texto = string.Empty;
            PuedeEnviar = false;
            ((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();

            messages.Add(new { content = mensajeUsuario.Contenido, role = "user" });

            var requestBody = new
            {
                messages = messages.ToArray(),
                model = Settings1.Default.IA,
                max_tokens = 2048
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                IaPensando = true;

                var response = await _httpClient.PostAsync(Settings1.Default.urlLocalhost, content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<ChatResponse>(responseBody);

                var mensajeBot = new Mensaje { Contenido = resultado.choices[0].message.content, EsUsuario = false };
                Mensajes.Add(mensajeBot);
                messages.Add(new { content = mensajeBot.Contenido, role = "assistant" });
            }
            catch (Exception ex)
            {
                Mensajes.Add(new Mensaje { Contenido = "Error en la respuesta: " + ex.Message, EsUsuario = false });
            }
            finally
            {
                IaPensando = false;
            }
        }
    }
}