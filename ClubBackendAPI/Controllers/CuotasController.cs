using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuotasController : ControllerBase
    {
        private readonly string _connectionString;

        public CuotasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Cuotas/morosos (Para el Administrador)
        [HttpGet("morosos")]
        public IActionResult GetMorosos()
        {
            var morosos = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT c.id_cuota, u.id_usuario, (u.nombre + ' ' + u.apellido) AS jugador, u.dni, c.periodo, c.monto, c.estado
                    FROM cuotas c
                    INNER JOIN usuarios u ON c.fk_usuario_id = u.id_usuario
                    WHERE c.estado = 'Pendiente'
                    ORDER BY c.id_cuota DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        morosos.Add(new
                        {
                            IdCuota = reader.GetInt32(0),
                            IdUsuario = reader.GetInt32(1),
                            Jugador = reader.GetString(2),
                            Dni = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                            Periodo = reader.GetString(4),
                            Monto = reader.GetDecimal(5),
                            Estado = reader.GetString(6)
                        });
                    }
                }
            }
            return Ok(morosos);
        }

        // GET: api/Cuotas/jugador/{idUsuario} (Para el Rol Jugador)
        [HttpGet("jugador/{idUsuario}")]
        public IActionResult GetCuotasPorJugador(int idUsuario)
        {
            var misCuotas = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT id_cuota, periodo, monto, estado
                    FROM cuotas
                    WHERE fk_usuario_id = @UsuarioId
                    ORDER BY id_cuota DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", idUsuario);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            misCuotas.Add(new
                            {
                                IdCuota = reader.GetInt32(0),
                                Periodo = reader.GetString(1),
                                Monto = reader.GetDecimal(2),
                                Estado = reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return Ok(misCuotas);
        }

        // POST: api/Cuotas/asignar
        [HttpPost("asignar")]
        public IActionResult AsignarCuota([FromBody] NuevaCuotaRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    INSERT INTO cuotas (fk_usuario_id, periodo, monto, estado)
                    VALUES (@UsuarioId, @Periodo, @Monto, 'Pendiente')";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", request.UsuarioId);
                    cmd.Parameters.AddWithValue("@Periodo", request.Periodo);
                    cmd.Parameters.AddWithValue("@Monto", request.Monto);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "Cuota asignada correctamente." });
        }

        // PUT: api/Cuotas/pagar/{idCuota}
        [HttpPut("pagar/{idCuota}")]
        public IActionResult PagarCuota(int idCuota)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "UPDATE cuotas SET estado = 'Pagada' WHERE id_cuota = @IdCuota";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdCuota", idCuota);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "Pago registrado con éxito." });
        }
    }

    public class NuevaCuotaRequest
    {
        public int UsuarioId { get; set; }
        public string Periodo { get; set; }
        public decimal Monto { get; set; }
    }
}
