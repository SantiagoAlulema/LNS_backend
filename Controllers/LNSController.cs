using back_lns_libros.Clases;
using back_lns_libros.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace back_lns_libros.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LNSController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public LNSController(IConfiguration config)
        {
            Configuration = config;
        }

        [HttpGet("consultar-libro")]
        public IActionResult VerificarLibro(string sku = "", string serie = "")
        {
            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            string JSONString = String.Empty;


            string cadena = $@"SELECT p.id codigoproducto, 
                                       p.sku, 
                                       p.serie, 
                                       p.titulo, 
                                       p.periodo,
                                       COALESCE(es.id ,'') codigoestudiante, 
                                       es.nombre
                                FROM sysplus.product p
                                LEFT JOIN sysplus.estudiantelibro el ON el.codigoproduct = p.id
                                LEFT JOIN sysplus.estudent es ON es.id = el.codigoestudent
                               where sku = '{sku}' and serie = '{serie}' and
                               EXTRACT(YEAR FROM CURRENT_DATE) BETWEEN SPLIT_PART(p.periodo, '-', 1)::INTEGER 
                                        AND SPLIT_PART(p.periodo, '-', 2)::INTEGER;;";
            DataTable dataTable = conn.ejecutarconsulta_dt(cadena);
            RespuestaSW res = new();
            if (dataTable.Rows.Count == 0)
            {
                res.estado = 404;
                res.mensaje = "NO SE ENCONTRARON RESULTADOS";
                return Ok(res);
            }

            if (dataTable.Rows[0]["codigoestudiante"] == "")
            {
                var dataList = dataTable.AsEnumerable().Select(row => new
                {
                    codigoproducto = row["codigoproducto"].ToString(),
                    sku = row["sku"].ToString(),
                    serie = row["serie"].ToString(),
                    titulo = row["titulo"].ToString(),
                    periodo = row["periodo"].ToString(),
                    codigoestudiante = row["codigoestudiante"].ToString(),
                    nombre = row["nombre"].ToString()
                }).ToList();

                res.estado = 404;
                res.mensaje = "EL LIBRO SE ENCUENTRA DISPONIBLE PARA SU REGISTRO";
                res.data = dataList;
                JSONString = Newtonsoft.Json.JsonConvert.SerializeObject(res);
                return Ok(res);
            }

            res.estado = 200;
            res.mensaje = "LIBRO NO ESTA DISPONIBLE";
            JSONString = Newtonsoft.Json.JsonConvert.SerializeObject(res);
            return Ok(res);
        }
        [HttpGet("buscar-estudiante")]
        public IActionResult BuscarEstudiante([FromQuery] string cedula)
        {
            string cadena = $@"SELECT es.id, 
                                      es.nombre, 
                                      el.descripcion periodo , 
                                      i.descripcion ciclo
                               FROM sysplus.estudent es
                               left join sysplus.education_level el on el.id = es.periodo 
                               left join sysplus.institution i on i.id = es.ciclo 
                               WHERE id='{cedula}';";

            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            DataTable dataTable = conn.ejecutarconsulta_dt(cadena);

            if (dataTable.Rows.Count == 0)
            {
                return Ok(new RespuestaSW
                {
                    estado = 404,
                    mensaje = "ESTUDIANTE NO ENCONTRADO",
                    data = new List<Dictionary<string, object>>() // Devuelve una lista vacía
                });
            }

            // Convertir DataTable a lista de diccionarios
            List<Dictionary<string, object>> listaEstudiantes = ConvertirDataTable(dataTable);

            return Ok(new RespuestaSW
            {
                estado = 202,
                mensaje = "ESTUDIANTE ENCONTRADO",
                data = listaEstudiantes
            });
        }

        private List<Dictionary<string, object>> ConvertirDataTable(DataTable dt)
        {
            var lista = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col]; // Asigna cada columna al diccionario
                }
                lista.Add(dict);
            }
            return lista;
        }


        [HttpGet("lista-intituciones")]
        public IActionResult ListaInstituciones()
        {
            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            string cadena = $@"SELECT id, descripcion FROM sysplus.institution;";
            DataTable dataTable = conn.ejecutarconsulta_dt(cadena);

            var lista = new List<Dictionary<string, object>>();
            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                lista.Add(dict);
            }

            RespuestaSW res = new()
            {
                estado = 200,
                mensaje = "LISTA DE INSTITUCIONES ENCONTRADAS",
                data = lista
            };

            return Ok(res);
        }

        [HttpGet("lista-nivel-lectivo")]
        public IActionResult ListaNivelLectivo()
        {
            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            string cadena = $@"SELECT id, descripcion FROM sysplus.education_level;";
            DataTable dataTable = conn.ejecutarconsulta_dt(cadena);

            var lista = new List<Dictionary<string, object>>();
            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                lista.Add(dict);
            }

            RespuestaSW res = new()
            {
                estado = 200,
                mensaje = "LISTA ENCONTRADA NIVEL LECTIVO",
                data = lista
            };
            return Ok(res);
        }
    }
}
