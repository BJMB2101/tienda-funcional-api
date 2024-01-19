using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
namespace FunctionApp1
{
    public static class Get
    {
        [FunctionName("Get")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
        HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                // obtiene los par�metros que pasan en la URL
                string path = null;
                bool descargar = false;
                foreach (var q in req.GetQueryNameValuePairs())
                    if (q.Key.ToLower() == "nombre")
                        path = q.Value;
                    else
                    if (q.Key.ToLower() == "descargar")
                        descargar = q.Value.ToLower() == "si";
                // la variable de entorno HOME est� predefinida en el servidor (C:\home o D:\home)
                string home = Environment.GetEnvironmentVariable("HOME");
                byte[] contenido;
                try
                {
                    // lee el contenido solicitado en la petici�n GET
                    contenido = File.ReadAllBytes(home + "/data" + path);
                }
                catch (FileNotFoundException)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                string nombre = Path.GetFileName(path);
                string tipo_mime = MimeMapping.GetMimeMapping(nombre);
                DateTime fecha_modificacion = File.GetLastWriteTime(home + "/data" + path);
                // verifica si el archivo fue modificado, si no, regresa el c�digo 304
                if (req.Headers.Contains("If-Modified-Since"))
                    if (DateTime.Parse(req.Headers.GetValues("If-Modified-Since").First()) == fecha_modificacion)
                        return new HttpResponseMessage(HttpStatusCode.NotModified);
                // regresa el contenido
                HttpResponseMessage respuesta = req.CreateResponse(HttpStatusCode.OK);
                respuesta.Content = new ByteArrayContent(contenido);
                respuesta.Content.Headers.ContentType = new MediaTypeHeaderValue(tipo_mime);
                // indica al navegador que guarde el contenido en la cache
                respuesta.Content.Headers.LastModified = fecha_modificacion;
                if (descargar) // indica al navegador que descargue el archivo
                    respuesta.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = nombre };
                return respuesta;
            }
            catch (Exception e)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
        }
    }
}