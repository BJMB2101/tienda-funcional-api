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
    public static class borra_de_carrito
    {
        class Articulo
        {
            public int id_articulo;
            public string descripcion;
            public int cantidad; // foto en base 64
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
        [FunctionName("borra_de_carrito")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamAltaArticulo data = JsonConvert.DeserializeObject<ParamAltaArticulo>(body);
                Articulo articulo = data.articulo;

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
                        string queryAumentarStock = "UPDATE articulos SET cantidad = cantidad + @cantidad WHERE id_articulo = @idArticulo";
                        var cmdAumentarStock = new MySqlCommand(queryAumentarStock, conexion);
                        cmdAumentarStock.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                        cmdAumentarStock.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                        cmdAumentarStock.ExecuteNonQuery();

                        string queryEliminarCarrito = "DELETE FROM carrito_compra WHERE id_articulo = @idArticulo";
                        var cmdEliminarCarrito = new MySqlCommand(queryEliminarCarrito, conexion);
                        cmdEliminarCarrito.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                        cmdEliminarCarrito.ExecuteNonQuery();
                        new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                        return req.CreateResponse(HttpStatusCode.OK, "Se eliminó el artículo " + articulo.descripcion + " del carrito");
                        
                        
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