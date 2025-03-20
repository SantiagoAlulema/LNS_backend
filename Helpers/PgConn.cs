using Npgsql;
using System.Data;

namespace back_lns_libros.Helpers
{
    public class PgConn
    {
        // conexion base de datos
        public NpgsqlConnection connectSQL = new NpgsqlConnection();
        public string cadenaConnect = string.Empty; //= "Username=postgres;Password=.multim0t0s.;Host=10.10.10.28;Database=unnopartsdb";

        public void abrir()
        {
            try
            {
                //abrir conexion
                connectSQL.ConnectionString = cadenaConnect;
                connectSQL.Open();
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), ex.Message);
            }
        }

        public void cerrar()
        {
            try
            {
                //cerrar conexion
                connectSQL.Close();
                connectSQL.Dispose();
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), ex.Message);
                System.Environment.Exit(0);
            }
        }

        public DataTable ejecutarconsulta_dt(string consulta, int timeout = 0)
        {
            DataTable retorna = new DataTable();

            NpgsqlDataReader conn = default(NpgsqlDataReader);
            //Dim conn1 As OracleDataAdapter
            try
            {

                NpgsqlCommand comand = new NpgsqlCommand(consulta, connectSQL);
                if (timeout > 0)
                    comand.CommandTimeout = timeout;
                this.abrir();
                conn = comand.ExecuteReader();
                retorna.Load(conn);
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), ex.Message);
                throw new Exception(ex.Message);
                //System.Environment.Exit(0);
            }
            this.cerrar();
            return retorna;
            //return null;
        }


        public string ejecutarconsulta_sin_dt(string consulta)
        {
            string resultado = string.Empty;
            DataTable retorna = new DataTable();
            NpgsqlCommand conn = default(NpgsqlCommand);
            try
            {
                this.abrir();
                conn = new NpgsqlCommand(consulta, connectSQL);
                conn.ExecuteNonQueryAsync();
                resultado = "OK";
            }
            catch (Exception ex)
            {
                resultado = ex.Message;
                this.guarda_errores(obtiene_fecha(), ex.Message);
                //System.Environment.Exit(0);                
            }
            this.cerrar();
            return resultado;
        }

        public async Task<string> EjecutarConsultaSinDTAsync(string consulta, Dictionary<string, object> parametros = null)
        {
            string resultado = string.Empty;

            try
            {
                this.abrir();

                using (var cmd = new NpgsqlCommand(consulta, connectSQL))
                {
                    // Agregar parámetros si existen
                    if (parametros != null)
                    {
                        foreach (var param in parametros)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    await cmd.ExecuteNonQueryAsync();
                    resultado = "OK";
                }
            }
            catch (Exception ex)
            {
                resultado = ex.Message;
                this.guarda_errores(obtiene_fecha(), ex.Message);
            }
            finally
            {
                this.cerrar();
            }

            return resultado;
        }

        public async Task<DataTable> EjecutarConsultaDTAsync(string consulta, Dictionary<string, object> parametros = null)
        {
            DataTable resultado = new DataTable();

            try
            {
                using (var cmd = new NpgsqlCommand(consulta, connectSQL))
                {
                    // Agregar parámetros si existen
                    if (parametros != null)
                    {
                        foreach (var param in parametros)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }


                    NpgsqlDataReader conn = default(NpgsqlDataReader);
                    this.abrir();
                    conn = await cmd.ExecuteReaderAsync();
                    resultado.Load(conn);
                }
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                this.cerrar();
            }

            return resultado;

        }

        public void ejecutaprocedimientos(string nombre, int timeout = 0)
        {
            try
            {
                this.abrir();

                using (var conn = new NpgsqlCommand(nombre, connectSQL))
                {
                    conn.CommandType = CommandType.StoredProcedure;
                    //conn.Parameters.AddWithValue("pfecha", NpgsqlDbType.Date, fecha);

                    if (timeout > 0)
                        conn.CommandTimeout = timeout;

                    conn.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), ex.Message);
                //System.Environment.Exit(0);
            }
            finally
            {
                this.cerrar();
            }
        }


        public Boolean ejecutaprocedimientos_actualizarGcobranza_Imora(string[] _params)
        {
            /*OracleCommand conn = default(OracleCommand);
            string nombre = "STOCK.PK_ACTUALIZACION_DATA_APP.PS_ACTUALIZAR_GCOBRANZAS_IMORA";
            DataSet Ds = new DataSet();
            Boolean respuesta = false;
            try
            {
                this.abrir();
                conn = new OracleCommand("{ CALL " + nombre + "(?) }", sqlConnection1);
                conn.CommandText = nombre;
                conn.CommandType = CommandType.StoredProcedure;
                conn.Parameters.Add("pgestor", OracleDbType.Varchar2).Value = _params[0];
                conn.Parameters.Add("pcliente", OracleDbType.Varchar2).Value = _params[1];
                conn.Parameters.Add("pcod_comprobante", OracleDbType.Varchar2).Value = _params[2];
                conn.Parameters.Add("ptipo_comprobante", OracleDbType.Varchar2).Value = _params[3];
                conn.Parameters.Add("pempresa", OracleDbType.Varchar2).Value = _params[4];
                conn.ExecuteNonQuery();
                respuesta = true;
            }
            catch (Exception ex)
            {
                this.guarda_errores(obtiene_fecha(), "ACTUALIZAR GASTO-IMORA " + ex.Message);
                System.Environment.Exit(0);
            }
            this.cerrar();
            return respuesta;*/
            return true;
        }

        private static readonly object lockObject = new object();

        public void guarda_errores(string fecha, string mensaje)
        {
            string consulta = Directory.GetCurrentDirectory();
            string rutaArchivo = Path.Combine(consulta, "error_log.txt");

            try
            {
                lock (lockObject)
                {
                    using (StreamWriter archivo = File.AppendText(rutaArchivo))
                    {
                        archivo.WriteLine($"{fecha} - - - - - - - {mensaje}");
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"No se pudo escribir en el archivo de log: {e.Message}");
            }
        }

        public static string obtiene_fecha()
        {
            string fechahora = Convert.ToString(DateTime.Now.ToLongDateString());
            return fechahora;
        }
    }

}