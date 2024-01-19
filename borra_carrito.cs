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
    public static class borra_carrito
    {
        class Articulo
        {
            public int id_articulo;
            public string descripcion;
            public int cantidad; // Aquí, la propiedad "foto" parece no utilizarse
        }

        class ParamBorraArticulos
        {
            public Articulo[] articulos; // Cambiado para aceptar un arreglo de Articulo
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [FunctionName("borra_carrito")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamBorraArticulos data = JsonConvert.DeserializeObject<ParamBorraArticulos>(body);

                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string cs = $"Server={Server};UserID={UserID};Password={Password};Database={Database};SslMode=Preferred;";
                using (var conexion = new MySqlConnection(cs))
                {
                    conexion.Open();
                    new MySqlCommand("begin work", conexion).ExecuteNonQuery();

                    try
                    {
                        foreach (var articulo in data.articulos)
                        {
                            string queryAumentarStock = "UPDATE articulos SET cantidad = cantidad + @cantidad WHERE id_articulo = @idArticulo";
                            var cmdAumentarStock = new MySqlCommand(queryAumentarStock, conexion);
                            cmdAumentarStock.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                            cmdAumentarStock.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                            cmdAumentarStock.ExecuteNonQuery();

                            string queryEliminarCarrito = "DELETE FROM carrito_compra WHERE id_articulo = @idArticulo";
                            var cmdEliminarCarrito = new MySqlCommand(queryEliminarCarrito, conexion);
                            cmdEliminarCarrito.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                            cmdEliminarCarrito.ExecuteNonQuery();
                        }

                        new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                        return req.CreateResponse(HttpStatusCode.OK, "Artículos eliminados del carrito y cantidades actualizadas en stock");
                    }
                    catch (Exception e)
                    {
                        new MySqlCommand("rollback work", conexion).ExecuteNonQuery();
                        throw new Exception(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
        }
    }
}