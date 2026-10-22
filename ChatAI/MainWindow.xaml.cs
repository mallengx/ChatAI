using ChatAI.Modelo;
using ChatAI.VistaModelo;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Specialized;

namespace ChatAI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Asignar el ViewModel como DataContext
            var viewModel = new ChatViewModel();
            DataContext = viewModel;

            // Suscribirse al evento CollectionChanged de la colección Mensajes
            viewModel.Mensajes.CollectionChanged += Mensajes_CollectionChanged;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                // Evita que se inserte un salto de línea
                e.Handled = true;

                // Enviar el mensaje si se puede
                if (DataContext is ChatViewModel vm && vm.PuedeEnviar)
                {
                    vm.EnviarMensajeCommand.Execute(null);

                    // Desplazar el scroll al final después de enviar el mensaje
                    MessageScrollViewer.ScrollToEnd();
                }
            }
        }

        private void Mensajes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Desplazar el scroll al final cuando se añade un nuevo mensaje
            MessageScrollViewer.ScrollToEnd();
        }
    }
}