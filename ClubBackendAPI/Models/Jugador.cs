namespace ClubBackendAPI.Models
{
    public class JugadorDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Telefono { get; set; }
        public int IdCategoria { get; set; }
        public string NombreCategoria { get; set; }
        public string Posicion { get; set; }
        public bool EsDestacado { get; set; }
        public string Estado { get; set; }
    }
}
