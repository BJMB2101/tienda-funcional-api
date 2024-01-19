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
    public static class ingresar_articulo
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
        [FunctionName("ingresa_articulo")]
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
                        if (articulo.cantidad == -1)
                        {
                            // Consulta la cantidad actual en carrito_compra
                            string queryConsultaCarrito = "SELECT cantidad FROM carrito_compra WHERE id_articulo = @idArticulo";
                            var cmdConsultaCarrito = new MySqlCommand(queryConsultaCarrito, conexion);
                            cmdConsultaCarrito.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                            object resultCarrito = cmdConsultaCarrito.ExecuteScalar();

                            if (resultCarrito != null)
                            {
                                int cantidadCarrito = Convert.ToInt32(resultCarrito);

                                // Aumenta la cantidad en 1 en la tabla articulos
                                string queryAumentarStock = "UPDATE articulos SET cantidad = cantidad + 1 WHERE id_articulo = @idArticulo";
                                var cmdAumentarStock = new MySqlCommand(queryAumentarStock, conexion);
                                cmdAumentarStock.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                cmdAumentarStock.ExecuteNonQuery();

                                if (cantidadCarrito == 1)
                                {
                                    // Elimina el artículo del carrito si la cantidad es 1
                                    string queryEliminarCarrito = "DELETE FROM carrito_compra WHERE id_articulo = @idArticulo";
                                    var cmdEliminarCarrito = new MySqlCommand(queryEliminarCarrito, conexion);
                                    cmdEliminarCarrito.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                    cmdEliminarCarrito.ExecuteNonQuery();
                                    new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                                    return req.CreateResponse(HttpStatusCode.OK, "Se eliminó el artículo "+articulo.descripcion+" del carrito");
                                }
                                else
                                {
                                    // Resta la cantidad en 1 en el carrito_compra si la cantidad es mayor que 1
                                    string queryDisminuirCarrito = "UPDATE carrito_compra SET cantidad = cantidad - 1 WHERE id_articulo = @idArticulo";
                                    var cmdDisminuirCarrito = new MySqlCommand(queryDisminuirCarrito, conexion);
                                    cmdDisminuirCarrito.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                    cmdDisminuirCarrito.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                        {
                            // Verifica la cantidad disponible en la tabla de artículos
                            string queryStock = "SELECT cantidad FROM articulos WHERE id_articulo = @idArticulo";
                            var cmdStock = new MySqlCommand(queryStock, conexion);
                            cmdStock.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                            object resultStock = cmdStock.ExecuteScalar();

                            if (resultStock != null && Convert.ToInt32(resultStock) >= articulo.cantidad)
                            {
                                // Procede con la lógica de carrito_compra

                                // Verifica si el artículo ya existe en carrito_compra
                                string queryVerificar = "SELECT cantidad FROM carrito_compra WHERE id_articulo = @idArticulo";
                                var cmdVerificar = new MySqlCommand(queryVerificar, conexion);
                                cmdVerificar.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                object result = cmdVerificar.ExecuteScalar();

                                if (result != null)
                                {
                                    // Artículo existe, actualiza la cantidad
                                    int cantidadExistente = Convert.ToInt32(result);
                                    string queryActualizar = "UPDATE carrito_compra SET cantidad = @nuevaCantidad WHERE id_articulo = @idArticulo";
                                    var cmdActualizar = new MySqlCommand(queryActualizar, conexion);
                                    cmdActualizar.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                    cmdActualizar.Parameters.AddWithValue("@nuevaCantidad", cantidadExistente + articulo.cantidad);
                                    cmdActualizar.ExecuteNonQuery();
                                }
                                else
                                {
                                    // Artículo no existe, inserta nuevo artículo
                                    string queryInsertar = "INSERT INTO carrito_compra(id_carrito, id_articulo, cantidad) VALUES (0, @idArticulo, @cantidad)";
                                    var cmdInsertar = new MySqlCommand(queryInsertar, conexion);
                                    cmdInsertar.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                    cmdInsertar.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                                    cmdInsertar.ExecuteNonQuery();
                                }

                                // Actualiza la cantidad en la tabla articulos
                                string queryActualizarStock = "UPDATE articulos SET cantidad = cantidad - @cantidad WHERE id_articulo = @idArticulo";
                                var cmdActualizarStock = new MySqlCommand(queryActualizarStock, conexion);
                                cmdActualizarStock.Parameters.AddWithValue("@cantidad", articulo.cantidad);
                                cmdActualizarStock.Parameters.AddWithValue("@idArticulo", articulo.id_articulo);
                                cmdActualizarStock.ExecuteNonQuery();
                            }
                            else
                            {
                                // No hay suficiente stock
                                new MySqlCommand("rollback work", conexion).ExecuteNonQuery();
                                return req.CreateResponse(HttpStatusCode.BadRequest, "No hay suficiente cantidad en stock");
                            }
                        }

                        new MySqlCommand("commit work", conexion).ExecuteNonQuery();
                        return req.CreateResponse(HttpStatusCode.OK, "Operación realizada con éxito");
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