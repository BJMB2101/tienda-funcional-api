using System;
using System.Collections.Generic;
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
    public static class consulta_carrito
    {
        class Articulo
        {
            public int id_articulo;
            public string descripcion;
            public int cantidad;
            public float precio;
            public string foto;  // foto en base 64
        }
        class ParamConsultaArticulo
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
        [FunctionName("consulta_carrito")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                string body = await req.Content.ReadAsStringAsync();
                ParamConsultaArticulo data = JsonConvert.DeserializeObject<ParamConsultaArticulo>(body);
                string descripcion = data.descripcion;
                log.Info($"Descripción: {descripcion}");
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string Database = Environment.GetEnvironmentVariable("Database");

                string cs = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + Database + ";SslMode=Preferred;";
                var conexion = new MySqlConnection(cs);
                conexion.Open();

                try
                {
                    var cmd = new MySqlCommand("SELECT a.id_articulo, a.descripcion, b.cantidad, a.precio, c.foto, LENGTH(c.foto) FROM articulos a LEFT OUTER JOIN carrito_compra b ON a.id_articulo = b.id_articulo LEFT OUTER JOIN fotos_articulos c ON a.id_articulo = c.id_articulo WHERE b.cantidad > 0; ");

                    cmd.Connection = conexion;
                    MySqlDataReader r = cmd.ExecuteReader();

                    try
                    {
                        var listaArticulos = new List<Articulo>();

                        while (r.Read())
                        {
                            var articulo_foto = new Articulo();
                            articulo_foto.id_articulo = r.GetInt32(0);
                            articulo_foto.descripcion = r.GetString(1);
                            articulo_foto.cantidad = r.GetInt32(2);
                            articulo_foto.precio = r.GetFloat(3);

                            if (!r.IsDBNull(4))
                            {
                                var longitud = r.GetInt32(5);
                                byte[] foto = new byte[longitud];
                                r.GetBytes(4, 0, foto, 0, longitud);
                                articulo_foto.foto = Convert.ToBase64String(foto);
                            }

                            listaArticulos.Add(articulo_foto);
                        }

                        // regresa OK (código 200)
                        HttpResponseMessage respuesta = req.CreateResponse($"Status: {HttpStatusCode.OK}. Descripción: {descripcion}");

                        // regresa JSON con todos los registros
                        respuesta.Content = new StringContent(JsonConvert.SerializeObject(listaArticulos), System.Text.Encoding.UTF8, "application/json");
                        return respuesta;
                    }
                    finally
                    {
                        r.Close();
                    }
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
