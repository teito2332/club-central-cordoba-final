using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT u.id_usuario, u.nombre, u.apellido, u.fk_rol_id, r.nombre as nombre_rol 
                    FROM usuarios u
                    INNER JOIN roles r ON u.fk_rol_id = r.id_rol
                    WHERE u.dni = @Dni";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Dni", request.Dni);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                IdUsuario = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Apellido = reader.GetString(2),
                                RolId = reader.GetInt32(3),
                                RolNombre = reader.GetString(4)
                            });
                        }
                        else
                        {
                            return Unauthorized(new { mensaje = "DNI no encontrado. Verifique sus datos." });
                        }
                    }
                }
            }
        }

        // GET: api/Auth/personal (Lista de Admins y Profes)
        [HttpGet("personal")]
        public IActionResult GetPersonal()
        {
            var personal = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT u.id_usuario, u.nombre, u.apellido, u.dni, u.telefono, u.fk_rol_id, r.nombre as nombre_rol
                    FROM usuarios u
                    INNER JOIN roles r ON u.fk_rol_id = r.id_rol
                    WHERE u.fk_rol_id IN (1, 2)
                    ORDER BY u.fk_rol_id, u.apellido";

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        personal.Add(new
                        {
                            IdUsuario = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Apellido = reader.GetString(2),
                            Dni = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Telefono = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                            RolId = reader.GetInt32(5),
                            RolNombre = reader.GetString(6)
                        });
                    }
                }
            }
            return Ok(personal);
        }

        // POST: api/Auth/registrar (Dar de alta nuevo Profe o Admin)
        [HttpPost("registrar")]
        public IActionResult RegistrarPersonal([FromBody] NuevoPersonalRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                try
                {
                    string query = @"
                        INSERT INTO usuarios (nombre, apellido, dni, telefono, fk_rol_id)
                        VALUES (@Nombre, @Apellido, @Dni, @Telefono, @RolId)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", request.Nombre);
                        cmd.Parameters.AddWithValue("@Apellido", request.Apellido);
                        cmd.Parameters.AddWithValue("@Dni", request.Dni);
                        cmd.Parameters.AddWithValue("@Telefono", (object)request.Telefono ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RolId", request.RolId);

                        cmd.ExecuteNonQuery();
                    }
                    return Ok(new { mensaje = "Usuario registrado con éxito" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { mensaje = "Error al registrar usuario (verifique que el DNI no esté repetido)", detalle = ex.Message });
                }
            }
        }

        // DELETE: api/Auth/personal/{id} (Dar de baja a un Profe o Admin)
        [HttpDelete("personal/{id}")]
        public IActionResult EliminarPersonal(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // Si el profe había tomado asistencias, las reasignamos al Admin (ID 1) para cuidar el historial
                        string queryAsistencias = "UPDATE asistencias SET registrado_por = 1 WHERE registrado_por = @Id";
                        using (SqlCommand cmdA = new SqlCommand(queryAsistencias, con, transaction))
                        {
                            cmdA.Parameters.AddWithValue("@Id", id);
                            cmdA.ExecuteNonQuery();
                        }

                        // Borramos al profe o admin de la tabla usuarios
                        string queryUsuario = "DELETE FROM usuarios WHERE id_usuario = @Id AND fk_rol_id IN (1, 2)";
                        using (SqlCommand cmdU = new SqlCommand(queryUsuario, con, transaction))
                        {
                            cmdU.Parameters.AddWithValue("@Id", id);
                            cmdU.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return Ok(new { mensaje = "Personal dado de baja correctamente." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { mensaje = "Error al dar de baja", detalle = ex.Message });
                    }
                }
            }
        }
    }

    public class LoginRequest
    {
        public string Dni { get; set; }
    }

    public class NuevoPersonalRequest
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string? Telefono { get; set; }
        public int RolId { get; set; } // 1 = Administrador, 2 = Entrenador
    }
}
