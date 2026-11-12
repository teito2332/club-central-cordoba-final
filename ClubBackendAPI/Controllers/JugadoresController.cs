using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ClubBackendAPI.Models;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JugadoresController : ControllerBase
    {
        private readonly string _connectionString;

        public JugadoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Jugadores
        [HttpGet]
        public IActionResult GetJugadores()
        {
            var jugadores = new List<JugadorDTO>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT 
                        u.id_usuario, u.nombre, u.apellido, u.dni, u.telefono,
                        f.fk_categoria_id, c.nombre as nombre_categoria, 
                        f.posicion, f.es_destacado, f.estado
                    FROM usuarios u
                    INNER JOIN fichas_deportivas f ON u.id_usuario = f.fk_usuario_id
                    INNER JOIN categorias c ON f.fk_categoria_id = c.id_categoria
                    WHERE u.fk_rol_id = 3 AND f.estado = 'Activo'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jugadores.Add(new JugadorDTO
                            {
                                IdUsuario = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Apellido = reader.GetString(2),
                                Dni = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Telefono = reader.IsDBNull(4) ? null : reader.GetString(4),
                                IdCategoria = reader.GetInt32(5),
                                NombreCategoria = reader.GetString(6),
                                Posicion = reader.IsDBNull(7) ? null : reader.GetString(7),
                                EsDestacado = reader.GetBoolean(8),
                                Estado = reader.GetString(9)
                            });
                        }
                    }
                }
            }

            return Ok(jugadores);
        }

        // POST: api/Jugadores
        [HttpPost]
        public IActionResult CrearJugador([FromBody] JugadorDTO nuevoJugador)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string queryUsuario = @"
                            INSERT INTO usuarios (nombre, apellido, dni, telefono, fk_rol_id) 
                            OUTPUT INSERTED.id_usuario
                            VALUES (@Nombre, @Apellido, @Dni, @Telefono, 3)";

                        int nuevoUsuarioId;
                        using (SqlCommand cmdU = new SqlCommand(queryUsuario, con, transaction))
                        {
                            cmdU.Parameters.AddWithValue("@Nombre", nuevoJugador.Nombre);
                            cmdU.Parameters.AddWithValue("@Apellido", nuevoJugador.Apellido);
                            cmdU.Parameters.AddWithValue("@Dni", (object)nuevoJugador.Dni ?? DBNull.Value);
                            cmdU.Parameters.AddWithValue("@Telefono", (object)nuevoJugador.Telefono ?? DBNull.Value);
                            nuevoUsuarioId = (int)cmdU.ExecuteScalar();
                        }

                        string queryFicha = @"
                            INSERT INTO fichas_deportivas (fk_usuario_id, fk_categoria_id, posicion, es_destacado, estado)
                            VALUES (@UsuarioId, @CategoriaId, @Posicion, @EsDestacado, 'Activo')";

                        using (SqlCommand cmdF = new SqlCommand(queryFicha, con, transaction))
                        {
                            cmdF.Parameters.AddWithValue("@UsuarioId", nuevoUsuarioId);
                            cmdF.Parameters.AddWithValue("@CategoriaId", nuevoJugador.IdCategoria);
                            cmdF.Parameters.AddWithValue("@Posicion", (object)nuevoJugador.Posicion ?? DBNull.Value);
                            cmdF.Parameters.AddWithValue("@EsDestacado", nuevoJugador.EsDestacado ? 1 : 0);
                            cmdF.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Ok(new { mensaje = "Jugador creado con éxito" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { mensaje = "Error al crear", detalle = ex.Message });
                    }
                }
            }
        }

        // DELETE: api/Jugadores/{idUsuario} (Baja lógica del plantel activo)
        [HttpDelete("{idUsuario}")]
        public IActionResult DarBajaJugador(int idUsuario)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "UPDATE fichas_deportivas SET estado = 'Baja' WHERE fk_usuario_id = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "Jugador dado de baja del plantel." });
        }
    }
}
