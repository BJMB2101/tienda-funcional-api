// (c) Carlos Pineda Guerrero. 2023

using System;
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
    public static class alta_articulo
    {
        class Articulo
        {
            public string descripcion;
            public int cantidad;
            public float precio;
            public string foto;  // foto en base 64
        }
        class ParamAltaArticulo
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
        [FunctionName("alta_articulo")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamAltaArticulo data = JsonConvert.DeserializeObject<ParamAltaArticulo>(body);
                Articulo articulo = data.articulo;

                if (articulo.descripcion == null || articulo.descripcion == "") throw new Exception("Se debe ingresar la descripcion");
                if (articulo.cantidad <= 0) throw new Exception("Ingrese una cantidad valida");
                if (articulo.precio <= 0) throw new Exception("Se debe ingresar el apellido_paterno");

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
                    var cmd_1 = new MySqlCommand();
                    cmd_1.Connection = conexion;
                    //cmd_1.Transaction = transaccion;
                    cmd_1.CommandText = "INSERT INTO articulos(id_articulo,descripcion, cantidad, precio) VALUES (0,@descripcion,@cantidad,@precio)";
                    cmd_1.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                    cmd_1.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                    cmd_1.Parameters.AddWithValue("@precio", articulo.precio);
                    cmd_1.ExecuteNonQuery();

                    if (articulo.foto != null)
                    {
                        var cmd_2 = new MySqlCommand();
                        cmd_2.Connection = conexion;
                        //cmd_2.Transaction = transaccion;
                        cmd_2.CommandText = "INSERT INTO fotos_articulos (foto,id_articulo) VALUES (@foto,(SELECT id_articulo FROM articulos WHERE descripcion=@descripcion))";
                        cmd_2.Parameters.AddWithValue("@foto", Convert.FromBase64String(articulo.foto));
                        cmd_2.Parameters.AddWithValue("@descripcion", articulo.descripcion);
                        cmd_2.ExecuteNonQuery();
                    }

                    //transaccion.Commit();
                    new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                    return req.CreateResponse(HttpStatusCode.OK, "Se dió de alta el articulo");
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
