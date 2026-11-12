using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoticiasController : ControllerBase
    {
        private readonly string _connectionString;

        public NoticiasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Noticias
        [HttpGet]
        public IActionResult GetNoticias()
        {
            var noticias = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "SELECT id_noticia, titulo, cuerpo, imagenUrl, fechaPublicacion FROM noticias ORDER BY id_noticia DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        noticias.Add(new
                        {
                            IdNoticia = reader.GetInt32(0),
                            Titulo = reader.GetString(1),
                            Cuerpo = reader.GetString(2),
                            ImagenUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                            FechaPublicacion = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        });
                    }
                }
            }
            return Ok(noticias);
        }

        // POST: api/Noticias
        [HttpPost]
        public IActionResult CrearNoticia([FromBody] NuevaNoticiaRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    INSERT INTO noticias (titulo, cuerpo, imagenUrl, fechaPublicacion)
                    VALUES (@Titulo, @Cuerpo, @ImagenUrl, CONVERT(VARCHAR(10), GETDATE(), 103))";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Titulo", request.Titulo);
                    cmd.Parameters.AddWithValue("@Cuerpo", request.Cuerpo);
                    cmd.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(request.ImagenUrl) ? (object)DBNull.Value : request.ImagenUrl);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "Noticia publicada con éxito." });
        }

        // DELETE: api/Noticias/{id}
        [HttpDelete("{id}")]
        public IActionResult EliminarNoticia(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "DELETE FROM noticias WHERE id_noticia = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "Noticia eliminada." });
        }
    }

    public class NuevaNoticiaRequest
    {
        public string Titulo { get; set; }
        public string Cuerpo { get; set; }
        public string? ImagenUrl { get; set; }
    }
}