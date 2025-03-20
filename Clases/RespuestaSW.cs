namespace back_lns_libros.Clases
{
    public class RespuestaSW
    {
        public int estado { get; set; } = 200;
        public string mensaje { get; set; } = "";
        public object data { get; set; } = new();
    }
}
