// (c) Carlos Pineda Guerrero. 2023

using System.IO;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class borra_articulo
    {
        class ParamBorraArticulo
        {
            public string descripcion;
        }
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("borra_articulo")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamBorraArticulo data = JsonConvert.DeserializeObject<ParamBorraArticulo>(body);
                string descripcion = data.descripcion;

                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion = new MySqlConnection(cs);
                conexion.Open();

                //MySqlTransaction transaccion = conexion.BeginTransaction();
                new MySqlCommand("begin work", conexion).ExecuteNonQuery();

                try
                {
                    var cmd_2 = new MySqlCommand();
                    cmd_2.Connection = conexion;
                    //cmd_2.Transaction = transaccion;
                    cmd_2.CommandText = "DELETE FROM fotos_articulos WHERE id_articulo=(SELECT id_articulo FROM articulos WHERE descripcion LIKE CONCAT('%', @descripcion, '%'))";
                    cmd_2.Parameters.AddWithValue("@descripcion", descripcion);
                    cmd_2.ExecuteNonQuery();

                    var cmd_3 = new MySqlCommand();
                    cmd_3.Connection = conexion;
                    //cmd_3.Transaction = transaccion;
                    cmd_3.CommandText = "DELETE FROM articulos WHERE descripcion LIKE CONCAT('%', @descripcion, '%')";
                    cmd_3.Parameters.AddWithValue("@descripcion", descripcion);
                    cmd_3.ExecuteNonQuery();

                    //transaccion.Commit();
                    new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                    return req.CreateResponse(HttpStatusCode.OK, "Articulo borrado");
                }
                catch (Exception e)
                {
                    //transaccion.Rollback();
                    new MySqlCommand("rollback work", conexion).ExecuteNonQuery();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return req.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
        }
    }
}
