using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciasController : ControllerBase
    {
        private readonly string _connectionString;

        public AsistenciasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Asistencias/plantel/{categoriaId}
        [HttpGet("plantel/{categoriaId}")]
        public IActionResult GetPlantel(int categoriaId)
        {
            var plantel = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT u.id_usuario, u.nombre, u.apellido 
                    FROM usuarios u
                    INNER JOIN fichas_deportivas f ON u.id_usuario = f.fk_usuario_id
                    WHERE f.fk_categoria_id = @CategoriaId AND f.estado = 'Activo'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CategoriaId", categoriaId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            plantel.Add(new
                            {
                                IdUsuario = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Apellido = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return Ok(plantel);
        }

        // GET: api/Asistencias/jugador/{idUsuario} (Historial para el Rol Jugador)
        [HttpGet("jugador/{idUsuario}")]
        public IActionResult GetAsistenciasPorJugador(int idUsuario)
        {
            var historial = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT CONVERT(VARCHAR(10), a.fecha, 103) AS fecha_formateada, c.nombre AS categoria, a.estado
                    FROM asistencias a
                    INNER JOIN categorias c ON a.fk_categoria_id = c.id_categoria
                    WHERE a.fk_usuario_id = @UsuarioId
                    ORDER BY a.fecha DESC, a.id_asistencia DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", idUsuario);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            historial.Add(new
                            {
                                Fecha = reader.GetString(0),
                                Categoria = reader.GetString(1),
                                Estado = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return Ok(historial);
        }

        // POST: api/Asistencias/registrar
        [HttpPost("registrar")]
        public IActionResult RegistrarAsistencia([FromBody] RegistroAsistenciaRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string query = @"
                            INSERT INTO asistencias (fk_usuario_id, fk_categoria_id, fecha, estado, registrado_por) 
                            VALUES (@UsuarioId, @CategoriaId, @Fecha, @Estado, @RegistradoPor)";

                        foreach (var jugador in request.Jugadores)
                        {
                            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@UsuarioId", jugador.UsuarioId);
                                cmd.Parameters.AddWithValue("@CategoriaId", request.CategoriaId);
                                cmd.Parameters.AddWithValue("@Fecha", request.Fecha.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@Estado", jugador.Estado);
                                cmd.Parameters.AddWithValue("@RegistradoPor", request.ProfesorId);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Ok(new { mensaje = "Asistencia masiva guardada correctamente." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { mensaje = "Error al guardar asistencia", detalle = ex.Message });
                    }
                }
            }
        }
    }

    public class RegistroAsistenciaRequest
    {
        public int CategoriaId { get; set; }
        public int ProfesorId { get; set; }
        public DateTime Fecha { get; set; }
        public List<JugadorAsistencia> Jugadores { get; set; }
    }

    public class JugadorAsistencia
    {
        public int UsuarioId { get; set; }
        public string Estado { get; set; }
    }
}