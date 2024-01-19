// (c) Carlos Pineda Guerrero. 2023

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class modifica_articulo
    {
        class Articulo
        {
            public string descripcion;
            public int cantidad;
            public float precio;
            public string foto;  // foto en base 64

        }
        class ParamModificaArticulo
        {
            public Articulo articulo;
        }
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("modifica_articulo")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamModificaArticulo data = JsonConvert.DeserializeObject<ParamModificaArticulo>(body);
                Articulo articulo = data.articulo;

                if (articulo.descripcion == null || articulo.descripcion == "") throw new Exception("Se debe ingresar una descripcion");
                if (articulo.cantidad <= 0) throw new Exception("Se debe ingresar una cantidad válida");
                if (articulo.precio <= 0) throw new Exception("Se debe ingresar un precio precio");
                
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
                    cmd_2.CommandText = "UPDATE articulos SET cantidad=@cantidad WHERE descripcion LIKE CONCAT('%', @descripcion, '%')";
                    cmd_2.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                    cmd_2.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                    cmd_2.ExecuteNonQuery();

                    var cmd_3 = new MySqlCommand();
                    cmd_3.Connection = conexion;
                    //cmd_3.Transaction = transaccion;
                    cmd_3.CommandText = "DELETE FROM fotos_articulos WHERE id_articulo=(SELECT id_articulo FROM articulos WHERE descripcion LIKE CONCAT('%', @descripcion, '%'))";
                    cmd_3.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                    cmd_3.ExecuteNonQuery();

                    if (articulo.foto != null)
                    {
                        var cmd_4 = new MySqlCommand();
                        cmd_4.Connection = conexion;
                        //cmd_4.Transaction = transaccion;
                        cmd_4.CommandText = "INSERT INTO fotos_articulos (foto,id_articulo) VALUES (@foto,(SELECT id_articulo FROM articulos WHERE descripcion LIKE CONCAT('%', @descripcion, '%')))";
                        cmd_4.Parameters.AddWithValue("@foto", Convert.FromBase64String(articulo.foto));
                        cmd_4.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                        cmd_4.ExecuteNonQuery();
                    }

                    //transaccion.Commit();
                    new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                    return req.CreateResponse(HttpStatusCode.OK, "articulo modificado");
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
                return req.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
        }
    }
}
