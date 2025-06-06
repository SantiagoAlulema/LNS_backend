﻿using back_lns_libros.Clases;
using back_lns_libros.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OfficeOpenXml;
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
        public IActionResult VerificarLibro(string serie = "")
        {
            string JSONString = String.Empty;
            RespuestaSW res = new();
            DataTable dataTable;
            string cadena = $@"select *
                                from librolns.estudiantelibro el
                                where UPPER(el.serielibro) = UPPER('{serie}')";
            dataTable = ejecutarconsulta(cadena);
            if (dataTable.Rows.Count > 0)
            {
                res.estado = 202;
                res.mensaje = "EL LIBRO YA SE ENCUENTRA REGISTRADO";
                JSONString = Newtonsoft.Json.JsonConvert.SerializeObject(res);
                return Ok(res);
            }
         

            cadena = $@"SELECT p.id codigoproducto, 
                                       p.sku, 
                                       p.serie, 
                                       p.titulo, 
                                       p.periodo,
                                       COALESCE(es.id, '') AS codigoestudiante, 
                                       es.nombre
                                FROM librolns.product p
                                LEFT JOIN librolns.estudiantelibro el ON el.codigoproduct = p.id
                                LEFT JOIN librolns.estudent es ON es.id = el.codigoestudent
                               where UPPER(p.serie) = UPPER('{serie}') and p.estado = 'A' 
                               AND NOT EXISTS (SELECT 1 FROM librolns.estudiantelibro esl WHERE esl.codigoproduct = p.id )
                               limit 20;";
            dataTable = ejecutarconsulta(cadena);
            if (dataTable.Rows.Count > 0)
            {
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

                    res.estado = 200;
                    res.mensaje = "EL LIBRO SE ENCUENTRA DISPONIBLE PARA SU REGISTRO";
                    res.data = dataList;
                    JSONString = Newtonsoft.Json.JsonConvert.SerializeObject(res);
                    return Ok(res);
                }
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
                                       COALESCE(esli.codigoinstitucion, '') AS periodo, 
       COALESCE(esli.nivelacademico, '') AS ciclo
                               FROM librolns.estudent es
                               left join librolns.estudiantelibro esli on esli.codigoestudent =  es.id 
                               left join librolns.education_level el on el.id = esli.nivelacademico
                               left join librolns.institution i on i.id = esli.codigoinstitucion 
                               WHERE es.id='{cedula}';";

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
            string cadena = $@"SELECT id, descripcion FROM librolns.institution;";
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


        [HttpPost("upload-book-news")]
        public IActionResult ExecuteBulkInsert(List<BookData> books)
        {
            try
            {
                //String cadena = "UPDATE librolns.product SET estado = 'I'";
                //ejecutarconsulta(cadena);
                String cadena = "INSERT INTO librolns.product VALUES ";
                cadena += string.Join(", ", books.Select(book =>
                                     $"('{Guid.NewGuid().ToString()}', '{book.sku}', '{book.serie}', '{book.titulo}', '{book.periodo}', 'A')"));
                ejecutarconsulta(cadena);
                return Ok("SE INSERTO CORRECTAMENTE");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR: {ex.Message}");
            }
        }


        //[HttpPost("upload-book-news")]

        //public IActionResult UploadBookNews(List<BookData> books)
        //{
        //    try
        //    {
        //        // 1. Crear tabla de repetidos si no existe
        //        ejecutarconsulta(@"
        //    CREATE TABLE IF NOT EXISTS librolns.temp_books (
        //        sku TEXT,
        //        serie TEXT,
        //        titulo TEXT,
        //        periodo TEXT,
        //        fecha_registro TIMESTAMP DEFAULT NOW()
        //    );");

        //        // 2. Crear tabla temporal de sesión (no puede ser TEMP porque cambia de conexión)
        //        ejecutarconsulta(@"
        //    CREATE TABLE IF NOT EXISTS librolns.temp_books_session (
        //        sku TEXT,
        //        serie TEXT,
        //        titulo TEXT,
        //        periodo TEXT,
        //        fecha_registro TIMESTAMP DEFAULT NOW()
        //    );");

        //        // 3. Limpiar tabla de sesión antes de usarla
        //        ejecutarconsulta("DELETE FROM librolns.temp_books_session;");

        //        // 4. Insertar los datos del request en la tabla temporal
        //        if (books.Any())
        //        {
        //            var insertTemp = "INSERT INTO librolns.temp_books_session (sku, serie, titulo, periodo, fecha_registro) VALUES " +
        //                string.Join(", ", books.Select(b =>
        //                    $"('{b.sku.Replace("'", "''")}', '{b.serie.Replace("'", "''")}', '{b.titulo.Replace("'", "''")}', '{b.periodo.Replace("'", "''")}', NOW())"));
        //            ejecutarconsulta(insertTemp);

        //            // 5. Insertar los duplicados en temp_books (los que YA existen en product)
        //            var insertDuplicates = @"
        //        INSERT INTO librolns.temp_books (sku, serie, titulo, periodo, fecha_registro)
        //        SELECT t.sku, t.serie, t.titulo, t.periodo, NOW()
        //        FROM librolns.temp_books_session t
        //        WHERE EXISTS (
        //            SELECT 1 FROM librolns.product p WHERE p.serie = t.serie
        //        );";
        //            ejecutarconsulta(insertDuplicates);

        //            // 6. Insertar los libros únicos en product
        //            var insertUnique = @"
        //        INSERT INTO librolns.product (id, sku, serie, titulo, periodo, estado)
        //        SELECT gen_random_uuid(), t.sku, t.serie, t.titulo, t.periodo, 'A'
        //        FROM librolns.temp_books_session t
        //        WHERE NOT EXISTS (
        //            SELECT 1 FROM librolns.product p WHERE p.serie = t.serie
        //        );";
        //            ejecutarconsulta(insertUnique);
        //        }

        //        return Ok("Insertados correctamente. Duplicados almacenados en librolns.temp_books.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest($"Error: {ex.Message}");
        //    }
        //}



        [HttpGet("download-duplicates")]
        public IActionResult DownloadDuplicates()
        {
            try
            {
                var query = @"
            SELECT t.* 
            FROM librolns.temp_books t
            WHERE t.fecha_registro >= NOW() - INTERVAL '1 hour'";

                var duplicates = ejecutarconsulta(query);

                if (duplicates.Rows.Count == 0)
                    return Ok("No se encontraron duplicados en la última hora.");
                ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
                using (var package = new ExcelPackage())
                {

                    var worksheet = package.Workbook.Worksheets.Add("Duplicados");
                    worksheet.Cells["A1"].LoadFromDataTable(duplicates, true);
                    return File(package.GetAsByteArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "duplicados.xlsx");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al generar el archivo: {ex.Message}");
            }
        }

        [HttpGet("reporte-libros-registrados")]
        public IActionResult ReporteComercial([FromQuery] string institucion = "", [FromQuery] string nivelacademico = "", [FromQuery] string parameterSearch = "")
        {
            List<string> condiciones = new List<string>();

            if (!string.IsNullOrWhiteSpace(institucion))
            {
                condiciones.Add($"rp.id_institucion = '{institucion}'");
            }
            if (!string.IsNullOrWhiteSpace(nivelacademico))
            {
                condiciones.Add($"rp.id_academico = '{nivelacademico}'");
            }
            if (!string.IsNullOrWhiteSpace(parameterSearch))
            {
                condiciones.Add($"(rp.estudiante ILIKE '%{parameterSearch}%' OR rp.serie ILIKE '%{parameterSearch}%' OR rp.cedula ILIKE '%{parameterSearch}%')");
            }
            string whereClause = condiciones.Count > 0 ? $"WHERE {string.Join(" OR ", condiciones)}" : "";

            string consulta = $@"SELECT * 
                                FROM librolns.v_rpt_comercial rp 
                                {whereClause};";

            PgConn conn = new PgConn
            {
                cadenaConnect = Configuration["Conn_LNS"]
            };

            DataTable dataTable = conn.ejecutarconsulta_dt(consulta, 800);
            string jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(dataTable);
            return Ok(jsonResult);
        }



        [HttpPost("login-user")]
        public IActionResult GrabarLibroEstudiante([FromBody] UserLogin user)
        {
            string cadena = $@"select coduser, username, status from librolns.user where username = '{user.username}' and password ='{user.password}';";
            DataTable dtResponse = ejecutarconsulta(cadena);
            string JSONString = Newtonsoft.Json.JsonConvert.SerializeObject(dtResponse);
            return Ok(JSONString);

        }


        [HttpPost("save-estudiante-libro")]
        public IActionResult GrabarLibroEstudiante([FromBody] StudentBookSave nuevolibroestudiante)
        {
            try
            {
                string cadena = string.Empty;
                string codigoUnidadEducativa = nuevolibroestudiante.noexisteunidadeducativa ? Guid.NewGuid().ToString() : nuevolibroestudiante.unidadeducativa;
                if (nuevolibroestudiante.noexisteunidadeducativa)
                {
                    cadena = $@"INSERT INTO librolns.institution
                                VALUES('{codigoUnidadEducativa}', '{nuevolibroestudiante.unidadeducativa}');";
                    ejecutarconsulta(cadena);
                }

                cadena = $@"INSERT INTO librolns.estudent
                            SELECT '{nuevolibroestudiante.codigoestudiante}', '{nuevolibroestudiante.nombreestudiante}', 'nuevo', 'nuevo'
                            WHERE NOT EXISTS (
                            SELECT 1 FROM librolns.estudent WHERE id = '{nuevolibroestudiante.codigoestudiante}');";
                ejecutarconsulta(cadena);

                cadena = $@"INSERT INTO librolns.estudiantelibro
                                   VALUES('{Guid.NewGuid().ToString()}', 
                                          '{nuevolibroestudiante.codigoestudiante}', 
                                          '{nuevolibroestudiante.codigolibro}', 
                                           NOW(), 
                                          'A', 
                                          '{nuevolibroestudiante.periodo}',
                                          '{codigoUnidadEducativa}',
                                           '{nuevolibroestudiante.ciclo}',
                                          '{nuevolibroestudiante.serie}',
                                          '{nuevolibroestudiante.latitud}',
                                          '{nuevolibroestudiante.longitud}');";
                ejecutarconsulta(cadena);
                return Ok("SE INSERTO EL LIBRO CORRECTAMENTE");
            }
            catch (Exception ex)
            {
                return BadRequest($"SE PRESENTO EL SIGUIENTE ERROR: {ex.Message}");
            }
        }

        private DataTable ejecutarconsulta(string cadena)
        {
            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            DataTable dtRespuesta = conn.ejecutarconsulta_dt(cadena);
            return dtRespuesta;
        }

        [HttpGet("lista-nivel-lectivo")]
        public IActionResult ListaNivelLectivo()
        {
            PgConn conn = new PgConn();
            conn.cadenaConnect = Configuration["Conn_LNS"];
            string cadena = $@"SELECT id, descripcion FROM librolns.education_level;";
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
