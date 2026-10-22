using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAI.Modelo
{
    public class Conversacion
    {
        public string Nombre { get; set; } // Nombre de la conversación (opcional)
        public List<Mensaje> Mensajes { get; set; } = new(); // Lista de mensajes
    }
}
